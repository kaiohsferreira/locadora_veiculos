/**
 * Chamadas das reservas do cliente (contrato reserva: GET /api/reservas e DELETE /api/reservas/{id}). A tela só usa
 * estas duas rotas; GET por id e POST não são usados aqui. As telas acessam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type { FiltroReservas, RespostaReservas } from '@/types/reservas';

const CAMINHO_RESERVAS = '/api/reservas';

/** Parâmetros da query de GET /api/reservas. */
export type ParametrosListagemReservas = Record<string, string | number>;

/**
 * Monta a query da lista. Busca vazia ou só com espaços não é enviada; a busca sai aparada.
 * (O corte em 60 caracteres fica em parametrosReservas, antes de chegar aqui.)
 */
export function montarParametrosListagemReservas(filtro: FiltroReservas): ParametrosListagemReservas {
  const parametros: ParametrosListagemReservas = {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
    situacao: filtro.situacao,
  };
  const busca = filtro.busca?.trim();
  if (busca) parametros.busca = busca;
  return parametros;
}

/** Resposta vazia ou sem lista vira página vazia, para a tela não quebrar. */
function normalizarResposta(corpo: unknown, filtro: FiltroReservas): RespostaReservas {
  const objeto = corpo as Partial<RespostaReservas> | null | undefined;
  if (objeto === null || typeof objeto !== 'object' || !Array.isArray(objeto.itens)) {
    return { itens: [], total: 0, pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina };
  }
  return {
    itens: objeto.itens,
    total: typeof objeto.total === 'number' ? objeto.total : objeto.itens.length,
    pagina: typeof objeto.pagina === 'number' ? objeto.pagina : filtro.pagina,
    tamanhoPagina: typeof objeto.tamanhoPagina === 'number' ? objeto.tamanhoPagina : filtro.tamanhoPagina,
  };
}

/** Lista paginada das reservas do cliente (200). Erros chegam como ErroApi e propagam. */
export async function listarReservas(filtro: FiltroReservas): Promise<RespostaReservas> {
  const resposta = await api.get<RespostaReservas>(CAMINHO_RESERVAS, {
    params: montarParametrosListagemReservas(filtro),
  });
  return normalizarResposta(resposta.data, filtro);
}

/** Cancela a reserva do cliente (204). Cancelar de novo uma CANCELADA não altera nada; CONCLUIDA responde 409. */
export async function cancelarReserva(idReserva: number): Promise<void> {
  await api.delete(`${CAMINHO_RESERVAS}/${idReserva}`);
}
