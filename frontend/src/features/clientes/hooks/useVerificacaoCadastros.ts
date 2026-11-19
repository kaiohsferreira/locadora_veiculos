import { keepPreviousData, useMutation, useQuery, useQueryClient, type UseQueryResult } from '@tanstack/react-query';
import {
  buscarVerificacaoCliente,
  decidirVerificacaoCliente,
  listarDocumentosVerificacao,
  listarFilaVerificacao,
} from '@/api/verificacaoCadastros';
import type { ErroApi, Pagina } from '@/types/api';
import type {
  ClienteFila,
  ClienteVerificacao,
  DocumentoVerificacao,
  FiltroFilaVerificacao,
  RequisicaoDecisaoVerificacao,
} from '@/types/verificacaoCadastros';
import { filtroPendentes } from '@/features/clientes/verificacao/regras';

/** Família de chaves da verificação: a decisão invalida esta raiz (fila, cartão, detalhe e documentos). */
export const CHAVE_VERIFICACAO = 'verificacao-cadastros';

/**
 * Fila paginada para um filtro. Ao trocar de página ou de filtro, mantém a lista anterior até a nova chegar. `gcTime: 0`
 * tira nome e CPF do cache assim que a tela sai de uso (como o detalhe).
 */
export function useFilaVerificacao(filtro: FiltroFilaVerificacao): UseQueryResult<Pagina<ClienteFila>, ErroApi | Error> {
  return useQuery<Pagina<ClienteFila>, ErroApi | Error>({
    queryKey: [CHAVE_VERIFICACAO, 'fila', filtro],
    queryFn: () => listarFilaVerificacao(filtro),
    placeholderData: keepPreviousData,
    gcTime: 0,
  });
}

/** Total de pendentes do cartão (a contagem não depende da busca nem do filtro da tabela). */
export function useTotalPendentesVerificacao(): UseQueryResult<Pagina<ClienteFila>, ErroApi | Error> {
  return useQuery<Pagina<ClienteFila>, ErroApi | Error>({
    queryKey: [CHAVE_VERIFICACAO, 'pendentes'],
    queryFn: () => listarFilaVerificacao(filtroPendentes()),
    gcTime: 0,
  });
}

/**
 * Cadastro do painel. Sem id, a consulta não roda. `gcTime: 0` tira os dados pessoais do cache assim que o painel fecha
 * (ou o cliente sai da tela).
 */
export function useVerificacaoCliente(idCliente: number | null): UseQueryResult<ClienteVerificacao, ErroApi | Error> {
  return useQuery<ClienteVerificacao, ErroApi | Error>({
    queryKey: [CHAVE_VERIFICACAO, 'detalhe', idCliente],
    queryFn: () => buscarVerificacaoCliente(idCliente as number),
    enabled: idCliente !== null,
    gcTime: 0,
  });
}

/**
 * Documentos do painel, com o link temporário de leitura. O link é renovado a cada abertura: `gcTime: 0` e
 * `staleTime: 0` impedem que a resposta fique guardada depois que o painel fecha.
 */
export function useDocumentosVerificacao(idCliente: number | null): UseQueryResult<DocumentoVerificacao[], ErroApi | Error> {
  return useQuery<DocumentoVerificacao[], ErroApi | Error>({
    queryKey: [CHAVE_VERIFICACAO, 'documentos', idCliente],
    queryFn: () => listarDocumentosVerificacao(idCliente as number),
    enabled: idCliente !== null,
    staleTime: 0,
    gcTime: 0,
  });
}

/** Variáveis da decisão: o cliente e o corpo do PATCH. */
export interface VariaveisDecisaoVerificacao {
  idCliente: number;
  requisicao: RequisicaoDecisaoVerificacao;
}

/**
 * Aprova ou recusa o pedido (PATCH, 204). Ao dar certo ou errar, recarrega a fila, o cartão e o detalhe do cliente, para a
 * linha mostrar o status novo. Os documentos não são recarregados: cada consulta gera uma linha de auditoria e troca os
 * links de leitura, e a decisão não muda os arquivos.
 */
export function useDecidirVerificacao() {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisDecisaoVerificacao>({
    mutationFn: ({ idCliente, requisicao }) => decidirVerificacaoCliente(idCliente, requisicao),
    onSettled: (_resultado, _erro, variaveis) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'fila'] }),
        queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'pendentes'] }),
        queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'detalhe', variaveis.idCliente] }),
      ]),
  });
}
