import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { abrirLocacao, buscarContextoRetirada } from '@/api/locacoes';
import type { ContextoRetirada } from '@/types/retirada';
import { chaveFamiliaPainelOperacional } from './chavesPainelOperacional';
export const chaveRetirada = (id: number | null) => ['retirada', id] as const;
export function opcoesRetirada(id: number | null) {
  return { queryKey: chaveRetirada(id), queryFn: () => buscarContextoRetirada(id as number),
    enabled: id !== null, retry: false as const, staleTime: 0, gcTime: 0 };
}
export function useRetirada(id: number | null) { return useQuery(opcoesRetirada(id)); }
export function opcoesAbrirLocacao(cliente: ReturnType<typeof useQueryClient>, idReserva: number) {
  return {
    mutationFn: () => abrirLocacao({ idReserva }), retry: false as const,
    onSuccess: async (locacao: Awaited<ReturnType<typeof abrirLocacao>>) => {
      await cliente.cancelQueries({ queryKey: chaveRetirada(idReserva) });
      cliente.setQueryData<ContextoRetirada>(chaveRetirada(idReserva), (anterior) =>
        anterior ? { ...anterior, idLocacaoExistente: locacao.idLocacao } : anterior);
      await cliente.invalidateQueries({ queryKey: chaveFamiliaPainelOperacional() });
      await cliente.invalidateQueries({ queryKey: ['reservas'] });
    },
    // Recupera também resposta perdida sem repetir automaticamente o POST.
    onError: async () => { await cliente.invalidateQueries({ queryKey: chaveRetirada(idReserva) }); },
  };
}
export function useAbrirLocacao(idReserva: number) {
  return useMutation(opcoesAbrirLocacao(useQueryClient(), idReserva));
}
