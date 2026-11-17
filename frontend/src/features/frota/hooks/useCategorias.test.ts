import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { listarCategorias } from '@/api/categorias';
import { useCategorias } from './useCategorias';

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));
vi.mock('@/api/categorias', () => ({
  listarCategorias: vi.fn(),
}));

const useQueryMock = vi.mocked(useQuery);

describe('useCategorias', () => {
  beforeEach(() => {
    useQueryMock.mockReset();
  });

  it('consulta a lista de categorias pela chave própria e pela função da API', () => {
    useCategorias();

    const opcoes = useQueryMock.mock.calls[0][0];
    expect(opcoes.queryKey).toEqual(['categorias', 'lista']);
    expect(opcoes.queryFn).toBe(listarCategorias);
  });
});
