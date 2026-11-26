/**
 * Chamada das locações do cliente (contrato locacao-cliente: GET /api/locacoes/minhas). Só o cliente logado recebe
 * a lista; o servidor decide quais locações entram. A tela acessa esta função pelos hooks do TanStack Query.
 */
import { api } from './client';
import type { FiltroLocacoes, RespostaLocacoes } from '@/types/locacoesCliente';

const CAMINHO_MINHAS_LOCACOES = '/api/locacoes/minhas';

/** Parâmetros da query de GET /api/locacoes/minhas. */
export type ParametrosListagemLocacoes = Record<string, string | number>;

/** Monta a query da lista: página, tamanho e situação (EM_ANDAMENTO ou HISTORICO). */
export function montarParametrosListagemLocacoes(filtro: FiltroLocacoes): ParametrosListagemLocacoes {
  return {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
    situacao: filtro.situacao,
  };
}

/** Resposta vazia ou sem lista vira página vazia, para a tela não quebrar. */
export function normalizarRespostaLocacoes(corpo: unknown, filtro: FiltroLocacoes): RespostaLocacoes {
  const objeto = corpo as Partial<RespostaLocacoes> | null | undefined;
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

/** Lista paginada das locações do cliente (200). Erros chegam como ErroApi e propagam. */
export async function listarMinhasLocacoes(filtro: FiltroLocacoes): Promise<RespostaLocacoes> {
  const resposta = await api.get<RespostaLocacoes>(CAMINHO_MINHAS_LOCACOES, {
    params: montarParametrosListagemLocacoes(filtro),
  });
  return normalizarRespostaLocacoes(resposta.data, filtro);
}
