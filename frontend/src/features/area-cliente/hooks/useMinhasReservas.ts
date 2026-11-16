import { useQuery, type QueryKey, type UseQueryResult } from '@tanstack/react-query';
import { listarReservas } from '@/api/reservas';
import { montarFiltroContagem } from '@/features/area-cliente/reservas/parametrosReservas';
import type { ErroApi } from '@/types/api';
import type { FiltroReservas, RespostaReservas, SituacaoReserva } from '@/types/reservas';

/** Família de chaves das reservas: as mutações invalidam esta raiz para atualizar listas e contadores. */
export const CHAVE_RESERVAS = 'reservas';

/**
 * Lista de uma aba. Trocar de página ou de busca mantém a lista anterior na tela até a nova chegar. Trocar de aba
 * não mantém: a lista de outra situação nunca aparece na aba nova. Erros 4xx não são repetidos (regra do QueryClient).
 */
export function useReservas(filtro: FiltroReservas): UseQueryResult<RespostaReservas, ErroApi | Error> {
  return useQuery<RespostaReservas, ErroApi | Error>({
    queryKey: [CHAVE_RESERVAS, 'lista', filtro],
    queryFn: () => listarReservas(filtro),
    placeholderData: (dadosAnteriores, consultaAnterior) => {
      const chaveAnterior: QueryKey | undefined = consultaAnterior?.queryKey;
      const situacaoAnterior = (chaveAnterior?.[2] as FiltroReservas | undefined)?.situacao;
      return situacaoAnterior === filtro.situacao ? dadosAnteriores : undefined;
    },
  });
}

/** Contador de uma aba: consulta de uma linha só, para o `total` (a aba ativa também usa a própria lista). */
export function useTotalReservas(situacao: SituacaoReserva): UseQueryResult<RespostaReservas, ErroApi | Error> {
  return useQuery<RespostaReservas, ErroApi | Error>({
    queryKey: [CHAVE_RESERVAS, 'total', situacao],
    queryFn: () => listarReservas(montarFiltroContagem(situacao)),
  });
}
