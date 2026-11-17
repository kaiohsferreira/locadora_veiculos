import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { editarVeiculo } from '@/api/veiculos';
import type { ErroApi } from '@/types/api';
import type { VeiculoRequisicao } from '@/types/veiculos';
import { chaveFamiliaVeiculos } from './chavesVeiculos';

/** Variáveis da edição: id do veículo e os 13 campos. */
export interface VariaveisEdicaoVeiculo {
  idVeiculo: number;
  requisicao: VeiculoRequisicao;
}

/**
 * Altera um veículo (PUT, 204). Ao dar certo, invalida a família de veículos (detalhe, fotos e listas). O status não
 * muda por esta rota.
 */
export function useEditarVeiculo(): UseMutationResult<void, ErroApi | Error, VariaveisEdicaoVeiculo> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisEdicaoVeiculo>({
    mutationFn: ({ idVeiculo, requisicao }) => editarVeiculo(idVeiculo, requisicao),
    gcTime: 0,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chaveFamiliaVeiculos() }),
  });
}
