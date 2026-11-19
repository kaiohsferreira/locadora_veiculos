import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { alterarStatusVeiculo } from '@/api/veiculos';
import type { ErroApi } from '@/types/api';
import type { DestinoManualStatusVeiculo } from '@/types/veiculos';
import { chaveFamiliaVisaoGeral } from '@/features/visao-geral/chaveVisaoGeral';
import { chaveFamiliaVeiculos } from './chavesVeiculos';

/** Variáveis da troca manual: o veículo e o status de destino (um dos três que a tela oferece). */
export interface VariaveisAlterarStatusVeiculo {
  idVeiculo: number;
  status: DestinoManualStatusVeiculo;
}

/**
 * Troca o status do veículo (PATCH, 204). Depois de QUALQUER resposta, sucesso ou erro, invalida a família de veículos
 * (lista e detalhe) e a visão geral (cartões e pílulas da tela 11). A mutação só termina depois dessas recargas, então
 * a linha segue bloqueada até a lista mostrar o status novo.
 */
export function useAlterarStatusVeiculo(): UseMutationResult<void, ErroApi | Error, VariaveisAlterarStatusVeiculo> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, VariaveisAlterarStatusVeiculo>({
    mutationFn: ({ idVeiculo, status }) => alterarStatusVeiculo(idVeiculo, status),
    onSettled: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: chaveFamiliaVeiculos() }),
        queryClient.invalidateQueries({ queryKey: chaveFamiliaVisaoGeral() }),
      ]);
    },
  });
}
