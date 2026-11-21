import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { enviarPropostaCompra } from '@/api/vendas';
import type { ErroApi } from '@/types/api';
import type { PropostaCompra, PropostaCompraRequisicao } from '@/types/vendas';

/**
 * Envia a proposta de compra (POST, 201). Nenhuma lista da tela depende da proposta, então não há consulta para
 * invalidar; a tela mostra a confirmação com a resposta. Erros chegam como ErroApi (401, 404, 409, 400, 429).
 */
export function useEnviarPropostaCompra(): UseMutationResult<PropostaCompra, ErroApi | Error, PropostaCompraRequisicao> {
  return useMutation<PropostaCompra, ErroApi | Error, PropostaCompraRequisicao>({
    mutationFn: enviarPropostaCompra,
  });
}
