import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query';
import { baixarPagamento } from '@/api/pagamentos';
import { buscarIndicadoresFinanceiros, listarTitulosFinanceiros } from '@/api/painelFinanceiro';
import type { ErroApi } from '@/types/api';
import type {
  BaixarPagamentoRequisicao,
  FiltroTitulosFinanceiros,
  RespostaBaixaPagamento,
  RespostaIndicadoresFinanceiros,
  RespostaTitulosFinanceiros,
} from '@/types/painelFinanceiro';
import {
  chaveFamiliaPainelFinanceiro,
  chaveIndicadoresFinanceiros,
  chaveTitulosFinanceiros,
} from '@/features/financeiro/hooks/chavesPainelFinanceiro';
import { chaveFamiliaVisaoGeral } from '@/features/visao-geral/chaveVisaoGeral';

/** Indicadores do mês corrente (o servidor escolhe o período). */
export function useIndicadoresFinanceiros(): UseQueryResult<RespostaIndicadoresFinanceiros, ErroApi | Error> {
  return useQuery<RespostaIndicadoresFinanceiros, ErroApi | Error>({
    queryKey: chaveIndicadoresFinanceiros(),
    queryFn: buscarIndicadoresFinanceiros,
  });
}

/**
 * Página da tabela para a aba pedida. Ao trocar de aba ou de página, mantém a tabela anterior na tela até a nova chegar.
 */
export function useTitulosFinanceiros(
  filtro: FiltroTitulosFinanceiros,
): UseQueryResult<RespostaTitulosFinanceiros, ErroApi | Error> {
  return useQuery<RespostaTitulosFinanceiros, ErroApi | Error>({
    queryKey: chaveTitulosFinanceiros(filtro),
    queryFn: () => listarTitulosFinanceiros(filtro),
    placeholderData: keepPreviousData,
  });
}

/** Alvo da invalidação: o QueryClient real, ou um dublê nos testes (só `invalidateQueries` é usado). */
export interface AlvoInvalidacaoPainelFinanceiro {
  invalidateQueries: (filtro: { queryKey: readonly unknown[] }) => Promise<unknown>;
}

/**
 * Recarrega os indicadores e a tabela: invalida a raiz da família do painel. Não espera a recarga terminar, e não há
 * atualização por tempo.
 */
export function invalidarPainelFinanceiro(alvo: AlvoInvalidacaoPainelFinanceiro): void {
  void alvo.invalidateQueries({ queryKey: chaveFamiliaPainelFinanceiro() });
}

/** Variáveis da baixa: a cobrança e o corpo (forma e confirmação do PIX). */
export interface VariaveisBaixarPagamento {
  idPagamento: number;
  requisicao: BaixarPagamentoRequisicao;
}

/**
 * Dá baixa numa cobrança. Depois de QUALQUER resposta (sucesso ou erro), recarrega o painel e a tabela: a cobrança sai de
 * Em Atraso e entra em Pagos Recentemente quando a baixa vai. A visão geral da locadora (a receber e em atraso) também
 * recarrega, como na manutenção.
 */
export function useBaixarPagamento(): UseMutationResult<RespostaBaixaPagamento, ErroApi | Error, VariaveisBaixarPagamento> {
  const queryClient = useQueryClient();
  return useMutation<RespostaBaixaPagamento, ErroApi | Error, VariaveisBaixarPagamento>({
    mutationFn: ({ idPagamento, requisicao }) => baixarPagamento(idPagamento, requisicao),
    onSettled: () => {
      invalidarPainelFinanceiro(queryClient);
      void queryClient.invalidateQueries({ queryKey: chaveFamiliaVisaoGeral() });
    },
  });
}
