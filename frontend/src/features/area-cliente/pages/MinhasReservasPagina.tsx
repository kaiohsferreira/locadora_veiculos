import { useEffect, useRef, useState } from 'react';
import { useAviso } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { useCancelarReserva } from '@/features/area-cliente/hooks/useCancelarReserva';
import { useReservas, useTotalReservas } from '@/features/area-cliente/hooks/useMinhasReservas';
import { AbasReservas, type ContagemAba } from '@/features/area-cliente/reservas/componentes/AbasReservas';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';
import { CabecalhoReservas } from '@/features/area-cliente/reservas/componentes/CabecalhoReservas';
import { ModalCancelarReserva } from '@/features/area-cliente/reservas/componentes/ModalCancelarReserva';
import { PainelHistoricoReservas } from '@/features/area-cliente/reservas/componentes/PainelHistoricoReservas';
import { PainelReservasAtivas } from '@/features/area-cliente/reservas/componentes/PainelReservasAtivas';
import type { EstadoListaReservas } from '@/features/area-cliente/reservas/estadoLista';
import { descreverAtualizacao } from '@/features/area-cliente/reservas/atualizacao';
import {
  ATRASO_BUSCA_RESERVAS_MS,
  ID_ABA_ATIVAS,
  ID_ABA_HISTORICO,
  ID_PAINEL_RESERVAS,
  montarFiltroReservas,
  normalizarBusca,
  ultimaPagina,
} from '@/features/area-cliente/reservas/parametrosReservas';
import { obterMensagemErro } from '@/lib/erros';
import { primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import { MENSAGEM_RESERVAS_CANCELADA, MENSAGEM_RESERVAS_TITULO_PAGINA } from '@/lib/mensagens';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import type { ErroApi } from '@/types/api';
import type { ReservaCliente, RespostaReservas, SituacaoReserva } from '@/types/reservas';

/**
 * T-FE-03 · Minhas reservas (tela 29, RF-46). Mostra só as reservas do próprio cliente (o servidor decide quais),
 * em duas abas: Ativas e Próximas, e Histórico. Cancelar uma reserva ativa pede confirmação; o cancelamento
 * atualiza as abas.
 */
export default function MinhasReservasPagina() {
  useTituloDocumento(MENSAGEM_RESERVAS_TITULO_PAGINA);
  const { usuario } = useAutenticacao();
  const aviso = useAviso();
  const nomeCondutor = usuario?.nome ?? '';

  const [situacao, setSituacao] = useState<SituacaoReserva>('ATIVAS');
  const [pagina, setPagina] = useState(1);
  const [buscaDigitada, setBuscaDigitada] = useState('');
  const [buscaAplicada, setBuscaAplicada] = useState('');
  const [paraCancelar, setParaCancelar] = useState<ReservaCliente | null>(null);
  /** Momento de referência do "Atualizado hoje às ...": fixado quando a tela abre. */
  const [agora] = useState(() => new Date());
  /** Depois de cancelar, o foco vai para a aba Ativas (o cartão cancelado sai da lista). */
  const focarAbaAtivasAoFechar = useRef(false);

  const filtro = montarFiltroReservas(situacao, pagina, buscaAplicada);
  const consulta = useReservas(filtro);
  /** Imagem de capa que falha: refaz a consulta uma vez nesta visita (URL assinada nova); depois, ícone de carro. */
  const aoFalharFotoCapa = useAoFalharFotoCapa(() => void consulta.refetch());
  const totalAtivas = useTotalReservas('ATIVAS');
  const totalHistorico = useTotalReservas('HISTORICO');
  const cancelamento = useCancelarReserva();
  const dados = consulta.data;
  /** Um único aviso por resposta da aba exibida: a primeira mensagem de erro de foto que o servidor mandou. */
  const erroFotoCapa = primeiraMensagemErroFotoCapa(dados?.itens ?? []);

  // Página além da última (ex.: a última reserva da página saiu, depois de cancelar): volta para a última existente.
  // Feito durante o render, como o React indica para estado derivado. Não roda com dado de outra consulta.
  if (dados !== undefined && !consulta.isPlaceholderData) {
    const ultima = ultimaPagina(dados.total, filtro.tamanhoPagina);
    if (pagina > ultima) setPagina(ultima);
  }

  // Busca digitada espera a pausa antes de consultar (uma consulta por pausa, não por tecla).
  const buscaNormalizada = normalizarBusca(buscaDigitada) ?? '';
  useEffect(() => {
    if (buscaNormalizada === buscaAplicada) return undefined;
    const temporizador = window.setTimeout(() => {
      setBuscaAplicada(buscaNormalizada);
      setPagina(1);
    }, ATRASO_BUSCA_RESERVAS_MS);
    return () => window.clearTimeout(temporizador);
  }, [buscaNormalizada, buscaAplicada]);

  // Depois do cancelamento, o Modal devolve o foco ao botão que o abriu; esse botão some com o cartão. O efeito roda
  // depois da limpeza do Modal e põe o foco na aba Ativas.
  useEffect(() => {
    if (paraCancelar !== null || !focarAbaAtivasAoFechar.current) return;
    focarAbaAtivasAoFechar.current = false;
    document.getElementById(ID_ABA_ATIVAS)?.focus();
  }, [paraCancelar]);

  function trocarSituacao(nova: SituacaoReserva) {
    if (nova === situacao) return;
    setSituacao(nova);
    setPagina(1);
  }

  /** Filtrar na hora (botão Filtrar ou Enter): não espera a pausa da digitação. */
  function enviarBusca() {
    setBuscaAplicada(buscaNormalizada);
    setPagina(1);
  }

  function limparBusca() {
    setBuscaDigitada('');
    setBuscaAplicada('');
    setPagina(1);
  }

  function confirmarCancelamento() {
    if (paraCancelar === null) return;
    const reserva = paraCancelar;
    cancelamento.mutate(reserva.idReserva, {
      onSuccess: () => {
        aviso.sucesso(MENSAGEM_RESERVAS_CANCELADA);
        focarAbaAtivasAoFechar.current = true;
        setParaCancelar(null);
      },
      onError: (erro: ErroApi | Error) => {
        setParaCancelar(null);
        aviso.erro(obterMensagemErro(erro));
      },
    });
  }

  const estadoLista: EstadoListaReservas = {
    dados,
    carregando: consulta.isPending,
    erro: consulta.error,
    atualizando: consulta.isFetching,
    aoTentarNovamente: () => void consulta.refetch(),
  };

  const contagens: Record<SituacaoReserva, ContagemAba> = {
    ATIVAS: contagemDe(totalAtivas.data, totalAtivas.isError),
    HISTORICO: contagemDe(totalHistorico.data, totalHistorico.isError),
  };

  return (
    <div className="relative overflow-hidden">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-32 right-1/4 h-[500px] w-[500px] rounded-full bg-orange/5 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute left-10 top-48 h-[420px] w-[420px] rounded-full bg-steel-alto/60 blur-3xl"
      />

      <div className="relative z-10 mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
        <CabecalhoReservas nome={nomeCondutor} />

        <AbasReservas
          situacao={situacao}
          aoMudar={trocarSituacao}
          contagens={contagens}
          atualizacao={descreverAtualizacao(consulta.dataUpdatedAt, agora)}
          atualizando={consulta.isFetching}
        />

        <div
          role="tabpanel"
          id={ID_PAINEL_RESERVAS}
          aria-labelledby={situacao === 'ATIVAS' ? ID_ABA_ATIVAS : ID_ABA_HISTORICO}
        >
          {erroFotoCapa !== null && <AvisoFotosReservas mensagem={erroFotoCapa} />}
          {situacao === 'ATIVAS' ? (
            <PainelReservasAtivas
              estado={estadoLista}
              pagina={pagina}
              aoMudarPagina={setPagina}
              nomeCondutor={nomeCondutor}
              aoCancelar={setParaCancelar}
              aoFalharFotoCapa={aoFalharFotoCapa}
            />
          ) : (
            <PainelHistoricoReservas
              estado={estadoLista}
              pagina={pagina}
              aoMudarPagina={setPagina}
              buscaDigitada={buscaDigitada}
              buscaAplicada={buscaAplicada}
              aoMudarBusca={setBuscaDigitada}
              aoEnviarBusca={enviarBusca}
              aoLimparBusca={limparBusca}
              aoFalharFotoCapa={aoFalharFotoCapa}
            />
          )}
        </div>

        <ModalCancelarReserva
          reserva={paraCancelar}
          pendente={cancelamento.isPending}
          aoConfirmar={confirmarCancelamento}
          aoFechar={() => {
            if (!cancelamento.isPending) setParaCancelar(null);
          }}
        />
      </div>
    </div>
  );
}

/** Contador de uma aba: o total quando chega; "indisponível" se a consulta falhou; senão, carregando. */
function contagemDe(total: RespostaReservas | undefined, falhou: boolean): ContagemAba {
  if (total !== undefined) return total.total;
  return falhou ? 'indisponivel' : 'carregando';
}
