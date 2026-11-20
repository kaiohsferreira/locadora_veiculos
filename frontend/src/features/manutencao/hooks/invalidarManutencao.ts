import type { QueryClient } from '@tanstack/react-query';
import { chaveFamiliaVisaoGeral } from '@/features/visao-geral/chaveVisaoGeral';
import { chaveFamiliaVeiculos } from '@/features/frota/hooks/chavesVeiculos';
import { chaveFamiliaManutencao } from '@/features/manutencao/hooks/chavesManutencao';

/**
 * Depois de abrir ou encerrar uma ordem, recarrega o que muda com ela: a própria manutenção (lista, resumo e alertas),
 * os veículos (o status muda para MANUTENCAO ou volta a DISPONIVEL, e a tela 11 e a 12 mostram isso) e a visão geral.
 */
export function invalidarAposMudancaManutencao(queryClient: QueryClient): Promise<unknown> {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: chaveFamiliaManutencao() }),
    queryClient.invalidateQueries({ queryKey: chaveFamiliaVeiculos() }),
    queryClient.invalidateQueries({ queryKey: chaveFamiliaVisaoGeral() }),
  ]);
}
