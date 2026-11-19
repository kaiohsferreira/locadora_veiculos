import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { buscarContextoDevolucao, registrarSaida } from '@/api/devolucao';
import type { ContextoDevolucao, RegistrarSaida } from '@/types/devolucao';

export const chaveDevolucao = (id: number | null) => ['devolucao', id] as const;
export function useDevolucao(id: number | null) {
  return useQuery({ queryKey: chaveDevolucao(id), queryFn: ({ signal }) => buscarContextoDevolucao(id as number, signal), enabled: id !== null, retry: false });
}
export function useRegistrarSaida(id: number) {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: (dados: RegistrarSaida) => registrarSaida(id, dados),
    retry: false,
    onSuccess: async comparacao => {
      await cliente.cancelQueries({ queryKey: chaveDevolucao(id) });
      cliente.setQueryData<ContextoDevolucao>(chaveDevolucao(id), anterior => anterior ? { ...anterior, comparacao } : anterior);
      await cliente.invalidateQueries({ queryKey: chaveDevolucao(id) });
    },
    // Inclusive falha de rede: o servidor pode ter persistido antes da conexão cair.
    onError: async () => { await cliente.invalidateQueries({ queryKey: chaveDevolucao(id) }); },
  });
}
