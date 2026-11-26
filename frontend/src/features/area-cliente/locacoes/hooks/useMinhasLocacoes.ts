import { useQuery, type QueryKey, type UseQueryResult } from '@tanstack/react-query';
import { listarMinhasLocacoes } from '@/api/locacoesCliente';
import type { ErroApi } from '@/types/api';
import type { FiltroLocacoes, RespostaLocacoes, SituacaoLocacao } from '@/types/locacoesCliente';

/** Família de chaves das locações do cliente. */
export const CHAVE_LOCACOES_CLIENTE = 'locacoes-cliente';

/**
 * Lista de uma situação. Trocar de página mantém a lista anterior na tela até a nova chegar. Trocar de aba não mantém:
 * a lista de outra situação nunca aparece na aba nova. Erros 4xx não são repetidos (regra do QueryClient).
 */
export function useLocacoesCliente(filtro: FiltroLocacoes): UseQueryResult<RespostaLocacoes, ErroApi | Error> {
  return useQuery<RespostaLocacoes, ErroApi | Error>({
    queryKey: [CHAVE_LOCACOES_CLIENTE, 'lista', filtro],
    queryFn: () => listarMinhasLocacoes(filtro),
    placeholderData: (dadosAnteriores, consultaAnterior) => {
      const chaveAnterior: QueryKey | undefined = consultaAnterior?.queryKey;
      const situacaoAnterior = (chaveAnterior?.[2] as FiltroLocacoes | undefined)?.situacao;
      return situacaoAnterior === filtro.situacao ? dadosAnteriores : undefined;
    },
  });
}

/** Contador de uma aba: consulta de uma linha só, para o `total` da situação. */
export function useTotalLocacoesCliente(situacao: SituacaoLocacao): UseQueryResult<RespostaLocacoes, ErroApi | Error> {
  return useQuery<RespostaLocacoes, ErroApi | Error>({
    queryKey: [CHAVE_LOCACOES_CLIENTE, 'total', situacao],
    queryFn: () => listarMinhasLocacoes({ pagina: 1, tamanhoPagina: 1, situacao }),
  });
}
