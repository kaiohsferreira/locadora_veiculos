import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { editarFuncionarioEquipe } from '@/api/equipe';
import type { ErroApi } from '@/types/api';
import type { RequisicaoEdicaoFuncionario } from '@/types/equipe';
import { CHAVE_EQUIPE } from './useEquipe';

/** Variáveis da edição: id do funcionário e os campos editáveis. */
export interface VariaveisEdicaoFuncionario {
  idFuncionario: number;
  requisicao: RequisicaoEdicaoFuncionario;
}

/**
 * Altera um funcionário (PUT). Sem CPF e sem senha. Ao dar certo, invalida a família `equipe` (lista, totais e
 * a consulta do funcionário). `gcTime: 0` tira a mutação do cache depois de terminar.
 */
export function useEditarFuncionario(): UseMutationResult<void, ErroApi | Error, VariaveisEdicaoFuncionario> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisEdicaoFuncionario>({
    mutationFn: ({ idFuncionario, requisicao }) => editarFuncionarioEquipe(idFuncionario, requisicao),
    gcTime: 0,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CHAVE_EQUIPE] }),
  });
}
