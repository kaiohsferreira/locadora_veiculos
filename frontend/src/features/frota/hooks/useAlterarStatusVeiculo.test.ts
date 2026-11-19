import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { alterarStatusVeiculo } from '@/api/veiculos';
import { ErroApi } from '@/types/api';
import { useAlterarStatusVeiculo } from './useAlterarStatusVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));
vi.mock('@/api/veiculos', () => ({
  alterarStatusVeiculo: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const useQueryClientMock = vi.mocked(useQueryClient);

interface OpcoesTroca {
  mutationFn: (variaveis: { idVeiculo: number; status: 'MANUTENCAO' }) => Promise<void>;
  onSettled: () => Promise<unknown>;
}

/** Opções que o hook passou ao useMutation na última chamada. */
function opcoesCapturadas(): OpcoesTroca {
  return useMutationMock.mock.calls[0][0] as unknown as OpcoesTroca;
}

describe('useAlterarStatusVeiculo', () => {
  let invalidateQueries: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    useMutationMock.mockReset();
    invalidateQueries = vi.fn().mockResolvedValue(undefined);
    useQueryClientMock.mockReturnValue({ invalidateQueries } as never);
  });

  it('envia o id e o status para a troca na API', async () => {
    const trocar = vi.mocked(alterarStatusVeiculo).mockResolvedValue(undefined);
    useAlterarStatusVeiculo();

    await opcoesCapturadas().mutationFn({ idVeiculo: 7, status: 'MANUTENCAO' });

    expect(trocar).toHaveBeenCalledWith(7, 'MANUTENCAO');
  });

  it('ao dar certo, invalida a família de veículos (lista e detalhe) e a visão geral', async () => {
    useAlterarStatusVeiculo();

    await opcoesCapturadas().onSettled();

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['painel-visao-geral'] });
  });

  it('o onSettled só termina depois das recargas (a mutação e a linha ficam bloqueadas até a lista atualizar)', async () => {
    useAlterarStatusVeiculo();
    const opcoes = opcoesCapturadas();
    let liberarRecarga: () => void = () => undefined;
    invalidateQueries.mockReturnValueOnce(
      new Promise<void>((resolver) => {
        liberarRecarga = resolver;
      }),
    );
    let terminou = false;

    const acabamento = opcoes.onSettled().then(() => {
      terminou = true;
    });
    await Promise.resolve();
    await Promise.resolve();
    expect(terminou).toBe(false);

    liberarRecarga();
    await acabamento;
    expect(terminou).toBe(true);
  });

  it('ao falhar, também invalida as duas famílias (o onSettled roda nos dois casos, sem depender do erro)', async () => {
    useAlterarStatusVeiculo();
    const opcoes = opcoesCapturadas();
    vi.mocked(alterarStatusVeiculo).mockRejectedValueOnce(new ErroApi({ status: 409, mensagem: 'Veículo com reserva.' }));

    await expect(opcoes.mutationFn({ idVeiculo: 7, status: 'MANUTENCAO' })).rejects.toBeInstanceOf(ErroApi);
    await opcoes.onSettled();

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['painel-visao-geral'] });
  });
});
