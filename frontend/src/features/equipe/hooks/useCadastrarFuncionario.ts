import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { cadastrarFuncionarioEquipe } from '@/api/equipe';
import type { ErroApi } from '@/types/api';
import type { FuncionarioEquipe, RequisicaoCadastroFuncionario } from '@/types/equipe';
import { CHAVE_EQUIPE } from './useEquipe';

/**
 * Adiciona um funcionário (POST). A senha inicial existe só na chamada. `gcTime: 0` tira a mutação do cache logo
 * depois, e o painel que a usa desmonta ao fechar. Ao dar certo, a família `equipe` é invalidada (lista e totais).
 */
export function useCadastrarFuncionario(): UseMutationResult<
  FuncionarioEquipe,
  ErroApi | Error,
  RequisicaoCadastroFuncionario
> {
  const queryClient = useQueryClient();
  return useMutation<FuncionarioEquipe, ErroApi | Error, RequisicaoCadastroFuncionario>({
    mutationFn: cadastrarFuncionarioEquipe,
    gcTime: 0,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CHAVE_EQUIPE] }),
  });
}
