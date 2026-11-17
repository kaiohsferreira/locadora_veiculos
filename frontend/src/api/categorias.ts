/**
 * Chamadas das categorias de veículo (contrato categoria.contract.json v3, GET anônimo e paginado). O seletor de
 * categoria da tela de veículo usa esta lista pelo hook do TanStack Query.
 */
import { api } from './client';
import type { Pagina } from '@/types/api';
import type { CategoriaVeiculo } from '@/types/categorias';

const CAMINHO_CATEGORIAS = '/api/categorias';

/** Primeira página: o seletor mostra as categorias ativas de uma vez. */
export const PAGINA_CATEGORIAS_SELETOR = 1;

/** Teto da API para tamanho de página (categoria.contract.json: tamanhoPagina de 1 a 100). */
export const TAMANHO_PAGINA_CATEGORIAS_SELETOR = 100;

/** Categorias ativas, em ordem de nome (200 PaginaVO<ReturnCategoriaVO>). */
export async function listarCategorias(): Promise<Pagina<CategoriaVeiculo>> {
  const resposta = await api.get<Pagina<CategoriaVeiculo>>(CAMINHO_CATEGORIAS, {
    params: {
      pagina: PAGINA_CATEGORIAS_SELETOR,
      tamanhoPagina: TAMANHO_PAGINA_CATEGORIAS_SELETOR,
    },
  });
  return resposta.data;
}
