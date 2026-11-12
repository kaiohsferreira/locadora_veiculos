import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { solicitarRecuperacaoSenha } from '@/api/autenticacao';
import type { ReturnRecuperacaoSolicitadaVO, SolicitarRecuperacaoSenhaVO } from '@/types/autenticacao';

/**
 * Pedido do link de redefinição (POST /api/auth/recuperacao-senha). A tela trata o resultado sem
 * depender do corpo: o texto de confirmação é fixo. `gcTime: 0` tira a mutação do cache logo depois.
 */
export function useSolicitarRecuperacaoSenha(): UseMutationResult<
  ReturnRecuperacaoSolicitadaVO,
  Error,
  SolicitarRecuperacaoSenhaVO
> {
  return useMutation<ReturnRecuperacaoSolicitadaVO, Error, SolicitarRecuperacaoSenhaVO>({
    mutationFn: solicitarRecuperacaoSenha,
    gcTime: 0,
  });
}
