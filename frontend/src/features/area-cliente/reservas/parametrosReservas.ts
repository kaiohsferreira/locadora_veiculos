import type { FiltroReservas, SituacaoReserva } from '@/types/reservas';

/** Itens por página da aba Ativas e Próximas (cartões grandes). */
export const TAMANHO_PAGINA_ATIVAS = 5;
/** Itens por página da aba Histórico (linhas de tabela). */
export const TAMANHO_PAGINA_HISTORICO = 10;
/** Pausa depois de digitar na busca do histórico, antes de consultar. */
export const ATRASO_BUSCA_RESERVAS_MS = 400;
/** Mesmo limite do contrato (ConstantsValues.TamanhoMaximoBuscaReserva). */
export const TAMANHO_MAXIMO_BUSCA_RESERVAS = 60;

export const ID_ABA_ATIVAS = 'reservas-aba-ativas';
export const ID_ABA_HISTORICO = 'reservas-aba-historico';
export const ID_PAINEL_RESERVAS = 'reservas-painel';

/** Busca aparada e cortada no limite. Vazia vira undefined (o parâmetro não é enviado). */
export function normalizarBusca(texto: string): string | undefined {
  const aparada = texto.trim().slice(0, TAMANHO_MAXIMO_BUSCA_RESERVAS).trim();
  return aparada === '' ? undefined : aparada;
}

export function tamanhoPaginaDe(situacao: SituacaoReserva): number {
  return situacao === 'ATIVAS' ? TAMANHO_PAGINA_ATIVAS : TAMANHO_PAGINA_HISTORICO;
}

/**
 * Filtro da lista de uma aba. A busca só vale no histórico: na aba de ativas ela não é enviada,
 * mesmo que tenha ficado preenchida.
 */
export function montarFiltroReservas(situacao: SituacaoReserva, pagina: number, busca: string): FiltroReservas {
  const filtro: FiltroReservas = { pagina, tamanhoPagina: tamanhoPaginaDe(situacao), situacao };
  if (situacao === 'HISTORICO') {
    const termo = normalizarBusca(busca);
    if (termo !== undefined) filtro.busca = termo;
  }
  return filtro;
}

/** Consulta de contagem de uma aba: uma linha só, que traz o `total`. */
export function montarFiltroContagem(situacao: SituacaoReserva): FiltroReservas {
  return { pagina: 1, tamanhoPagina: 1, situacao };
}

/** Última página existente (mínimo 1). Usada para voltar quando a página atual ficou além do fim. */
export function ultimaPagina(total: number, tamanhoPagina: number): number {
  if (total <= 0 || tamanhoPagina <= 0) return 1;
  return Math.ceil(total / tamanhoPagina);
}
