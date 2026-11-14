import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { cadastrarLocadora } from '@/api/empresas';
import type { RequisicaoCadastroLocadora, RespostaEmpresa } from '@/types/empresas';

/**
 * Cadastro público da locadora (POST /api/empresas). Usado na P10-FE-02 (etapa 2, com o login automático).
 * A senha e o CPF do responsável vão só no corpo. `gcTime: 0` tira a mutação do cache logo após terminar.
 */
export function useCadastrarLocadora(): UseMutationResult<RespostaEmpresa, Error, RequisicaoCadastroLocadora> {
  return useMutation<RespostaEmpresa, Error, RequisicaoCadastroLocadora>({
    mutationFn: cadastrarLocadora,
    gcTime: 0,
  });
}
