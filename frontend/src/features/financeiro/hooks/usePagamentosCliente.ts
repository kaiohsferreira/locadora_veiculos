import { useCallback } from 'react';
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query';
import { gerarPixMeuPagamento, listarMeusPagamentos, obterResumoMeusPagamentos } from '@/api/pagamentosCliente';
import { deveRecarregarCobrancasAposErroPix } from '@/features/financeiro/cliente/regrasPagamentosCliente';
import type { ErroApi } from '@/types/api';
import type {
  CobrancaPix,
  FiltroMeusPagamentos,
  MeuPagamento,
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

/** Parte do cliente que o invalidador usa: só a chamada de invalidar (o QueryClient real a cumpre). */
export interface AlvoInvalidacaoPagamentos {
  invalidateQueries: (filtro: { queryKey: readonly unknown[] }) => Promise<unknown>;
}

/**
 * Invalida a família de cobranças do cliente sem esperar o refresh. Não devolve a promise: o TanStack Query aguarda o
 * onSuccess da mutação, e a gaveta ficaria em "Gerando seu PIX..." até as listas recarregarem.
 */
export function invalidarCobrancasClientePosPix(alvo: AlvoInvalidacaoPagamentos): void {
  void alvo.invalidateQueries({ queryKey: [CHAVE_PAGAMENTOS_CLIENTE] });
}

/**
 * Decide o que fazer com um erro ao gerar o PIX: recarrega as cobranças só quando a regra manda (409, situação mudou no
 * servidor). Os outros erros deixam a lista como está; a gaveta mostra a mensagem em todos os casos.
 */
export function tratarErroGeracaoPixPagamento(alvo: AlvoInvalidacaoPagamentos, erro: unknown): void {
  if (deveRecarregarCobrancasAposErroPix(erro)) {
    invalidarCobrancasClientePosPix(alvo);
  }
}

/**
 * Gera ou reabre o PIX de uma cobrança do cliente (POST, sem corpo). Ao dar certo, invalida a família de cobranças do
 * cliente (as listas não trazem o PIX; a invalidação só mantém os dados das listas frescos). Erro 409 também invalida,
 * porque a situação da cobrança mudou no servidor. Não há polling: a gaveta não sincroniza com o provedor.
 */
export function useGerarPixPagamentoCliente(): UseMutationResult<CobrancaPix, ErroApi | Error, number> {
  const queryClient = useQueryClient();
  return useMutation<CobrancaPix, ErroApi | Error, number>({
    mutationFn: (idPagamento) => gerarPixMeuPagamento(idPagamento),
    onSuccess: () => {
      invalidarCobrancasClientePosPix(queryClient);
    },
    onError: (erro) => {
      tratarErroGeracaoPixPagamento(queryClient, erro);
    },
  });
}

/**
 * Busca a única cobrança pendente, sob demanda (clique em "Pagar Fatura" ou "Regularizar" quando há uma só). Devolve a
 * primeira da aba Pendentes, ou nulo se a lista já não tiver nenhuma. Erro propaga como ErroApi.
 */
export function useBuscarUnicaPendencia(): () => Promise<MeuPagamento | null> {
  const queryClient = useQueryClient();
  return useCallback(async () => {
    const resposta = await queryClient.fetchQuery<RespostaMeusPagamentos, ErroApi | Error>({
      queryKey: [CHAVE_PAGAMENTOS_CLIENTE, 'pendencia-unica'],
      queryFn: () => listarMeusPagamentos({ aba: 'PENDENTES', periodo: 'TUDO', pagina: 1, tamanhoPagina: 1 }),
      staleTime: 0,
    });
    return resposta.itens[0] ?? null;
  }, [queryClient]);
}
