import { useEffect, useId, useMemo, useRef, useState, type ChangeEvent, type DragEvent } from 'react';
import { Botao, Icone, Modal, useAviso } from '@/components/ui';
import { useFilaEnvioFotos } from '@/features/frota/hooks/useFilaEnvioFotos';
import { useFotosVeiculo } from '@/features/frota/hooks/useFotosVeiculo';
import { useRemoverFotoVeiculo } from '@/features/frota/hooks/useRemoverFotoVeiculo';
import { analisarArquivosFoto, type RecusaArquivoFoto } from '@/lib/arquivosFotoVeiculo';
import { lerCabecalhoDoArquivo } from '@/lib/cabecalhoFoto';
import { obterMensagemErro } from '@/lib/erros';
import {
  erroDaFotoGravada,
  fotosParaExibir,
  formatarContadorFotos,
  MAXIMO_FOTOS_POR_VEICULO,
  resumoFila,
  urlDaImagem,
  vagasRestantes,
  veiculoMudouDesdeAEscolha,
} from '@/lib/fotosVeiculo';
import { MENSAGEM_CARREGANDO } from '@/lib/mensagens';
import {
  MENSAGEM_FOTOS_ADICIONAR_MAIS,
  MENSAGEM_FOTOS_APOIO,
  MENSAGEM_FOTOS_ARRASTE,
  MENSAGEM_FOTOS_CLIQUE,
  MENSAGEM_FOTOS_CONFIRMAR_CANCELAR,
  MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_DESCRICAO,
  MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_TITULO,
  MENSAGEM_FOTOS_CONFIRMAR_REMOVER,
  MENSAGEM_FOTOS_ENVIADA,
  MENSAGEM_FOTOS_ERRO_CARREGAR,
  MENSAGEM_FOTOS_ERRO_REMOVER,
  MENSAGEM_FOTOS_FORMATO,
  MENSAGEM_FOTOS_REMOVIDA,
  MENSAGEM_FOTOS_SELECIONADAS,
  MENSAGEM_FOTOS_TENTAR_DE_NOVO,
  MENSAGEM_FOTOS_TITULO,
  MENSAGEM_FOTOS_VAZIO,
  MENSAGEM_FOTOS_ZONA_ROTULO,
  mensagemFotoAlt,
  mensagemFotoLimiteAtingido,
  mensagemFotoProgressoFila,
  mensagemFotoSemVaga,
} from '@/lib/mensagensFotosVeiculo';
import type { FotoDoVeiculo, ItemFilaFoto } from '@/types/fotosVeiculo';
import { MiniaturaFotoVeiculo } from './MiniaturaFotoVeiculo';

/** Item da fila ainda não gravado (a fila tira as concluídas da exibição). */
type ItemNaFila = ItemFilaFoto & { situacao: 'aguardando' | 'enviando' | 'erro' };

export interface BlocoFotosVeiculoProps {
  /** Id do veículo. Nulo no cadastro: as fotos escolhidas ficam na fila até o veículo existir. */
  idVeiculo: number | null;
  /** Nome curto do veículo para o texto alternativo das fotos (ex.: "Jeep Renegade"). */
  descricaoVeiculo?: string;
  /** Fim de um lote de envio (ver `useFilaEnvioFotos`). Opcional: a tela de cadastro usa para seguir para a edição. */
  aoFimDoLote?: (temFalha: boolean) => void;
}

/**
 * Bloco "Fotos do Veículo" do desenho 05. Dois usos: no cadastro (sem id, as fotos ficam na fila até o veículo
 * existir) e na edição (com id: carrega as gravadas, envia ao escolher e remove com confirmação). O servidor decide
 * o limite; aqui ele só evita pedidos que já sabemos que serão recusados.
 */
