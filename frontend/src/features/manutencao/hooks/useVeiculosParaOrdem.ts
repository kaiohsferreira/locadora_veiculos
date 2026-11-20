import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarVeiculos } from '@/api/veiculos';
import type { ErroApi } from '@/types/api';
import type { VeiculoRetorno } from '@/types/veiculos';
import { chaveVeiculosParaOrdem } from '@/features/manutencao/hooks/chavesManutencao';
import { reunirTodasAsPaginas } from '@/features/manutencao/hooks/paginas';

/** Tamanho de página no máximo que a listagem da frota aceita (1 a 100). */
const TAMANHO_PAGINA_VEICULOS_ORDEM = 100;

/**
 * Todos os veículos da locadora para o seletor do modal de abrir ordem. Junta as páginas da listagem da frota, então a
 * frota inteira aparece, e não só a primeira página.
 */
export function useVeiculosParaOrdem(): UseQueryResult<VeiculoRetorno[], ErroApi | Error> {
  return useQuery<VeiculoRetorno[], ErroApi | Error>({
    queryKey: chaveVeiculosParaOrdem(),
    queryFn: () =>
      reunirTodasAsPaginas((pagina) => listarVeiculos({ pagina, tamanhoPagina: TAMANHO_PAGINA_VEICULOS_ORDEM })),
  });
}
