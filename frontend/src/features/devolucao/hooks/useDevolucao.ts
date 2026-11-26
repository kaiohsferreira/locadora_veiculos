import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { buscarContextoDevolucao, encerrarLocacao, registrarSaida } from '@/api/devolucao';
import { ErroApi } from '@/types/api';
import type { ContextoDevolucao, EncerrarLocacao, RegistrarSaida } from '@/types/devolucao';
import { chaveDevolucoesPendentes, chaveSimulacaoEncerramento } from './chavesDevolucao';

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

/**
 * Encerramento (POST /api/locacoes/{id}/devolucao). Sem nova tentativa automática: a tela só repete quando a pessoa pede. Em
 * sucesso, recarrega o contexto (que vira FINALIZADA) e a lista de pendentes. Em 409 (total ou estado mudou), recarrega o
 * contexto para a pessoa conferir de novo.
 */
export function useEncerrarLocacao(id: number) {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: (dados: EncerrarLocacao) => encerrarLocacao(id, dados),
    retry: false,
    onSuccess: async () => {
      await cliente.invalidateQueries({ queryKey: chaveDevolucao(id) });
      await cliente.invalidateQueries({ queryKey: chaveDevolucoesPendentes });
    },
    // Em 409 a memória do cálculo também fica velha: recarrega o contexto e a simulação juntos.
    onError: async erro => {
      if (!(erro instanceof ErroApi) || erro.status !== 409) return;
      await cliente.invalidateQueries({ queryKey: chaveDevolucao(id) });
      await cliente.invalidateQueries({ queryKey: chaveSimulacaoEncerramento(id) });
    },
  });
}
