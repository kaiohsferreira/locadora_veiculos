import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cadastrarVeiculo } from '@/api/veiculos';
import type { VeiculoRequisicao, VeiculoRetorno } from '@/types/veiculos';
import { useCadastrarVeiculo } from './useCadastrarVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));
vi.mock('@/api/veiculos', () => ({
  cadastrarVeiculo: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const useQueryClientMock = vi.mocked(useQueryClient);

interface OpcoesCadastro {
  mutationFn: (requisicao: VeiculoRequisicao) => Promise<VeiculoRetorno>;
  onSuccess: (veiculo: VeiculoRetorno) => Promise<unknown>;
}

const VEICULO_NOVO = { idVeiculo: 9 } as VeiculoRetorno;

/** Opções que o hook passou ao useMutation na última chamada. */
function opcoesCapturadas(): OpcoesCadastro {
  return useMutationMock.mock.calls[0][0] as unknown as OpcoesCadastro;
}

describe('useCadastrarVeiculo', () => {
  let invalidateQueries: ReturnType<typeof vi.fn>;
  let setQueryData: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    useMutationMock.mockReset();
    invalidateQueries = vi.fn().mockResolvedValue(undefined);
    setQueryData = vi.fn();
    useQueryClientMock.mockReturnValue({ invalidateQueries, setQueryData } as never);
  });

  it('usa o cadastro da API como função da mutação', () => {
    useCadastrarVeiculo();

    expect(opcoesCapturadas().mutationFn).toBe(cadastrarVeiculo);
  });

  it('ao dar certo, invalida a família de veículos', async () => {
    useCadastrarVeiculo();

    await opcoesCapturadas().onSuccess(VEICULO_NOVO);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
  });

  it('ao dar certo, invalida também a visão geral (cartões e pílulas da tela 11), sem esperar os 30 s do cache', async () => {
    useCadastrarVeiculo();

    await opcoesCapturadas().onSuccess(VEICULO_NOVO);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['painel-visao-geral'] });
  });

  it('ao dar certo, grava lista de fotos vazia na chave de fotos do veículo novo', async () => {
    useCadastrarVeiculo();

    await opcoesCapturadas().onSuccess(VEICULO_NOVO);

    expect(setQueryData).toHaveBeenCalledWith(['veiculos', 'fotos', 9], []);
  });
});
