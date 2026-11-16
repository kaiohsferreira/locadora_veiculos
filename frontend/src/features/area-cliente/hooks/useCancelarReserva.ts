import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { cancelarReserva } from '@/api/reservas';
import type { ErroApi } from '@/types/api';
import { CHAVE_RESERVAS } from './useMinhasReservas';

/**
 * Cancela uma reserva pelo id (DELETE). Ao terminar, com sucesso ou com erro, invalida a família `reservas`: a
 * reserva sai de Ativas, entra no Histórico e os contadores das abas são atualizados.
 */
export function useCancelarReserva(): UseMutationResult<void, ErroApi | Error, number> {
  const queryClient = useQueryClient();
  return useMutation<void, ErroApi | Error, number>({
    mutationFn: (idReserva) => cancelarReserva(idReserva),
    onSettled: () => queryClient.invalidateQueries({ queryKey: [CHAVE_RESERVAS] }),
  });
}
