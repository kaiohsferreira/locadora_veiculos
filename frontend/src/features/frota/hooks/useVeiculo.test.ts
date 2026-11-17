import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { buscarVeiculo } from '@/api/veiculos';
import { useVeiculo } from './useVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));
vi.mock('@/api/veiculos', () => ({
  buscarVeiculo: vi.fn(),
}));

const useQueryMock = vi.mocked(useQuery);
const buscarMock = vi.mocked(buscarVeiculo);

describe('useVeiculo', () => {
  beforeEach(() => {
    useQueryMock.mockReset();
    buscarMock.mockReset();
    buscarMock.mockResolvedValue({ idVeiculo: 7 } as never);
  });

  it('consulta o veículo pela chave do detalhe, com a consulta ligada', async () => {
    useVeiculo(7);

    const opcoes = useQueryMock.mock.calls[0][0];
    expect(opcoes.queryKey).toEqual(['veiculos', 'detalhe', 7]);
    expect(opcoes.enabled).toBe(true);

    await (opcoes.queryFn as () => Promise<unknown>)();
    expect(buscarMock).toHaveBeenCalledWith(7);
  });

  it('sem id (cadastro) a consulta fica desligada e não chama a API', () => {
    useVeiculo(null);

    const opcoes = useQueryMock.mock.calls[0][0];
    expect(opcoes.enabled).toBe(false);
    expect(buscarMock).not.toHaveBeenCalled();
  });
});
