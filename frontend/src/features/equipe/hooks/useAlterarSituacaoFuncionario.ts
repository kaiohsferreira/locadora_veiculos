import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { alterarSituacaoFuncionarioEquipe } from '@/api/equipe';
import type { ErroApi } from '@/types/api';
import { CHAVE_EQUIPE } from './useEquipe';

/** Variáveis da troca de situação: quem e para qual situação (ativo = true reativa, false desativa). */
export interface VariaveisSituacaoFuncionario {
  idFuncionario: number;
  ativo: boolean;
}

/**
 * Desativa ou reativa o acesso (PATCH). Não existe exclusão. Ao dar certo, invalida a família `equipe` para a lista
 * e os totais refletirem a mudança.
 */
export function useAlterarSituacaoFuncionario(): UseMutationResult<void, ErroApi | Error, VariaveisSituacaoFuncionario> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisSituacaoFuncionario>({
    mutationFn: ({ idFuncionario, ativo }) => alterarSituacaoFuncionarioEquipe(idFuncionario, { ativo }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CHAVE_EQUIPE] }),
  });
}
