import { useRef, useState, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { enviarFotoAvaria, listarAvarias, registrarAvaria } from '@/api/avarias';
import { buscarVeiculo } from '@/api/veiculos';
import { obterMensagemErro } from '@/lib/erros';
import { prepararFotoParaEnvio, criarAmbienteReducaoDoNavegador, detectarSuporteReducao, memorizarSuporteReducao } from '@/lib/reducaoFotoVeiculo';
import { lerCabecalhoDoArquivo } from '@/lib/cabecalhoFoto';
import { ErroApi } from '@/types/api';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import type { VeiculoRetorno } from '@/types/veiculos';
import { marcarAguardandoComoErro, tirarMarcadores, type ItemAvariaLocal } from './avariasVistoria';
import {
  desfechoFalhaDoEnvio,
  localizarAvariaDoEnvio,
  passoDoEnvio,
  STATUS_RECUSA_DO_POST,
  STATUS_SEM_REPETICAO_DO_POST,
} from './sequenciaEnvioAvaria';

export const chaveAvarias = (idVistoria: number | null) => ['vistoria-avarias', idVistoria] as const;
export const chaveVeiculoDaVistoria = (idVeiculo: number | null) => ['vistoria-veiculo', idVeiculo] as const;

/** Avarias ativas da vistoria, com a foto (URL temporária) quando o arquivo existe. */
export function useAvariasVistoria(idVistoria: number | null) {
  return useQuery<AvariaRegistrada[]>({
    queryKey: chaveAvarias(idVistoria),
    queryFn: () => listarAvarias(idVistoria as number),
    enabled: idVistoria !== null,
    retry: false,
  });
}

/** Veículo da locação, para o nome e a placa da faixa de topo. Falha não bloqueia a tela. */
export function useVeiculoDaVistoria(idVeiculo: number | null) {
  return useQuery<VeiculoRetorno>({
    queryKey: chaveVeiculoDaVistoria(idVeiculo),
    queryFn: () => buscarVeiculo(idVeiculo as number),
    enabled: idVeiculo !== null,
    retry: false,
  });
}

/** Suporte do navegador à redução da foto: a sonda roda uma vez por carga da página (mesma regra da tela 05). */
const obterSuporteReducao = memorizarSuporteReducao(() => detectarSuporteReducao(criarAmbienteReducaoDoNavegador()));

/** Foto reduzida no navegador quando possível; nunca falha (sem redução, vai o original). */
async function prepararFoto(arquivo: File): Promise<File> {
  const cabecalho = await lerCabecalhoDoArquivo(arquivo);
  const suporte = await obterSuporteReducao();
  const preparada = await prepararFotoParaEnvio(arquivo, cabecalho, suporte, criarAmbienteReducaoDoNavegador());
  return preparada.arquivo;
}

/**
 * Procura, na lista, a avaria que acabou de ser gravada sem foto: mesma descrição, gravidade e ângulo, sem arquivo.
 * Pega a de maior id. Sem achar (ou sem lista), devolve nulo e o item volta a "erro".
 */
export async function localizarAvariaSemFoto(
  idVistoria: number,
  dados: DadosNovaAvaria,
): Promise<number | null> {
  try {
    const lista = await listarAvarias(idVistoria);
    const candidatas = lista.filter(
      (avaria) =>
        avaria.foto === null && avaria.descricao === dados.descricao && avaria.gravidade === dados.gravidade && avaria.angulo === dados.angulo,
    );
    if (candidatas.length === 0) return null;
    return Math.max(...candidatas.map((avaria) => avaria.idAvaria));
  } catch {
    return null;
  }
}

/** Lista da vistoria, ou nulo quando a leitura falha (a recuperação do envio então não roda; nada é repetido às cegas). */
async function listaOuNula(idVistoria: number): Promise<AvariaRegistrada[] | null> {
  try {
    return await listarAvarias(idVistoria);
  } catch {
    return null;
  }
}

/** Progresso do lote da tela: "Vistoria registrada. 2 de 3 avarias enviadas." */
export interface ProgressoEnvio {
  enviadas: number;
  total: number;
}

/** Avarias locais da tela 19 e o envio em ordem: o POST da avaria leva a foto; a falha da foto repete só a foto. */
export interface EnvioAvarias {
  locais: readonly ItemAvariaLocal[];
  enviando: boolean;
  progresso: ProgressoEnvio | null;
  /** Algum item local ainda não foi enviado (erro no POST ou foto pendente): calculado dos itens, não de um lote. */
  falhou: boolean;
  adicionar(dados: DadosNovaAvaria): void;
  redefinir(): void;
  /** A entrada já existe (409 do registro): as avarias locais ainda não enviadas viram erro, com "Tentar de novo". */
  marcarNaoEnviadas(): void;
  enviarPendentes(idVistoria: number): Promise<void>;
  tentarDeNovo(idVistoria: number, chave: number): Promise<void>;
  substituirFotoGravada(idVistoria: number, idAvaria: number, arquivo: File): Promise<void>;
}

const situacoesPendentes: ItemAvariaLocal['situacao'][] = ['aguardando', 'erro', 'foto_pendente'];

/** Controle do envio. Cada item segue: aguardando ou erro (POST) → gravada, ou foto_pendente (PUT da foto). */
export function useEnvioAvarias(): EnvioAvarias {
  const queryClient = useQueryClient();
  const [locais, setLocais] = useState<ItemAvariaLocal[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [progresso, setProgresso] = useState<ProgressoEnvio | null>(null);
  const proximaChave = useRef(1);
  const previasRef = useRef<string[]>([]);

  // Libera as prévias de foto ao desmontar a tela (cada URL de objeto é liberada uma única vez).
  useEffect(() => {
    previasRef.current = locais.map((item) => item.previa);
  }, [locais]);
  useEffect(
    () => () => {
      for (const previa of previasRef.current) URL.revokeObjectURL(previa);
    },
    [],
  );

  function atualizar(chave: number, parcial: Partial<ItemAvariaLocal>): void {
    setLocais((anteriores) => anteriores.map((item) => (item.chave === chave ? { ...item, ...parcial } : item)));
  }

  function remover(itens: readonly ItemAvariaLocal[]): void {
    const chaves = new Set(itens.map((item) => item.chave));
    for (const item of itens) URL.revokeObjectURL(item.previa);
    setLocais((anteriores) => anteriores.filter((item) => !chaves.has(item.chave)));
  }

  async function enviarUm(idVistoria: number, item: ItemAvariaLocal): Promise<boolean> {
    atualizar(item.chave, { situacao: 'enviando', mensagemErro: undefined, semRepeticao: undefined });
    // Lista antes do POST: se a resposta se perder, só uma avaria NOVA com os mesmos dados conta como gravada.
    let idsAntesDoPost: number[] | null = null;
    try {
      const arquivo = await prepararFoto(item.dados.foto);
      const passo = passoDoEnvio(item.idAvaria);
      if (passo.tipo === 'enviar_foto') {
        await enviarFotoAvaria(idVistoria, passo.idAvaria, arquivo);
      } else {
        const antes = await listaOuNula(idVistoria);
        idsAntesDoPost = antes === null ? null : antes.map((avaria) => avaria.idAvaria);
        await registrarAvaria(idVistoria, { ...item.dados, foto: arquivo });
      }
      remover([item]);
      await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
      return true;
    } catch (erro: unknown) {
      const desfecho = desfechoFalhaDoEnvio(item.idAvaria, erro);
      if (desfecho.tipo === 'foto_pendente') {
        atualizar(item.chave, { situacao: 'foto_pendente', idAvaria: desfecho.idAvaria });
        await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
        return false;
      }
      if (desfecho.tipo === 'sem_id_no_502') {
        // 502 sem idAvaria no corpo: procura a avaria sem foto na lista antes de qualquer nova tentativa do POST.
        const idAvaria = await localizarAvariaSemFoto(idVistoria, item.dados);
        if (idAvaria !== null) {
          atualizar(item.chave, { situacao: 'foto_pendente', idAvaria });
          await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
          return false;
        }
      }
      if (item.idAvaria === null && erro instanceof ErroApi && erro.status === 0 && idsAntesDoPost !== null) {
        // Sem resposta (tempo esgotado ou queda): o POST pode ter gravado. Se a lista mostra a avaria nova, ela conta como gravada.
        const depois = await listaOuNula(idVistoria);
        const gravada = depois === null ? null : localizarAvariaDoEnvio(idsAntesDoPost, depois, item.dados);
        if (gravada !== null) {
          if (gravada.foto !== null) {
            remover([item]);
            await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
            return true;
          }
          atualizar(item.chave, { situacao: 'foto_pendente', idAvaria: gravada.idAvaria });
          await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
          return false;
        }
      }
      // Recusa do POST (400, 404, 409): o texto do servidor, já filtrado, vai para o item. O 400 e o 409 não se resolvem repetindo.
      const recusado = erro instanceof ErroApi && STATUS_RECUSA_DO_POST.includes(erro.status);
      atualizar(item.chave, {
        situacao: 'erro',
        mensagemErro: recusado ? obterMensagemErro(erro) : undefined,
        semRepeticao: erro instanceof ErroApi && STATUS_SEM_REPETICAO_DO_POST.includes(erro.status),
      });
      return false;
    }
  }

  return {
    locais,
    enviando,
    progresso,
    // Um reenvio que dá certo não apaga o aviso enquanto outra avaria segue pendente (o aviso vem dos itens).
    falhou: locais.some((item) => item.situacao === 'erro' || item.situacao === 'foto_pendente'),
    adicionar(dados) {
      const chave = proximaChave.current;
      proximaChave.current += 1;
      const item: ItemAvariaLocal = {
        chave,
        dados,
        previa: URL.createObjectURL(dados.foto),
        situacao: 'aguardando',
        idAvaria: null,
      };
      setLocais((anteriores) => [...anteriores, item]);
    },
    marcarNaoEnviadas() {
      setLocais((anteriores) => marcarAguardandoComoErro(anteriores));
    },
    redefinir() {
      // Só tira o marcador (ângulo e posição) das avarias ainda não gravadas; elas continuam na lista, sem posição.
      setLocais((anteriores) =>
        anteriores.map((item) =>
          item.idAvaria === null && (item.situacao === 'aguardando' || item.situacao === 'erro')
            ? { ...item, dados: tirarMarcadores(item.dados) }
            : item,
        ),
      );
    },
    async enviarPendentes(idVistoria) {
      const pendentes = locais.filter((item) => situacoesPendentes.includes(item.situacao));
      if (pendentes.length === 0) return;
      setEnviando(true);
      setProgresso({ enviadas: 0, total: pendentes.length });
      let enviadas = 0;
      for (const item of pendentes) {
        if (await enviarUm(idVistoria, item)) enviadas += 1;
        setProgresso({ enviadas, total: pendentes.length });
      }
      setEnviando(false);
    },
    async tentarDeNovo(idVistoria, chave) {
      const item = locais.find((candidato) => candidato.chave === chave);
      if (!item || enviando) return;
      setEnviando(true);
      setProgresso(null);
      await enviarUm(idVistoria, item);
      setEnviando(false);
    },
    async substituirFotoGravada(idVistoria, idAvaria, arquivo) {
      // Troca da foto de avaria gravada: só o PUT, que grava por cima do arquivo anterior. Sem DELETE antes, a foto
      // antiga não some se a nova falhar. A avaria não muda e o POST não se repete.
      setEnviando(true);
      try {
        const preparado = await prepararFoto(arquivo);
        await enviarFotoAvaria(idVistoria, idAvaria, preparado);
      } finally {
        setEnviando(false);
        await queryClient.invalidateQueries({ queryKey: chaveAvarias(idVistoria) });
      }
    },
  };
}
