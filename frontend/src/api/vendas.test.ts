import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarVeiculoAVenda, enviarPropostaCompra, simularParcelamento } from '@/api/vendas';
import type { PropostaCompraRequisicao } from '@/types/vendas';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);

describe('buscarVeiculoAVenda', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { idVeiculo: 4, marca: 'Jeep' } });
  });

  it('faz GET em /api/veiculos/a-venda/{id} e devolve o veículo', async () => {
    const veiculo = await buscarVeiculoAVenda(4);

    expect(getMock).toHaveBeenCalledWith('/api/veiculos/a-venda/4');
    expect(veiculo.marca).toBe('Jeep');
  });
});

describe('simularParcelamento', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { valorParcela: 3333.33, valorUltimaParcela: 3333.34 } });
  });

  it('faz GET em /api/veiculos/{id}/simulacao com entrada e parcelas na query', async () => {
    const simulacao = await simularParcelamento(4, { valorEntrada: 0, numeroParcelas: 3 });

    expect(getMock).toHaveBeenCalledWith('/api/veiculos/4/simulacao', {
      params: { valorEntrada: 0, numeroParcelas: 3 },
    });
    expect(simulacao.valorUltimaParcela).toBe(3333.34);
  });
});

describe('enviarPropostaCompra', () => {
  const REQUISICAO: PropostaCompraRequisicao = { idVeiculo: 4, valorEntrada: 20000, numeroParcelas: 12 };

  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { idPropostaCompra: 11, status: 'ENVIADA' } });
  });

  it('faz POST em /api/propostas-compra só com veículo, entrada e parcelas', async () => {
    const proposta = await enviarPropostaCompra(REQUISICAO);

    expect(postMock).toHaveBeenCalledWith('/api/propostas-compra', REQUISICAO);
    expect(proposta.idPropostaCompra).toBe(11);
  });

  it('propaga o erro da API', async () => {
    const erro = new Error('falhou');
    postMock.mockRejectedValue(erro);

    await expect(enviarPropostaCompra(REQUISICAO)).rejects.toBe(erro);
  });
});
