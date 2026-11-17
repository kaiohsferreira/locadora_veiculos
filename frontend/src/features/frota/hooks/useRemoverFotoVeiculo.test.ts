import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { removerFotoVeiculo } from '@/api/fotosVeiculo';
import { useRemoverFotoVeiculo } from './useRemoverFotoVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));
vi.mock('@/api/fotosVeiculo', () => ({
  removerFotoVeiculo: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const useQueryClientMock = vi.mocked(useQueryClient);
const removerMock = vi.mocked(removerFotoVeiculo);

describe('useRemoverFotoVeiculo', () => {
  let invalidarMock: ReturnType<typeof vi.fn<(filtro: unknown) => Promise<void>>>;

  beforeEach(() => {
    useMutationMock.mockReset();
    useQueryClientMock.mockReset();
    removerMock.mockReset();
    removerMock.mockResolvedValue(undefined);
    invalidarMock = vi.fn<(filtro: unknown) => Promise<void>>().mockResolvedValue(undefined);
    useQueryClientMock.mockReturnValue({ invalidateQueries: invalidarMock } as unknown as ReturnType<typeof useQueryClient>);
  });

  it('remove a foto do veículo indicado', async () => {
    useRemoverFotoVeiculo();
    const opcoes = useMutationMock.mock.calls[0][0];

    await (opcoes.mutationFn as (variaveis: unknown) => Promise<unknown>)({ idVeiculo: 7, idVeiculoFoto: 42 });

    expect(removerMock).toHaveBeenCalledWith(7, 42);
  });

  it('ao dar certo, invalida a família do veículo (fotos e capas das listas)', async () => {
    useRemoverFotoVeiculo();
    const opcoes = useMutationMock.mock.calls[0][0];

    await (opcoes.onSuccess as (...args: unknown[]) => Promise<unknown>)(undefined, { idVeiculo: 7, idVeiculoFoto: 42 });

    expect(invalidarMock).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
  });
});
