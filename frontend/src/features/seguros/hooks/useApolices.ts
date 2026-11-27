import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarApolice, listarApolices } from '@/api/seguros';
import type { ErroApi } from '@/types/api';
import type { Apolice, RespostaApolices, FiltroApolices } from '@/types/seguros';
import { chaveApolice, chaveApolices, chaveTotalApolices } from './chavesSeguros';

/** Uma linha basta para saber o total de um indicador. */
const TAMANHO_CONSULTA_TOTAL = 1;

/** Página de apólices com os filtros. A página anterior continua na tela até a nova chegar. */
export function useApolices(filtro: FiltroApolices): UseQueryResult<RespostaApolices, ErroApi | Error> {
  return useQuery<RespostaApolices, ErroApi | Error>({
    queryKey: chaveApolices(filtro),
    queryFn: () => listarApolices(filtro),
    placeholderData: keepPreviousData,
  });
}

/**
 * Total de um indicador, sem filtro de veículo: "Apólices cadastradas" (todas, inclusive as vencidas) ou "Vencem em até
 * 60 dias". Consulta de uma linha só, para o `total`.
 */
export function useTotalApolices(apenasAVencer: boolean): UseQueryResult<RespostaApolices, ErroApi | Error> {
  return useQuery<RespostaApolices, ErroApi | Error>({
    queryKey: chaveTotalApolices(apenasAVencer),
    queryFn: () => listarApolices({ pagina: 1, tamanhoPagina: TAMANHO_CONSULTA_TOTAL, apenasAVencer }),
  });
}

/** Uma apólice pelo id, para abrir a edição com os dados atuais. Sem id, a consulta não roda. */
export function useApolice(idSeguro: number | null): UseQueryResult<Apolice, ErroApi | Error> {
  return useQuery<Apolice, ErroApi | Error>({
    queryKey: chaveApolice(idSeguro),
    queryFn: () => buscarApolice(idSeguro as number),
    enabled: idSeguro !== null,
  });
}
