import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarMeusPagamentos, obterResumoMeusPagamentos } from '@/api/pagamentosCliente';
import type { ErroApi } from '@/types/api';
import type {
  FiltroMeusPagamentos,
  PeriodoPagamentosCliente,
  RespostaMeusPagamentos,
  ResumoMeusPagamentos,
} from '@/types/pagamentos';

/** Família de chaves das cobranças do cliente. */
export const CHAVE_PAGAMENTOS_CLIENTE = 'pagamentos-cliente';

/** Lista paginada das cobranças do cliente (aba, período e página no filtro). */
export function useMeusPagamentos(
  filtro: FiltroMeusPagamentos,
): UseQueryResult<RespostaMeusPagamentos, ErroApi | Error> {
  return useQuery<RespostaMeusPagamentos, ErroApi | Error>({
    queryKey: [CHAVE_PAGAMENTOS_CLIENTE, 'lista', filtro],
    queryFn: () => listarMeusPagamentos(filtro),
  });
}

/**
 * Resumo do cliente (cartões, faixa e contagens das abas). Trocar de período mantém o resumo anterior na tela até o
 * novo chegar, para os cartões não piscarem.
 */
export function useResumoMeusPagamentos(
  periodo: PeriodoPagamentosCliente,
): UseQueryResult<ResumoMeusPagamentos, ErroApi | Error> {
  return useQuery<ResumoMeusPagamentos, ErroApi | Error>({
    queryKey: [CHAVE_PAGAMENTOS_CLIENTE, 'resumo', periodo],
    queryFn: () => obterResumoMeusPagamentos(periodo),
    placeholderData: keepPreviousData,
  });
}
