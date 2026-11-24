import { useRef, useState, type FormEvent } from 'react';
import { generatePath, useNavigate, useParams } from 'react-router-dom';
import { Botao, BotaoLink, EstadoErro, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { ErroApi } from '@/types/api';
import type { ContextoRetirada } from '@/types/retirada';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { BarraAcaoRetirada } from '../retirada/componentes/BarraAcaoRetirada';
import { ConferenciaRetirada } from '../retirada/componentes/ConferenciaRetirada';
import { PassoAPassoRetirada } from '../retirada/componentes/PassoAPassoRetirada';
import { ResumoReservaRetirada } from '../retirada/componentes/ResumoReservaRetirada';
import { TrilhaRetirada } from '../retirada/componentes/TrilhaRetirada';
import { botaoLigado, passosDaRetirada, prontoParaDespacho } from '../retirada/regrasRetirada';
import { esquemaConferenciaRetirada, lerIdReserva, retiradaDisponivel, SEM_CONFERENCIA, type ConferenciaRetirada as ValoresConferencia, type ItemConferencia } from '../retirada/validacaoRetirada';

export interface ConteudoRetiradaProps {
  contexto: ContextoRetirada;
  /** Consulta atualizando: bloqueia a conferência e o botão. */
  atualizando: boolean;
  /** Estado inicial da conferência (padrão: nada marcado). A tela não passa nada; os testes injetam o estado. */
  valoresIniciais?: ValoresConferencia;
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
 * fixa, depende dos três itens marcados. A página o recria por reserva (key), então a conferência começa desmarcada.
 */
export function ConteudoRetirada({ contexto, atualizando, valoresIniciais = SEM_CONFERENCIA }: ConteudoRetiradaProps) {
  const registro = useAbrirLocacao(contexto.idReserva);
  const navegar = useNavigate();
  const aviso = useAviso();
  const usuario = useAutenticacao().usuario;
  const enviando = useRef(false);
  const [valores, setValores] = useState<ValoresConferencia>(valoresIniciais);
  const disponivel = retiradaDisponivel(contexto);
  const idLocacaoJaAberta = contexto.idLocacaoExistente;
  const ocupado = atualizando || registro.isPending;
  const ligado = botaoLigado(valores, disponivel, ocupado);
  const pronto = prontoParaDespacho(valores, disponivel);

  function alternar(item: ItemConferencia) {
    setValores((anterior) => ({ ...anterior, [item]: !anterior[item] }));
  }

  async function enviar() {
    if (enviando.current || !ligado || !esquemaConferenciaRetirada.safeParse(valores).success) return;
    enviando.current = true;
    try {
      const locacao = await registro.mutateAsync();
      aviso.sucesso(M.sucesso);
      navegar(generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(locacao.idLocacao) }));
    } catch (erro: unknown) { aviso.erro(obterMensagemErro(erro)); }
    finally { enviando.current = false; }
  }

  function aoEnviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    void enviar();
  }

  const alertaErro = registro.isError ? (
    <div role="alert" className="space-y-1 text-corpo-denso text-status-perigo">
      <p>{obterMensagemErro(registro.error)}</p>
      <p>{registro.error instanceof ErroApi && registro.error.status === 409 ? M.conflito : M.recuperar}</p>
    </div>
  ) : null;

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

export default function RetiradaVeiculoPagina() {
  const { reservaId } = useParams<'reservaId'>();
  const id = lerIdReserva(reservaId);
  const consulta = useRetirada(id);
  const contexto = consulta.data;
  // 404 (reserva inexistente, desativada ou de outra locadora): repetir a consulta não resolve, então só fica o voltar.
  const naoEncontrada = consulta.error instanceof ErroApi && consulta.error.status === 404;
  useTituloDocumento(M.titulo);
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <TrilhaRetirada />
        <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="space-y-1">
            <h1 className="text-titulo-acesso font-bold tracking-tight text-texto-desenho">{M.titulo}</h1>
            <p className="text-corpo-denso text-texto-apoio">{M.subtitulo}</p>
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
        : contexto && <ConteudoRetirada key={id} contexto={contexto} atualizando={consulta.isFetching} />}
    </div>
  );
}
