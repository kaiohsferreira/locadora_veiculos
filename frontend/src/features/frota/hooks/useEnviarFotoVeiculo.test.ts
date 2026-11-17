import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation } from '@tanstack/react-query';
import { enviarFotoVeiculo } from '@/api/fotosVeiculo';
import { useEnviarFotoVeiculo } from './useEnviarFotoVeiculo';

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn(),
}));
vi.mock('@/api/fotosVeiculo', () => ({
  enviarFotoVeiculo: vi.fn(),
}));

const useMutationMock = vi.mocked(useMutation);
const enviarMock = vi.mocked(enviarFotoVeiculo);

describe('useEnviarFotoVeiculo', () => {
  beforeEach(() => {
    useMutationMock.mockReset();
    enviarMock.mockReset();
    enviarMock.mockResolvedValue({
      idVeiculoFoto: 1,
      ordem: 1,
      url: 'https://a/1',
      urlMiniatura: null,
      expiraEm: '2026-11-16T13:15:00.0000000+00:00',
    });
  });

  it('envia o arquivo do veículo com progresso e cancelamento repassados', async () => {
    useEnviarFotoVeiculo();
    const opcoes = useMutationMock.mock.calls[0][0];
    const arquivo = new File(['x'], 'frente.png', { type: 'image/png' });
    const aoProgredir = vi.fn<(percentual: number) => void>();
    const sinal = new AbortController().signal;

    await (opcoes.mutationFn as (variaveis: unknown) => Promise<unknown>)({ idVeiculo: 3, arquivo, aoProgredir, sinal });

    expect(enviarMock).toHaveBeenCalledWith(3, arquivo, { aoProgredir, sinal });
  });

  it('não invalida as listas ao terminar: quem envia em lote faz isso no fim do lote', () => {
    useEnviarFotoVeiculo();
    const opcoes = useMutationMock.mock.calls[0][0];

    expect(opcoes).not.toHaveProperty('onSuccess');
    expect(opcoes).not.toHaveProperty('onSettled');
  });
});
