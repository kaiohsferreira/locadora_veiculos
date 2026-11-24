import { useRef, useState, type FormEvent } from 'react';
import { generatePath, useNavigate, useParams } from 'react-router-dom';
import { Botao, BotaoLink, EstadoErro, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { ErroApi } from '@/types/api';
import type { ContextoRetirada, DadosCaucaoAbertura } from '@/types/retirada';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { BarraAcaoRetirada } from '../retirada/componentes/BarraAcaoRetirada';
import { ConferenciaRetirada, type CaucaoConferenciaProps } from '../retirada/componentes/ConferenciaRetirada';
import { PassoAPassoRetirada } from '../retirada/componentes/PassoAPassoRetirada';
import { ResumoReservaRetirada } from '../retirada/componentes/ResumoReservaRetirada';
import { TrilhaRetirada } from '../retirada/componentes/TrilhaRetirada';
import {
  dadosDaCaucaoParaAbrir,
  estadoCaucaoNaTela,
  memoriaComEscolha,
  memoriaComTentativa,
  memoriaDaRetirada,
  situacaoDaPreAutorizacao,
  statusDoErro,
  tokenDaTentativa,
  type EscolhaSimulacaoCaucao,
  type MemoriaCaucaoRetirada,
  type TentativaCaucao,
} from '../retirada/regrasCaucaoRetirada';
import { botaoLigado, complementoErroAbertura, passosDaRetirada, prontoParaDespacho, subtituloDaRetirada } from '../retirada/regrasRetirada';
import {
  esquemaConferenciaRetirada,
  itensDaConferencia,
  lerIdReserva,
  retiradaDisponivel,
  SEM_CONFERENCIA,
  type ConferenciaRetirada as ValoresConferencia,
  type ItemConferencia,
} from '../retirada/validacaoRetirada';

export interface ConteudoRetiradaProps {
  contexto: ContextoRetirada;
  /** Consulta atualizando: bloqueia a conferência e o botão. */
  atualizando: boolean;
  /** Estado inicial da conferência (padrão: nada marcado). A tela não passa nada; os testes injetam o estado. */
  valoresIniciais?: ValoresConferencia;
  /**
   * Saída escolhida na simulação da caução (nula até o atendente escolher). Vem da página, que não desmonta: continua
   * marcada depois de um erro, mesmo quando este conteúdo é recriado.
   */
  escolha: EscolhaSimulacaoCaucao | null;
  aoEscolher: (escolha: EscolhaSimulacaoCaucao) => void;
  /**
   * Última tentativa com caução que falhou: decide se a próxima repete o token (só sem resposta definitiva). Vem da página,
   * pelo mesmo motivo da escolha.
   */
  tentativaAnterior: TentativaCaucao | null;
  aoRegistrarTentativa: (tentativa: TentativaCaucao) => void;
}

/** Locação já aberta para esta reserva: o cartão no lugar da conferência, sem barra. */
function LocacaoJaIniciada({ idLocacao }: { idLocacao: number }) {
  return (
    <section aria-labelledby="titulo-locacao-iniciada" className="flex flex-col gap-4 overflow-hidden rounded-cartao bg-branco p-6 shadow-sm">
      <h2 id="titulo-locacao-iniciada" className="text-titulo-md font-bold text-texto-desenho">{M.locacaoJaIniciada}</h2>
      <output className="block text-corpo-denso text-texto-apoio">{M.existente}</output>
      <div>
        <BotaoLink to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(idLocacao) })} iconeDireita="arrow_forward">
          {M.continuarVistoria}
        </BotaoLink>
      </div>
    </section>
  );
}

/**
 * Conteúdo da tela quando os dados chegaram. A conferência fica aqui (e não na página) porque o botão principal, na barra
 * fixa, depende dos itens marcados e, com a caução exigida, da saída da simulação e do cargo da sessão. A página o recria
 * por reserva (key), então a conferência começa desmarcada; a saída escolhida e a última tentativa vêm da página.
 */
