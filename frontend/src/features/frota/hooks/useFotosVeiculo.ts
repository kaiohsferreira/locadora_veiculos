import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarFotosVeiculo } from '@/api/fotosVeiculo';
import type { ErroApi } from '@/types/api';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';
import { chaveFotosVeiculo } from './chavesVeiculos';

/**
 * Fotos gravadas do veículo, em ordem. A consulta usa o staleTime padrão do projeto (30 s). Sem id, a consulta
 * não roda e a lista vem vazia.
 */
export function useFotosVeiculo(idVeiculo: number | null): UseQueryResult<FotoDoVeiculo[], ErroApi | Error> {
  return useQuery<FotoDoVeiculo[], ErroApi | Error>({
    queryKey: chaveFotosVeiculo(idVeiculo),
    queryFn: () => (idVeiculo === null ? Promise.resolve([]) : listarFotosVeiculo(idVeiculo)),
    enabled: idVeiculo !== null,
  });
}
