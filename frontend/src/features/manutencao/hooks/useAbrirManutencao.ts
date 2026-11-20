import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { abrirManutencao } from '@/api/manutencoes';
import type { ErroApi } from '@/types/api';
import type { AbrirManutencaoRequisicao, ManutencaoRetorno } from '@/types/manutencao';
import { invalidarAposMudancaManutencao } from '@/features/manutencao/hooks/invalidarManutencao';

/**
 * Abre a ordem (POST). Depois de QUALQUER resposta, sucesso ou erro, recarrega manutenção, veículos e visão geral: o
 * veículo sai da busca de disponíveis quando a ordem é aberta.
 */
export function useAbrirManutencao(): UseMutationResult<ManutencaoRetorno, ErroApi | Error, AbrirManutencaoRequisicao> {
  const queryClient = useQueryClient();
  return useMutation<ManutencaoRetorno, ErroApi | Error, AbrirManutencaoRequisicao>({
    mutationFn: abrirManutencao,
    onSettled: () => invalidarAposMudancaManutencao(queryClient),
  });
}
