import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarRetiradasDeHoje } from '@/api/painelOperacional';
import type { RespostaPainelOperacionalHoje } from '@/types/painelOperacional';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);

const RESPOSTA: RespostaPainelOperacionalHoje = {
  data: '2026-11-24',
  dataStr: '24/11/2026',
  retiradas: [
    {
      origem: 'RESERVA', idReserva: 31, idLocacao: null, codigoReserva: 'AV-00031', dataHora: '24/11/2026 00:00',
      status: 'CONFIRMADA', idCliente: 5, nomeCliente: 'Ana Souza', idVeiculo: 2, placa: 'ABC1D23', marca: 'Jeep', modelo: 'Renegade',
    },
  ],
  devolucoes: [],
};

describe('buscarRetiradasDeHoje', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: RESPOSTA });
  });

  it('chama o endpoint do painel de hoje, sem parâmetros', async () => {
    await buscarRetiradasDeHoje();

    expect(getMock).toHaveBeenCalledWith('/api/painel-operacional/hoje');
  });

  it('devolve o corpo da resposta sem alterar os campos (codigoReserva inclusive)', async () => {
    const dados = await buscarRetiradasDeHoje();

    expect(dados).toEqual(RESPOSTA);
    expect(dados.retiradas[0]?.codigoReserva).toBe('AV-00031');
  });

  it('repassa o erro do cliente HTTP sem tratar', async () => {
    const falha = new Error('falha de rede');
    getMock.mockRejectedValueOnce(falha);

    await expect(buscarRetiradasDeHoje()).rejects.toBe(falha);
  });
});
