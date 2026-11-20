import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { abrirManutencao, encerrarManutencao } from '@/api/manutencoes';
import { useAbrirManutencao } from './useAbrirManutencao';
import { useEncerrarManutencao } from './useEncerrarManutencao';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));
vi.mock('@/api/manutencoes', () => ({
  abrirManutencao: vi.fn(),
  encerrarManutencao: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const useQueryClientMock = vi.mocked(useQueryClient);

interface OpcoesMutacao {
  mutationFn: (variaveis: unknown) => Promise<unknown>;
  onSettled: () => Promise<unknown>;
}

function opcoesCapturadas(): OpcoesMutacao {
  return useMutationMock.mock.calls[0][0] as unknown as OpcoesMutacao;
}

describe('useAbrirManutencao', () => {
  let invalidateQueries: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    useMutationMock.mockReset();
    invalidateQueries = vi.fn().mockResolvedValue(undefined);
    useQueryClientMock.mockReturnValue({ invalidateQueries } as never);
  });

  it('envia o pedido de abertura para a API', async () => {
    const abrir = vi.mocked(abrirManutencao).mockResolvedValue({} as never);
    useAbrirManutencao();

    await opcoesCapturadas().mutationFn({ idVeiculo: 7, tipo: 'PREVENTIVA' });

    expect(abrir).toHaveBeenCalledWith({ idVeiculo: 7, tipo: 'PREVENTIVA' });
  });

  it('depois de qualquer resposta invalida manutenção, veículos e visão geral', async () => {
    useAbrirManutencao();

    await opcoesCapturadas().onSettled();

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['manutencoes'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['painel-visao-geral'] });
  });
});

describe('useEncerrarManutencao', () => {
  beforeEach(() => {
    useMutationMock.mockReset();
    useQueryClientMock.mockReturnValue({ invalidateQueries: vi.fn().mockResolvedValue(undefined) } as never);
  });

  it('envia o id da ordem e o corpo do encerramento para a API', async () => {
    const encerrar = vi.mocked(encerrarManutencao).mockResolvedValue({} as never);
    useEncerrarManutencao();

    await opcoesCapturadas().mutationFn({ idManutencao: 12, requisicao: { kmAtual: 40120 } });

    expect(encerrar).toHaveBeenCalledWith(12, { kmAtual: 40120 });
  });
});