export function ConteudoRetirada({
  contexto, atualizando, valoresIniciais = SEM_CONFERENCIA, escolha, aoEscolher, tentativaAnterior, aoRegistrarTentativa,
}: ConteudoRetiradaProps) {
  const registro = useAbrirLocacao(contexto.idReserva);
  const navegar = useNavigate();
  const aviso = useAviso();
  const usuario = useAutenticacao().usuario;
  // Cargo gravado na sessão no login (nulo sem cargo): com a caução exigida, só ATENDENTE e ADMINISTRADOR iniciam (A1).
  const { cargo } = usePermissoes();
  const enviando = useRef(false);
  const [valores, setValores] = useState<ValoresConferencia>(valoresIniciais);
  const disponivel = retiradaDisponivel(contexto);
  const idLocacaoJaAberta = contexto.idLocacaoExistente;
  const itens = itensDaConferencia(contexto.caucaoExigida);
  const caucaoNaTela = estadoCaucaoNaTela(contexto, escolha, cargo);
  const ocupado = atualizando || registro.isPending;
  const ligado = botaoLigado(valores, disponivel, ocupado, caucaoNaTela);
  const pronto = prontoParaDespacho(valores, disponivel, caucaoNaTela);

  function alternar(item: ItemConferencia) {
    setValores((anterior) => ({ ...anterior, [item]: !anterior[item] }));
  }

  async function enviar() {
    if (enviando.current || !ligado || !esquemaConferenciaRetirada(itens).safeParse(valores).success) return;
    // Com a caução exigida (o botão só liga com uma saída escolhida), o token vem da saída: novo a cada tentativa nova e o
    // mesmo só depois de uma falha sem resposta definitiva. Sem caução, o POST leva só a reserva.
    const escolhaEnviada = caucaoNaTela.exigida ? escolha : null;
    if (caucaoNaTela.exigida && escolhaEnviada === null) return;
    const token = escolhaEnviada === null ? null : tokenDaTentativa(escolhaEnviada, tentativaAnterior);
    const dadosCaucao: DadosCaucaoAbertura | null = token === null ? null : dadosDaCaucaoParaAbrir(token);
    enviando.current = true;
    try {
      const locacao = await registro.mutateAsync(dadosCaucao);
      aviso.sucesso(M.sucesso);
      navegar(generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(locacao.idLocacao) }));
    } catch (erro: unknown) {
      // Sem aviso flutuante (ajuste A2 do Kaio, com e sem caução): o erro fica só escrito na tela, na linha com role="alert"
      // embaixo da lista. A tentativa vai para a página, que não desmonta: se a releitura falhar e este conteúdo for
      // recriado, a tentativa seguinte ainda sabe se repete o token.
      if (escolhaEnviada !== null && token !== null) {
        aoRegistrarTentativa({ escolha: escolhaEnviada, token, status: statusDoErro(erro) });
      }
    } finally { enviando.current = false; }
  }

  function aoEnviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    void enviar();
  }

  const complemento = registro.isError ? complementoErroAbertura(statusDoErro(registro.error), contexto.caucaoExigida, disponivel) : null;
  const alertaErro = registro.isError ? (
    <div role="alert" className="space-y-1 text-corpo-denso text-status-perigo">
      <p>{obterMensagemErro(registro.error)}</p>
      {complemento && <p>{complemento}</p>}
    </div>
  ) : null;

  const caucaoConferencia: CaucaoConferenciaProps | null = contexto.caucaoExigida
    ? { valorCaucaoStr: contexto.valorCaucaoStr, situacao: situacaoDaPreAutorizacao(caucaoNaTela), escolha, aoEscolher }
    : null;

  return (
    <div className="space-y-8">
      <PassoAPassoRetirada passos={passosDaRetirada(disponivel)} />
      <div className="grid items-start gap-8 xl:grid-cols-12">
        <div className="xl:col-span-5">
          <ResumoReservaRetirada contexto={contexto} />
        </div>
        <div className="xl:col-span-7">
          {idLocacaoJaAberta !== null
            ? <LocacaoJaIniciada idLocacao={idLocacaoJaAberta} />
            : <ConferenciaRetirada
                contexto={contexto}
                valores={valores}
                itens={itens}
                caucao={caucaoConferencia}
                nomeFuncionario={usuario?.nome ?? null}
                bloqueada={ocupado || !disponivel}
                ocupada={ocupado}
                indisponivel={!disponivel}
                alertaErro={alertaErro}
                aoAlternar={alternar}
                aoEnviar={aoEnviar}
              />}
        </div>
      </div>
      {idLocacaoJaAberta === null && <BarraAcaoRetirada pronto={pronto} ligado={ligado} carregando={registro.isPending} />}
    </div>
  );
}