export function BlocoFotosVeiculo({ idVeiculo, descricaoVeiculo, aoFimDoLote }: BlocoFotosVeiculoProps) {
  const aviso = useAviso();
  const consulta = useFotosVeiculo(idVeiculo);
  const remocao = useRemoverFotoVeiculo();
  const fila = useFilaEnvioFotos(idVeiculo, { aoFimDoLote });
  const idTitulo = useId();
  const idSelecionadas = useId();
  const refEntrada = useRef<HTMLInputElement>(null);
  const [recusas, setRecusas] = useState<RecusaArquivoFoto[]>([]);
  const [arrastando, setArrastando] = useState(false);
  const [fotoParaRemover, setFotoParaRemover] = useState<FotoDoVeiculo | null>(null);

  // Quando o veículo muda (ex.: a rota de edição reaproveita o bloco), recusas e confirmação da tela anterior saem.
  // Ajuste de estado durante a renderização (padrão do React), sem efeito.
  const [idDoEstado, setIdDoEstado] = useState<number | null>(idVeiculo);
  if (idDoEstado !== idVeiculo) {
    setIdDoEstado(idVeiculo);
    setRecusas([]);
    setFotoParaRemover(null);
  }

  // Inclui as fotos já gravadas no lote que a lista ainda não trouxe: contador, vagas e grade seguem o mesmo número.
  const gravadas = useMemo(() => fotosParaExibir(consulta.data ?? [], fila.itens), [consulta.data, fila.itens]);
  const naFila = fila.itens.filter((item): item is ItemNaFila => item.situacao !== 'concluida');
  const resumo = resumoFila(fila.itens);
  const total = gravadas.length + naFila.length;
  const semVagas = vagasRestantes(gravadas.length, naFila.length) === 0;
  const listaCarregando = idVeiculo !== null && consulta.isPending;
  const listaComErro = idVeiculo !== null && consulta.isError;
  const zonaBloqueada = semVagas || listaCarregando || listaComErro;
  const contadorConhecido = idVeiculo === null || consulta.isSuccess;
  const temItens = total > 0;

  /** Prévia local de uma foto que acabou de ser enviada: fica na miniatura até a imagem do servidor carregar. */
  function previaDaFoto(idVeiculoFoto: number): string | null {
    const item = fila.itens.find((itemFila) => itemFila.situacao === 'concluida' && itemFila.foto?.idVeiculoFoto === idVeiculoFoto);
    return item?.previa ?? null;
  }

  // Valores de agora, para quem espera uma chamada assíncrona (a análise das fotos) e precisa do que vale depois dela.
  const agora = useRef({ idVeiculo, totalGravadas: gravadas.length });
  useEffect(() => {
    agora.current = { idVeiculo, totalGravadas: gravadas.length };
  });

  let textoStatus = '';
  if (resumo.emEnvio) textoStatus = mensagemFotoProgressoFila(resumo.atual, resumo.total);
  else if (resumo.concluidas > 0) textoStatus = MENSAGEM_FOTOS_ENVIADA;
  else if (semVagas) textoStatus = mensagemFotoLimiteAtingido(MAXIMO_FOTOS_POR_VEICULO);

  function abrirSeletor() {
    if (!zonaBloqueada) refEntrada.current?.click();
  }

  async function receberArquivos(arquivos: readonly File[]) {
    if (arquivos.length === 0 || zonaBloqueada) return;
    const idDoInicio = idVeiculo;
    const analise = await analisarArquivosFoto(arquivos, {
      criarPrevia: (arquivo) => URL.createObjectURL(arquivo),
      lerCabecalho: (arquivo) => lerCabecalhoDoArquivo(arquivo),
    });
    // Depois da espera: o veículo pode ter trocado e as fotos gravadas podem ter mudado. Vale o que está agora.
    const atualDepois = agora.current;
    if (veiculoMudouDesdeAEscolha(idDoInicio, atualDepois.idVeiculo)) {
      analise.validos.forEach((valido) => URL.revokeObjectURL(valido.previa));
      return;
    }
    const excedentes = fila.adicionar(analise.validos, atualDepois.totalGravadas);
    const aceitos = analise.validos.length - excedentes.length;
    const mensagemSemVaga = aceitos === 0 ? mensagemFotoLimiteAtingido(MAXIMO_FOTOS_POR_VEICULO) : mensagemFotoSemVaga(aceitos);
    setRecusas([
      ...analise.recusas,
      ...excedentes.map((excedente) => ({ nome: excedente.arquivo.name, mensagem: mensagemSemVaga })),
    ]);
  }

  function aoMudarEntrada(evento: ChangeEvent<HTMLInputElement>) {
    const arquivos = Array.from(evento.target.files ?? []);
    evento.target.value = '';
    void receberArquivos(arquivos);
  }

  function aoArrastarSobre(evento: DragEvent<HTMLButtonElement>) {
    if (zonaBloqueada) return;
    evento.preventDefault();
    setArrastando(true);
  }

  function aoSoltar(evento: DragEvent<HTMLButtonElement>) {
    evento.preventDefault();
    setArrastando(false);
    if (zonaBloqueada) return;
    void receberArquivos(Array.from(evento.dataTransfer.files));
  }

  function fecharConfirmacao() {
    if (remocao.isPending) return;
    setFotoParaRemover(null);
    remocao.reset();
  }

  function confirmarRemocao() {
    if (fotoParaRemover === null || idVeiculo === null) return;
    const idVeiculoFoto = fotoParaRemover.idVeiculoFoto;
    remocao.mutate(
      { idVeiculo, idVeiculoFoto },
      {
        onSuccess: () => {
          // Foto recém-gravada no lote ainda está na fila como concluída: sai dela também, senão a miniatura volta.
          fila.descartarGravada(idVeiculoFoto);
          setFotoParaRemover(null);
          aviso.sucesso(MENSAGEM_FOTOS_REMOVIDA);
        },
      },
    );
  }

  return (
    <section
      aria-labelledby={idTitulo}
      className="flex flex-col gap-4 rounded-cartao bg-branco p-6 shadow-sm"
    >
      <input
        ref={refEntrada}
        type="file"
        accept="image/png, image/jpeg"
        multiple
        tabIndex={-1}
        aria-hidden="true"
        disabled={zonaBloqueada}
        onChange={aoMudarEntrada}
        className="sr-only"
      />

      <div className="flex items-center justify-between gap-x-4 pb-1">
        <div className="flex min-w-0 items-center gap-2">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded bg-steel-alto text-navy-profundo">
            <Icone nome="photo_library" tamanho={18} />
          </span>
          <h2 id={idTitulo} className="text-nome-veiculo font-bold tracking-tight text-texto-desenho">
            {MENSAGEM_FOTOS_TITULO}
          </h2>
        </div>
        {contadorConhecido && (
          <span className="shrink-0 whitespace-nowrap rounded-full bg-steel-suave px-2 py-0.5 text-micro font-bold text-texto-apoio">
            {formatarContadorFotos(total)}
          </span>
        )}
      </div>

      <p className="text-apoio text-texto-apoio">{MENSAGEM_FOTOS_APOIO}</p>

      <button
        type="button"
        disabled={zonaBloqueada}
        aria-label={MENSAGEM_FOTOS_ZONA_ROTULO}
        onClick={abrirSeletor}
        onDragOver={aoArrastarSobre}
        onDragLeave={() => setArrastando(false)}
        onDrop={aoSoltar}
        className={`group flex w-full flex-col items-center justify-center rounded-cartao p-6 text-center outline-dashed outline-2 outline-contorno-suave focus-visible:outline-orange-acao ${
          zonaBloqueada
            ? 'cursor-not-allowed opacity-60'
            : 'cursor-pointer hover:bg-steel-suave hover:outline-orange-acao'
        } ${arrastando ? 'bg-steel-suave outline-orange-acao' : 'bg-steel-fundo'}`}
      >
        <span className="mb-2 flex h-12 w-12 items-center justify-center rounded-xl bg-branco text-status-pendente shadow-sm transition-transform group-hover:scale-110">
          <Icone nome="cloud_upload" tamanho={28} />
        </span>
        <span className="text-corpo-denso font-semibold text-texto-desenho">{MENSAGEM_FOTOS_ARRASTE}</span>
        <span className="mt-0.5 text-legenda text-texto-apoio">{MENSAGEM_FOTOS_CLIQUE}</span>
        <span className="mt-2 flex items-center gap-1 text-micro text-texto-apoio">
          <Icone nome="info" tamanho={14} />
          <span>{MENSAGEM_FOTOS_FORMATO}</span>
        </span>
      </button>

      {recusas.length > 0 && (
        <ul role="alert" className="flex flex-col gap-1 rounded bg-status-perigo-fundo p-2 text-legenda text-status-perigo">
          {recusas.map((recusa, indice) => (
            <li key={`${recusa.nome}-${indice}`} className="break-words">
              <span className="font-semibold">{recusa.nome}</span> {recusa.mensagem}
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-col gap-2 pt-1">
        <p id={idSelecionadas} className="text-micro font-semibold uppercase tracking-wider text-contorno">
          {MENSAGEM_FOTOS_SELECIONADAS}
        </p>

        {listaCarregando && <p className="text-corpo-denso text-texto-suave">{MENSAGEM_CARREGANDO}</p>}

        {listaComErro && (
          <div role="alert" className="flex flex-col items-start gap-2">
            <p className="text-corpo-denso font-semibold text-status-perigo">{MENSAGEM_FOTOS_ERRO_CARREGAR}</p>
            <p className="text-legenda text-texto-suave">{obterMensagemErro(consulta.error)}</p>
            <Botao
              variante="contorno"
              tamanho="pequeno"
              icone="refresh"
              carregando={consulta.isFetching}
              onClick={() => void consulta.refetch()}
            >
              {MENSAGEM_FOTOS_TENTAR_DE_NOVO}
            </Botao>
          </div>
        )}

        {!listaCarregando && !listaComErro && !temItens && (
          <p className="text-corpo-denso text-texto-suave">{MENSAGEM_FOTOS_VAZIO}</p>
        )}

        {!listaCarregando && !listaComErro && temItens && (
          <ul aria-labelledby={idSelecionadas} className="grid grid-cols-2 gap-2">
            {gravadas.map((foto, indice) => {
              // Foto gravada sem arquivo nesta máquina: o item fica no lugar dele, em erro, com o texto do servidor e remover.
              const erroDaFoto = erroDaFotoGravada(foto);
              if (erroDaFoto !== null) {
                return (
                  <MiniaturaFotoVeiculo
                    key={`gravada-${foto.idVeiculoFoto}`}
                    posicao={indice + 1}
                    url={null}
                    alt={mensagemFotoAlt(descricaoVeiculo)}
                    estado="erro"
                    principal={indice === 0}
                    mensagemErro={erroDaFoto}
                    removerSempreVisivel
                    aoRemover={() => setFotoParaRemover(foto)}
                  />
                );
              }
              return (
                <MiniaturaFotoVeiculo
                  key={`gravada-${foto.idVeiculoFoto}`}
                  posicao={indice + 1}
                  url={urlDaImagem(foto)}
                  alt={mensagemFotoAlt(descricaoVeiculo)}
                  estado="gravada"
                  principal={indice === 0}
                  previaAteCarregar={previaDaFoto(foto.idVeiculoFoto)}
                  aoRemover={() => setFotoParaRemover(foto)}
                />
              );
            })}
            {naFila.map((item, indice) => (
              <MiniaturaFotoVeiculo
                key={`fila-${item.chave}`}
                posicao={gravadas.length + indice + 1}
                url={item.previa}
                alt={mensagemFotoAlt(descricaoVeiculo)}
                estado={item.situacao}
                percentual={item.percentual}
                mensagemErro={item.erro}
                aoRemover={() => fila.remover(item.chave)}
                aoTentarDeNovo={() => fila.tentarDeNovo(item.chave)}
              />
            ))}
          </ul>
        )}
      </div>

      <output aria-live="polite" className="block min-h-4 text-legenda text-texto-suave">
        {textoStatus}
      </output>

      <button
        type="button"
        onClick={abrirSeletor}
        disabled={zonaBloqueada}
        className="mt-1 flex h-10 w-full items-center justify-center gap-1 rounded bg-steel-suave text-corpo-denso font-semibold text-texto-desenho transition-colors hover:bg-steel-alto disabled:cursor-not-allowed disabled:opacity-60"
      >
        <Icone nome="add_photo_alternate" tamanho={18} />
        {MENSAGEM_FOTOS_ADICIONAR_MAIS}
      </button>

      <Modal
        aberto={fotoParaRemover !== null}
        aoFechar={fecharConfirmacao}
        titulo={MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_TITULO}
        descricao={MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_DESCRICAO}
        tamanho="pequeno"
        rodape={
          <>
            <Botao variante="contorno" disabled={remocao.isPending} onClick={fecharConfirmacao}>
              {MENSAGEM_FOTOS_CONFIRMAR_CANCELAR}
            </Botao>
            <Botao variante="perigo" carregando={remocao.isPending} onClick={confirmarRemocao}>
              {MENSAGEM_FOTOS_CONFIRMAR_REMOVER}
            </Botao>
          </>
        }
      >
        {remocao.isError && <p role="alert" className="text-corpo-denso text-status-perigo">{MENSAGEM_FOTOS_ERRO_REMOVER}</p>}
      </Modal>
    </section>
  );
}
