import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { removerFotoVeiculo } from '@/api/fotosVeiculo';
import type { ErroApi } from '@/types/api';
import { CHAVE_VEICULOS } from './chavesVeiculos';

/** Foto gravada a remover: o veículo e o id da foto. */
export interface VariaveisRemocaoFoto {
  idVeiculo: number;
  idVeiculoFoto: number;
}

/**
 * Remove uma foto gravada (apaga o arquivo no servidor). Ao dar certo, invalida a família do veículo: a lista de
 * fotos e as listas que mostram a capa são atualizadas.
 */
export function useRemoverFotoVeiculo(): UseMutationResult<void, ErroApi | Error, VariaveisRemocaoFoto> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisRemocaoFoto>({
    mutationFn: ({ idVeiculo, idVeiculoFoto }) => removerFotoVeiculo(idVeiculo, idVeiculoFoto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CHAVE_VEICULOS] }),
  });
}
