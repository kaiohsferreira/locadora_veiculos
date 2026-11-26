import { useQuery } from '@tanstack/react-query';
import { listarCidadesLocadoras } from '@/api/empresas';
import { buscarVeiculosDisponiveis } from '@/api/veiculos';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import type { FiltroVeiculosDisponiveis } from '@/types/veiculos';

export function useVeiculosDisponiveis(filtro: FiltroVeiculosDisponiveis, habilitado: boolean) {
  return useQuery({
    queryKey: ['veiculos', 'disponiveis', filtro],
    queryFn: ({ signal }) => buscarVeiculosDisponiveis(filtro, signal),
    enabled: habilitado,
  });
}

export { useCategorias as useCategoriasVeiculos };

export function useCidadesLocadoras() {
  return useQuery({
    queryKey: ['empresas', 'cidades'],
    queryFn: ({ signal }) => listarCidadesLocadoras(signal),
  });
}
