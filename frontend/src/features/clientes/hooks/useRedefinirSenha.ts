import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { redefinirSenha } from '@/api/autenticacao';
import type { RedefinirSenhaVO } from '@/types/autenticacao';

/**
 * Troca a senha com o token do link (POST /api/auth/redefinicao-senha). Sucesso é 204, sem corpo, e não
 * abre sessão. `gcTime: 0` impede que a senha e o token fiquem no cache do TanStack Query.
 */
export function useRedefinirSenha(): UseMutationResult<void, Error, RedefinirSenhaVO> {
  return useMutation<void, Error, RedefinirSenhaVO>({
    mutationFn: redefinirSenha,
    gcTime: 0,
  });
}
