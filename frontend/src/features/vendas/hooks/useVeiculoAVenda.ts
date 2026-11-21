import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarVeiculoAVenda } from '@/api/vendas';
import type { ErroApi } from '@/types/api';
import type { VeiculoAVenda } from '@/types/vendas';
import { chaveVeiculoAVenda } from './chavesVendas';

/** Detalhe público do seminovo, para o cartão do carro da proposta. Sem id válido, a consulta não roda. */
export function useVeiculoAVenda(idVeiculo: number | null): UseQueryResult<VeiculoAVenda, ErroApi | Error> {
  return useQuery<VeiculoAVenda, ErroApi | Error>({
    queryKey: chaveVeiculoAVenda(idVeiculo),
    queryFn: () => buscarVeiculoAVenda(idVeiculo as number),
    enabled: idVeiculo !== null,
  });
}
