import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { listarCategorias } from '@/api/categorias';
import type { Pagina, ErroApi } from '@/types/api';
import type { CategoriaVeiculo } from '@/types/categorias';
import { chaveCategorias } from './chavesVeiculos';

/**
 * Categorias ativas para o seletor da tela de veículo. A lista muda pouco; o padrão do projeto (30 s) basta. Erros
 * 4xx não são repetidos (regra do QueryClient).
 */
export function useCategorias(): UseQueryResult<Pagina<CategoriaVeiculo>, ErroApi | Error> {
  return useQuery<Pagina<CategoriaVeiculo>, ErroApi | Error>({
    queryKey: chaveCategorias(),
    queryFn: listarCategorias,
  });
}