interface ErroRetiradaProps {
  mensagem: string;
  /** Sem ele não aparece "Tentar novamente" (id inválido e reserva não encontrada: tentar de novo não resolve). */
  aoTentarNovamente?: () => void;
  tentando?: boolean;
}

/** Estado de erro da tela 16 com o botão de volta à página de entrada, abaixo e centralizado (acerto C de 24/11/2026). */
function ErroRetirada({ mensagem, aoTentarNovamente, tentando = false }: ErroRetiradaProps) {
  return (
    <div>
      <EstadoErro mensagem={mensagem} aoTentarNovamente={aoTentarNovamente} tentando={tentando} />
      <div className="flex justify-center">
        <BotaoLink to={CAMINHOS.retiradas} variante="contorno" icone="arrow_back">{M.voltarRetiradas}</BotaoLink>
      </div>
    </div>
  );
}

export interface RetiradaVeiculoPaginaProps {
  /** Memória inicial da caução (padrão: nenhuma). A rota não passa nada; os testes injetam a memória. */
  memoriaInicial?: MemoriaCaucaoRetirada | null;
}

export default function RetiradaVeiculoPagina({ memoriaInicial = null }: RetiradaVeiculoPaginaProps) {
  const { reservaId } = useParams<'reservaId'>();
  const id = lerIdReserva(reservaId);
  const consulta = useRetirada(id);
  const contexto = consulta.data;
  // Saída escolhida na simulação e última tentativa com caução: ficam aqui, e não no conteúdo, porque esta página não
  // desmonta quando a releitura falha e o conteúdo dá lugar ao estado de erro (locacao.contract.json v4,
  // tela_16_v4.repeticao_e_token). Quando a reserva da rota muda, as duas voltam a nulo (memoriaDaRetirada), no próprio
  // render, antes de a tela mostrar a reserva nova.
  const [memoriaGuardada, setMemoriaGuardada] = useState<MemoriaCaucaoRetirada>(() => memoriaDaRetirada(memoriaInicial, id));
  const memoria = memoriaDaRetirada(memoriaGuardada, id);
  if (memoria !== memoriaGuardada) setMemoriaGuardada(memoria);
  // 404 (reserva inexistente, desativada ou de outra locadora): repetir a consulta não resolve, então só fica o voltar.
  const naoEncontrada = consulta.error instanceof ErroApi && consulta.error.status === 404;
  // Subtítulo com "garantias" só quando os dados estão na tela e a caução é exigida; nos estados sem dados, o de hoje.
  const dadosNaTela = id !== null && !consulta.isPending && !consulta.isError && contexto !== undefined;
  const subtitulo = subtituloDaRetirada(dadosNaTela && contexto.caucaoExigida);
  useTituloDocumento(M.titulo);
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <TrilhaRetirada />
        <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="space-y-1">
            <h1 className="text-titulo-acesso font-bold tracking-tight text-texto-desenho">{M.titulo}</h1>
            <p className="text-corpo-denso text-texto-apoio">{subtitulo}</p>
          </div>
          {contexto && (
            <Botao variante="fantasma" icone="refresh" disabled={consulta.isFetching} onClick={() => { void consulta.refetch(); }}>
              {M.atualizarDados}
            </Botao>
          )}
        </header>
      </div>
      {id === null ? <ErroRetirada mensagem={M.idInvalido} />
        : consulta.isPending ? <CarregandoPagina />
        : consulta.isError ? (
          <ErroRetirada
            mensagem={obterMensagemErro(consulta.error)}
            aoTentarNovamente={naoEncontrada ? undefined : () => { void consulta.refetch(); }}
            tentando={consulta.isFetching}
          />
        )
        : contexto && (
          <ConteudoRetirada
            key={id}
            contexto={contexto}
            atualizando={consulta.isFetching}
            escolha={memoria.escolha}
            aoEscolher={(escolha) => setMemoriaGuardada((anterior) => memoriaComEscolha(anterior, id, escolha))}
            tentativaAnterior={memoria.tentativaAnterior}
            aoRegistrarTentativa={(tentativa) => setMemoriaGuardada((anterior) => memoriaComTentativa(anterior, id, tentativa))}
          />
        )}
    </div>
  );
}
