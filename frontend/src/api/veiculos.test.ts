import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  alterarStatusVeiculo,
  buscarVeiculo,
  cadastrarVeiculo,
  editarVeiculo,
  listarVeiculos,
  montarParametrosListagemVeiculos,
} from '@/api/veiculos';
import type { VeiculoRequisicao } from '@/types/veiculos';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const putMock = vi.mocked(api.put);
const patchMock = vi.mocked(api.patch);

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

describe('alterarStatusVeiculo', () => {
  beforeEach(() => {
    patchMock.mockReset();
    patchMock.mockResolvedValue({ data: undefined });
  });

  it('faz PATCH em /api/veiculos/{id}/status com o corpo { status } e não devolve corpo (204)', async () => {
    const resultado = await alterarStatusVeiculo(7, 'MANUTENCAO');

    expect(patchMock.mock.calls[0]).toEqual(['/api/veiculos/7/status', { status: 'MANUTENCAO' }]);
    expect(resultado).toBeUndefined();
  });

  it('o corpo leva só o status (sem id, sem empresa)', async () => {
    await alterarStatusVeiculo(9, 'BAIXADO');

    const [, corpo] = patchMock.mock.calls[0];
    expect(corpo).toEqual({ status: 'BAIXADO' });
  });
});

describe('montarParametrosListagemVeiculos', () => {
  it('leva página e tamanho; status, categoria e busca só quando definidos', () => {
    expect(montarParametrosListagemVeiculos({ pagina: 2, tamanhoPagina: 25 })).toEqual({ pagina: 2, tamanhoPagina: 25 });
    expect(
      montarParametrosListagemVeiculos({ pagina: 1, tamanhoPagina: 10, status: 'LOCADO', idCategoria: 3, busca: 'Onix' }),
    ).toEqual({ pagina: 1, tamanhoPagina: 10, status: 'LOCADO', idCategoria: 3, busca: 'Onix' });
  });

  it('busca vazia ou só com espaços não vai na query', () => {
    expect(montarParametrosListagemVeiculos({ pagina: 1, tamanhoPagina: 10, busca: '' })).not.toHaveProperty('busca');
    expect(montarParametrosListagemVeiculos({ pagina: 1, tamanhoPagina: 10, busca: '   ' })).not.toHaveProperty('busca');
  });

  it('a busca sai aparada', () => {
    expect(montarParametrosListagemVeiculos({ pagina: 1, tamanhoPagina: 10, busca: '  Corolla ' }).busca).toBe('Corolla');
  });
});

describe('listarVeiculos', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });
  });

  it('faz GET em /api/veiculos com os parâmetros montados e devolve a página', async () => {
    const pagina = await listarVeiculos({ pagina: 1, tamanhoPagina: 10, status: 'MANUTENCAO', busca: 'Argo' });

    expect(getMock.mock.calls[0][0]).toBe('/api/veiculos');
    expect(getMock.mock.calls[0][1]).toEqual({
      params: { pagina: 1, tamanhoPagina: 10, status: 'MANUTENCAO', busca: 'Argo' },
    });
    expect(pagina).toEqual({ itens: [], total: 0, pagina: 1, tamanhoPagina: 10 });
  });
});
