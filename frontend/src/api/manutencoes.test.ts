import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  abrirManutencao,
  buscarResumoManutencao,
  encerrarManutencao,
  listarAlertasRevisao,
  listarManutencoes,
  montarParametrosListagemManutencoes,
} from '@/api/manutencoes';
import type { AbrirManutencaoRequisicao, EncerrarManutencaoRequisicao, FiltroManutencao } from '@/types/manutencao';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const patchMock = vi.mocked(api.patch);

const FILTRO_BASE: FiltroManutencao = { pagina: 1, tamanhoPagina: 20 };

describe('montarParametrosListagemManutencoes', () => {
  it('leva só a página e o tamanho quando não há filtro', () => {
    expect(montarParametrosListagemManutencoes(FILTRO_BASE)).toEqual({ pagina: 1, tamanhoPagina: 20 });
  });

  it('apara a busca e não envia busca vazia ou só com espaços', () => {
    expect(montarParametrosListagemManutencoes({ ...FILTRO_BASE, busca: '  BRA2E19  ' })).toMatchObject({
      busca: 'BRA2E19',
    });
    expect(montarParametrosListagemManutencoes({ ...FILTRO_BASE, busca: '   ' })).not.toHaveProperty('busca');
    expect(montarParametrosListagemManutencoes({ ...FILTRO_BASE, busca: '' })).not.toHaveProperty('busca');
  });

  it('envia tipo, situação e idVeiculo só quando definidos', () => {
    const parametros = montarParametrosListagemManutencoes({
      ...FILTRO_BASE,
      tipo: 'CORRETIVA',
      situacao: 'ABERTA',
      idVeiculo: 7,
    });

    expect(parametros).toEqual({ pagina: 1, tamanhoPagina: 20, tipo: 'CORRETIVA', situacao: 'ABERTA', idVeiculo: 7 });
  });

  it('não envia nenhum filtro opcional quando não foram escolhidos', () => {
    const parametros = montarParametrosListagemManutencoes(FILTRO_BASE);

    expect(parametros).not.toHaveProperty('tipo');
    expect(parametros).not.toHaveProperty('situacao');
    expect(parametros).not.toHaveProperty('idVeiculo');
  });
});

describe('listarManutencoes', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 20 } });
  });

  it('faz GET em /api/manutencoes com os parâmetros da listagem', async () => {
    const pagina = await listarManutencoes({ ...FILTRO_BASE, tipo: 'PREVENTIVA' });

    expect(getMock.mock.calls[0]).toEqual([
      '/api/manutencoes',
      { params: { pagina: 1, tamanhoPagina: 20, tipo: 'PREVENTIVA' } },
    ]);
    expect(pagina).toEqual({ itens: [], total: 0, pagina: 1, tamanhoPagina: 20 });
  });
});

describe('buscarResumoManutencao', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { ordensAbertas: 18 } });
  });

  it('faz GET em /api/manutencoes/resumo, sem parâmetros (o período é o mês corrente)', async () => {
    const resumo = await buscarResumoManutencao();

    expect(getMock.mock.calls[0]).toEqual(['/api/manutencoes/resumo']);
    expect(resumo).toEqual({ ordensAbertas: 18 });
  });
});

describe('listarAlertasRevisao', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { total: 0, itens: [], intervaloRevisaoKm: 10000 } });
  });

  it('faz GET em /api/manutencoes/alertas', async () => {
    const alertas = await listarAlertasRevisao();

    expect(getMock.mock.calls[0]).toEqual(['/api/manutencoes/alertas']);
    expect(alertas).toEqual({ total: 0, itens: [], intervaloRevisaoKm: 10000 });
  });
});

describe('abrirManutencao', () => {
  const REQUISICAO: AbrirManutencaoRequisicao = {
    idVeiculo: 7,
    tipo: 'PREVENTIVA',
    oficina: 'Oficina Central',
    dataEntrada: '2026-11-20',
  };

  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { idManutencao: 3 } });
  });

  it('faz POST em /api/manutencoes com o corpo e devolve a ordem criada', async () => {
    const ordem = await abrirManutencao(REQUISICAO);

    expect(postMock.mock.calls[0]).toEqual(['/api/manutencoes', REQUISICAO]);
    expect(ordem).toEqual({ idManutencao: 3 });
  });

  it('não manda quilometragem no corpo (o servidor usa a do veículo)', async () => {
    await abrirManutencao(REQUISICAO);

    const [, corpo] = postMock.mock.calls[0];
    expect(corpo).not.toHaveProperty('kmAtual');
    expect(corpo).not.toHaveProperty('kmRegistrado');
  });
});

describe('encerrarManutencao', () => {
  const REQUISICAO: EncerrarManutencaoRequisicao = { kmAtual: 40120, dataSaida: '2026-11-21', custo: 890.5 };

  beforeEach(() => {
    patchMock.mockReset();
    patchMock.mockResolvedValue({ data: { idManutencao: 12, aberta: false } });
  });

  it('faz PATCH em /api/manutencoes/{id}/encerrar com o corpo e devolve a ordem encerrada', async () => {
    const ordem = await encerrarManutencao(12, REQUISICAO);

    expect(patchMock.mock.calls[0]).toEqual(['/api/manutencoes/12/encerrar', REQUISICAO]);
    expect(ordem).toEqual({ idManutencao: 12, aberta: false });
  });
});
