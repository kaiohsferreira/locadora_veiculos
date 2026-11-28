import { useQuery } from '@tanstack/react-query';
import { buscarVeiculoLocacao } from '@/api/veiculos';
import type { PeriodoLocacaoVeiculo } from '@/types/veiculos';

export function useDetalheVeiculo(idVeiculo: number, periodo?: PeriodoLocacaoVeiculo) {
  return useQuery({
    queryKey: ['veiculos', 'locacao', idVeiculo, periodo],
    queryFn: ({ signal }) => buscarVeiculoLocacao(idVeiculo, periodo, signal),
    staleTime: 0,
    retry: false,
  });
}
