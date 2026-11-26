import { useQuery } from '@tanstack/react-query';
import { listarDevolucoesPendentes } from '@/api/locacoes';
import type { FiltroDevolucoesPendentes } from '@/types/devolucao';
import { chaveDevolucoesPendentes } from './chavesDevolucao';

/** Lista de devoluções pendentes da página de entrada da retirada (GET /api/locacoes?status=). */
export function useDevolucoesPendentes(filtro: FiltroDevolucoesPendentes) {
  return useQuery({
    queryKey: [...chaveDevolucoesPendentes, filtro],
    queryFn: () => listarDevolucoesPendentes(filtro),
    retry: false,
  });
}
