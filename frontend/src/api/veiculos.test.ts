import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarVeiculo, cadastrarVeiculo, editarVeiculo } from '@/api/veiculos';
import type { VeiculoRequisicao } from '@/types/veiculos';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const putMock = vi.mocked(api.put);

const REQUISICAO: VeiculoRequisicao = {
  idCategoria: 2,
  placa: 'BRA2E19',
  chassi: '9BWZZZ377VT004251',
  renavam: '00984521360',
  marca: 'Fiat',
  modelo: 'Argo Drive 1.0',
  anoFabricacao: 2023,
  anoModelo: 2024,
  cor: 'Prata Shark',
  kmAtual: 12,
  valorDiaria: 229,
  valorVenda: null,
  dataAquisicao: '2024-10-15',
  fornecedor: 'Jeep Dahruj SP Motors',
  numeroNotaFiscalCompra: 'NF-e 492.180',
};

describe('buscarVeiculo', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { idVeiculo: 7 } });
  });

  it('faz GET em /api/veiculos/{id} e devolve o veículo', async () => {
    const veiculo = await buscarVeiculo(7);

    expect(getMock.mock.calls[0][0]).toBe('/api/veiculos/7');
    expect(veiculo).toEqual({ idVeiculo: 7 });
  });
});

describe('cadastrarVeiculo', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { idVeiculo: 9 } });
  });

  it('faz POST em /api/veiculos com o corpo do pedido e devolve o retorno com o id', async () => {
    const veiculo = await cadastrarVeiculo(REQUISICAO);

    expect(postMock.mock.calls[0]).toEqual(['/api/veiculos', REQUISICAO]);
    expect(veiculo).toEqual({ idVeiculo: 9 });
  });

  it('não manda status nem empresa no corpo', async () => {
    await cadastrarVeiculo(REQUISICAO);

    const [, corpo] = postMock.mock.calls[0];
    expect(corpo).not.toHaveProperty('status');
    expect(corpo).not.toHaveProperty('idEmpresa');
  });
});

describe('editarVeiculo', () => {
  beforeEach(() => {
    putMock.mockReset();
    putMock.mockResolvedValue({ data: undefined });
  });

  it('faz PUT em /api/veiculos/{id} com os 13 campos e não devolve corpo (204)', async () => {
    const resultado = await editarVeiculo(7, REQUISICAO);

    expect(putMock.mock.calls[0]).toEqual(['/api/veiculos/7', REQUISICAO]);
    expect(resultado).toBeUndefined();
  });
});
