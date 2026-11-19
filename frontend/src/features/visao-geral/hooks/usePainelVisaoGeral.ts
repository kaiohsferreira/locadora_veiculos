import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarVisaoGeral } from '@/api/painel';
import type { ErroApi } from '@/types/api';
import type { PeriodoVisaoGeral, RespostaVisaoGeral } from '@/types/painel';
import { CHAVE_VISAO_GERAL } from '../chaveVisaoGeral';

/**
 * Indicadores da visão geral para um período. Ao trocar de período, mantém os números anteriores na tela até chegar o
 * novo (a página não volta a carregar inteira). Erros 4xx não são repetidos automaticamente (regra do QueryClient).
 */
export function usePainelVisaoGeral(periodo: PeriodoVisaoGeral): UseQueryResult<RespostaVisaoGeral, ErroApi | Error> {
  return useQuery<RespostaVisaoGeral, ErroApi | Error>({
    queryKey: [CHAVE_VISAO_GERAL, periodo],
    queryFn: () => buscarVisaoGeral(periodo),
    placeholderData: keepPreviousData,
  });
}
