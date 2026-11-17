import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { listarFotosVeiculo } from '@/api/fotosVeiculo';
import { useFotosVeiculo } from './useFotosVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));
vi.mock('@/api/fotosVeiculo', () => ({
  listarFotosVeiculo: vi.fn(),
}));

const useQueryMock = vi.mocked(useQuery);
const listarMock = vi.mocked(listarFotosVeiculo);

describe('useFotosVeiculo', () => {
  beforeEach(() => {
    useQueryMock.mockReset();
    listarMock.mockReset();
    listarMock.mockResolvedValue([]);
  });

  it('consulta a lista do veículo com a chave estável e a consulta ligada', async () => {
    useFotosVeiculo(7);

    const opcoes = useQueryMock.mock.calls[0][0];
    expect(opcoes.queryKey).toEqual(['veiculos', 'fotos', 7]);
    expect(opcoes.enabled).toBe(true);

    await (opcoes.queryFn as () => Promise<unknown>)();
    expect(listarMock).toHaveBeenCalledWith(7);
  });

  it('sem id (cadastro) a consulta fica desligada e não chama a API', async () => {
    useFotosVeiculo(null);

    const opcoes = useQueryMock.mock.calls[0][0];
    expect(opcoes.enabled).toBe(false);

    await expect((opcoes.queryFn as () => Promise<unknown>)()).resolves.toEqual([]);
    expect(listarMock).not.toHaveBeenCalled();
  });
});
