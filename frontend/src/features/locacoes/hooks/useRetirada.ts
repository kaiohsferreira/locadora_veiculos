import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { abrirLocacao, buscarContextoRetirada } from '@/api/locacoes';
import type { ContextoRetirada, DadosCaucaoAbertura } from '@/types/retirada';
import { chaveFamiliaPainelOperacional } from './chavesPainelOperacional';
export const chaveRetirada = (id: number | null) => ['retirada', id] as const;
export function opcoesRetirada(id: number | null) {
  return { queryKey: chaveRetirada(id), queryFn: () => buscarContextoRetirada(id as number),
    enabled: id !== null, retry: false as const, staleTime: 0, gcTime: 0 };
}
export function useRetirada(id: number | null) { return useQuery(opcoesRetirada(id)); }
/**
 * Abertura da locação. A variável da mutação são os campos do cartão da caução: nulo sem caução (o POST leva só a
 * reserva, como antes) e preenchido com a caução exigida (locacao.contract.json v4, tela_16_v4.chamada).
 */
export function opcoesAbrirLocacao(cliente: ReturnType<typeof useQueryClient>, idReserva: number) {
  return {
    mutationFn: (caucao: DadosCaucaoAbertura | null) => abrirLocacao(caucao === null ? { idReserva } : { idReserva, caucao }),
    retry: false as const,
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
