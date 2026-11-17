import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { editarVeiculo } from '@/api/veiculos';
import type { VeiculoRequisicao } from '@/types/veiculos';
import { useEditarVeiculo } from './useEditarVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));
vi.mock('@/api/veiculos', () => ({
  editarVeiculo: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const useQueryClientMock = vi.mocked(useQueryClient);

interface OpcoesEdicao {
  mutationFn: (variaveis: { idVeiculo: number; requisicao: VeiculoRequisicao }) => Promise<void>;
  onSuccess: () => Promise<unknown>;
}

const REQUISICAO = { idCategoria: 2 } as VeiculoRequisicao;

/** Opções que o hook passou ao useMutation na última chamada. */
function opcoesCapturadas(): OpcoesEdicao {
  return useMutationMock.mock.calls[0][0] as unknown as OpcoesEdicao;
}

describe('useEditarVeiculo', () => {
  let invalidateQueries: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    useMutationMock.mockReset();
    invalidateQueries = vi.fn().mockResolvedValue(undefined);
    useQueryClientMock.mockReturnValue({ invalidateQueries } as never);
  });

  it('envia o veículo e os campos para a edição da API', async () => {
    const editar = vi.mocked(editarVeiculo).mockResolvedValue(undefined);
    useEditarVeiculo();

    await opcoesCapturadas().mutationFn({ idVeiculo: 7, requisicao: REQUISICAO });

    expect(editar).toHaveBeenCalledWith(7, REQUISICAO);
  });

  it('ao dar certo, invalida a família de veículos (detalhe, fotos e listas)', async () => {
    useEditarVeiculo();

    await opcoesCapturadas().onSuccess();

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
  });
});
