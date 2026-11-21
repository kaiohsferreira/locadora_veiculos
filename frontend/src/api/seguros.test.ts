import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarApolice, cadastrarApolice, editarApolice, listarApolices, montarParametrosApolices } from '@/api/seguros';
import { buscarVeiculosPorPlaca } from '@/api/veiculos';
import type { ApoliceRequisicao } from '@/types/seguros';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const putMock = vi.mocked(api.put);

const REQUISICAO: ApoliceRequisicao = {
  idVeiculo: 7,
  seguradora: 'Porto Seguro',
  numeroApolice: '123.456-7',
  dataInicio: '2026-01-01',
  dataFim: '2026-12-30',
  valorPremio: 3450.5,
  cobertura: null,
};

describe('api de seguros', () => {
  beforeEach(() => {
    getMock.mockReset();
    postMock.mockReset();
    putMock.mockReset();
  });

  it('monta a query só com o veículo escolhido e "apenasAVencer" ligado', () => {
    expect(montarParametrosApolices({ pagina: 1, tamanhoPagina: 20, apenasAVencer: false })).toEqual({ pagina: 1, tamanhoPagina: 20 });
    expect(montarParametrosApolices({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, idVeiculo: 7 })).toEqual({
      pagina: 2,
      tamanhoPagina: 20,
      idVeiculo: 7,
      apenasAVencer: true,
    });
  });

  it('lista em GET /api/seguros e trata resposta sem lista como página vazia', async () => {
    getMock.mockResolvedValue({ data: { itens: [{ idSeguro: 1 }], total: 1, pagina: 1, tamanhoPagina: 20 } });
    const pagina = await listarApolices({ pagina: 1, tamanhoPagina: 20, apenasAVencer: true });
    expect(getMock).toHaveBeenCalledWith('/api/seguros', { params: { pagina: 1, tamanhoPagina: 20, apenasAVencer: true } });
    expect(pagina.total).toBe(1);

    getMock.mockResolvedValue({ data: '' });
    expect(await listarApolices({ pagina: 3, tamanhoPagina: 20, apenasAVencer: false })).toEqual({
      itens: [],
      total: 0,
      pagina: 3,
      tamanhoPagina: 20,
    });
  });

  it('consulta, cadastra e edita nas rotas do contrato', async () => {
    getMock.mockResolvedValue({ data: { idSeguro: 3 } });
    postMock.mockResolvedValue({ data: { idSeguro: 9 } });
    putMock.mockResolvedValue({ data: undefined });

    expect((await buscarApolice(3)).idSeguro).toBe(3);
    expect(getMock).toHaveBeenCalledWith('/api/seguros/3');
    expect((await cadastrarApolice(REQUISICAO)).idSeguro).toBe(9);
    expect(postMock).toHaveBeenCalledWith('/api/seguros', REQUISICAO);
    await editarApolice(3, REQUISICAO);
    expect(putMock).toHaveBeenCalledWith('/api/seguros/3', REQUISICAO);
  });

  it('busca veículos por placa em GET /api/veiculos, no máximo 8', async () => {
    getMock.mockResolvedValue({ data: { itens: [{ idVeiculo: 7, placa: 'ABC1D23' }], total: 1 } });
    const veiculos = await buscarVeiculosPorPlaca(' abc ');
    expect(getMock).toHaveBeenCalledWith('/api/veiculos', { params: { placa: 'abc', pagina: 1, tamanhoPagina: 8 } });
    expect(veiculos).toHaveLength(1);

    getMock.mockResolvedValue({ data: null });
    expect(await buscarVeiculosPorPlaca('xyz')).toEqual([]);
  });
});
