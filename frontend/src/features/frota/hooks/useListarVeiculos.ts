import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarVeiculos } from '@/api/veiculos';
import type { ErroApi, Pagina } from '@/types/api';
import type { FiltroListagemVeiculos, VeiculoRetorno } from '@/types/veiculos';
import { chaveListagemVeiculos } from './chavesVeiculos';

/**
 * Lista paginada da frota para um filtro. Ao trocar de página, de filtro ou de tamanho, mantém a lista anterior na tela
 * até a nova chegar (sem piscar). Erros 4xx não são repetidos automaticamente (regra do QueryClient).
 */
export function useListarVeiculos(filtro: FiltroListagemVeiculos): UseQueryResult<Pagina<VeiculoRetorno>, ErroApi | Error> {
  return useQuery<Pagina<VeiculoRetorno>, ErroApi | Error>({
    queryKey: chaveListagemVeiculos(filtro),
    queryFn: () => listarVeiculos(filtro),
    placeholderData: keepPreviousData,
  });
}
