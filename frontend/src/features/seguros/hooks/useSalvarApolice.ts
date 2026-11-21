import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { cadastrarApolice, editarApolice } from '@/api/seguros';
import type { ErroApi } from '@/types/api';
import type { Apolice, ApoliceRequisicao } from '@/types/seguros';
import { chaveFamiliaSeguros } from './chavesSeguros';

/** Dados da edição: a apólice e o corpo novo. */
export interface EdicaoApolice {
  idSeguro: number;
  requisicao: ApoliceRequisicao;
}

/** Cadastra a apólice (POST, 201). Ao dar certo, atualiza a lista, os indicadores e os detalhes. */
export function useCadastrarApolice(): UseMutationResult<Apolice, ErroApi | Error, ApoliceRequisicao> {
  const queryClient = useQueryClient();
  return useMutation<Apolice, ErroApi | Error, ApoliceRequisicao>({
    mutationFn: cadastrarApolice,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chaveFamiliaSeguros() }),
  });
}

/** Edita a apólice (PUT, 204). Ao dar certo, atualiza a lista, os indicadores e os detalhes. */
export function useEditarApolice(): UseMutationResult<void, ErroApi | Error, EdicaoApolice> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, EdicaoApolice>({
    mutationFn: ({ idSeguro, requisicao }) => editarApolice(idSeguro, requisicao),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chaveFamiliaSeguros() }),
  });
}
