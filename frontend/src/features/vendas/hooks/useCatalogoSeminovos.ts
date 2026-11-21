import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarCatalogo } from '@/api/vendas';
import type { ErroApi } from '@/types/api';
import type { FiltroCatalogo, RespostaCatalogo } from '@/types/vendas';
import { chaveCatalogo } from './chavesVendas';

/**
 * Página do catálogo de seminovos com os filtros aplicados. Ao trocar de página ou de filtro, a lista anterior continua
 * na tela até a nova chegar (`keepPreviousData`), para a grade não piscar. Erros 4xx não são repetidos (regra do QueryClient).
 */
export function useCatalogoSeminovos(filtro: FiltroCatalogo): UseQueryResult<RespostaCatalogo, ErroApi | Error> {
  return useQuery<RespostaCatalogo, ErroApi | Error>({
    queryKey: chaveCatalogo(filtro),
    queryFn: () => listarCatalogo(filtro),
    placeholderData: keepPreviousData,
  });
}
