import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { cancelarReserva, listarReservas, montarParametrosListagemReservas } from '@/api/reservas';
import { ErroApi } from '@/types/api';
import type { FiltroReservas } from '@/types/reservas';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), delete: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const deleteMock = vi.mocked(api.delete);

const FILTRO_BASE: FiltroReservas = { pagina: 2, tamanhoPagina: 10, situacao: 'HISTORICO' };

describe('montarParametrosListagemReservas', () => {
  it('envia pagina, tamanhoPagina e situacao, e omite a busca quando ausente', () => {
    expect(montarParametrosListagemReservas(FILTRO_BASE)).toEqual({
      pagina: 2,
      tamanhoPagina: 10,
      situacao: 'HISTORICO',
    });
  });

  it('não envia busca vazia nem só com espaços, e envia a busca aparada', () => {
    expect(montarParametrosListagemReservas({ ...FILTRO_BASE, busca: '   ' })).not.toHaveProperty('busca');
    expect(montarParametrosListagemReservas({ ...FILTRO_BASE, busca: '  AV-00042  ' }).busca).toBe('AV-00042');
  });

  it('nunca envia o parâmetro status: a tela só usa situacao', () => {
    expect(montarParametrosListagemReservas(FILTRO_BASE)).not.toHaveProperty('status');
  });
});

describe('listarReservas', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 2, tamanhoPagina: 10 } });
  });

  it('faz GET em /api/reservas com os parâmetros e devolve a página', async () => {
    const resposta = await listarReservas({ ...FILTRO_BASE, busca: 'renegade' });

    expect(resposta).toEqual({ itens: [], total: 0, pagina: 2, tamanhoPagina: 10 });
    const [url, configuracao] = getMock.mock.calls[0];
    expect(url).toBe('/api/reservas');
    expect(configuracao?.params).toEqual({ pagina: 2, tamanhoPagina: 10, situacao: 'HISTORICO', busca: 'renegade' });
  });

  it('não coloca a busca na URL', async () => {
    await listarReservas({ ...FILTRO_BASE, busca: 'renegade' });
    expect(String(getMock.mock.calls[0][0])).not.toContain('renegade');
  });

  it('resposta vazia ou sem lista vira página vazia, sem quebrar', async () => {
    getMock.mockResolvedValueOnce({ data: '' });
    expect(await listarReservas({ ...FILTRO_BASE, situacao: 'ATIVAS' })).toEqual({
      itens: [],
      total: 0,
      pagina: 2,
      tamanhoPagina: 10,
    });

    getMock.mockResolvedValueOnce({ data: null });
    expect((await listarReservas(FILTRO_BASE)).itens).toEqual([]);

    getMock.mockResolvedValueOnce({ data: { sem: 'itens' } });
    expect((await listarReservas(FILTRO_BASE)).total).toBe(0);
  });

  it('erro da API (ex.: 404) propaga como ErroApi, sem ser engolido', async () => {
    const erro = new ErroApi({ status: 404, mensagem: 'A reserva não foi encontrada.' });
    getMock.mockRejectedValueOnce(erro);

    await expect(listarReservas(FILTRO_BASE)).rejects.toBe(erro);
  });
});

describe('cancelarReserva', () => {
  beforeEach(() => {
    deleteMock.mockReset();
    deleteMock.mockResolvedValue({ data: undefined });
  });

  it('faz DELETE em /api/reservas/{id} e não envia corpo', async () => {
    await cancelarReserva(42);

    expect(deleteMock.mock.calls[0]).toEqual(['/api/reservas/42']);
  });

  it('erro de conflito (409) propaga como ErroApi, com a mensagem do servidor', async () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Uma reserva concluída não pode ser cancelada.' });
    deleteMock.mockRejectedValueOnce(erro);

    await expect(cancelarReserva(42)).rejects.toBe(erro);
  });
});
