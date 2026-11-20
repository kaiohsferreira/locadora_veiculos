import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { encerrarManutencao } from '@/api/manutencoes';
import type { ErroApi } from '@/types/api';
import type { EncerrarManutencaoRequisicao, ManutencaoRetorno } from '@/types/manutencao';
import { invalidarAposMudancaManutencao } from '@/features/manutencao/hooks/invalidarManutencao';

/** Variáveis do encerramento: a ordem e o corpo (km na saída, data e custo). */
export interface VariaveisEncerrarManutencao {
  idManutencao: number;
  requisicao: EncerrarManutencaoRequisicao;
}

/**
 * Encerra a ordem (PATCH). Depois de QUALQUER resposta, recarrega manutenção, veículos e visão geral: o veículo volta a
 * DISPONIVEL quando estava em manutenção.
 */
export function useEncerrarManutencao(): UseMutationResult<ManutencaoRetorno, ErroApi | Error, VariaveisEncerrarManutencao> {
  const queryClient = useQueryClient();
  return useMutation<ManutencaoRetorno, ErroApi | Error, VariaveisEncerrarManutencao>({
    mutationFn: ({ idManutencao, requisicao }) => encerrarManutencao(idManutencao, requisicao),
    onSettled: () => invalidarAposMudancaManutencao(queryClient),
  });
}
