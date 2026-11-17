import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarVeiculo } from '@/api/veiculos';
import type { ErroApi } from '@/types/api';
import type { VeiculoRetorno } from '@/types/veiculos';
import { chaveVeiculo } from './chavesVeiculos';

/**
 * Consulta de um veículo pelo id, para carregar o formulário de edição. Sem id, a consulta não roda. A tela carrega
 * os valores do formulário uma única vez; esta consulta pode ser atualizada depois sem apagar o que a pessoa digitou.
 */
export function useVeiculo(idVeiculo: number | null): UseQueryResult<VeiculoRetorno, ErroApi | Error> {
  return useQuery<VeiculoRetorno, ErroApi | Error>({
    queryKey: chaveVeiculo(idVeiculo),
    queryFn: () => buscarVeiculo(idVeiculo as number),
    enabled: idVeiculo !== null,
  });
}
