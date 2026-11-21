import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarVeiculoAVenda, enviarPropostaCompra, listarCatalogo, montarParametrosCatalogo, simularParcelamento } from '@/api/vendas';
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

describe('catálogo', () => {
  beforeEach(() => {
    getMock.mockReset();
  });

  it('monta a query só com os filtros informados e a marca aparada', () => {
    expect(montarParametrosCatalogo({ pagina: 2, tamanhoPagina: 20, marca: '  Jeep ', precoMaximo: 100000 })).toEqual({
      pagina: 2,
      tamanhoPagina: 20,
      marca: 'Jeep',
      precoMaximo: 100000,
    });
    expect(montarParametrosCatalogo({ pagina: 1, tamanhoPagina: 20, marca: '   ' })).toEqual({ pagina: 1, tamanhoPagina: 20 });
  });

  it('faz GET em /api/veiculos/a-venda com a query e devolve a página', async () => {
    getMock.mockResolvedValue({ data: { itens: [{ idVeiculo: 4 }], total: 21, pagina: 1, tamanhoPagina: 20 } });

    const resposta = await listarCatalogo({ pagina: 1, tamanhoPagina: 20, anoMinimo: 2021 });

    expect(getMock).toHaveBeenCalledWith('/api/veiculos/a-venda', { params: { pagina: 1, tamanhoPagina: 20, anoMinimo: 2021 } });
    expect(resposta.total).toBe(21);
  });

  it('resposta sem lista vira página vazia', async () => {
    getMock.mockResolvedValue({ data: null });

    const resposta = await listarCatalogo({ pagina: 3, tamanhoPagina: 20 });

    expect(resposta).toEqual({ itens: [], total: 0, pagina: 3, tamanhoPagina: 20 });
  });
});
