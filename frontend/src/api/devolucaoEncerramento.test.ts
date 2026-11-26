import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { buscarCobrancasLocacao, encerrarLocacao } from './devolucao';
import { listarDevolucoesPendentes } from './locacoes';
import { ErroApi } from '@/types/api';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));

const get = vi.mocked(api.get);
const post = vi.mocked(api.post);

describe('API do encerramento e das cobranças da devolução', () => {
  beforeEach(() => vi.clearAllMocks());

  it('o encerramento envia só política, versão e total (nada de caução, diária, km ou cartão)', async () => {
    const corpoComIntruso = { idConfiguracao: 3, versaoEsperada: 7, totalConfirmado: 360, valorCaucao: 1200, idEmpresa: 9 };
    post.mockResolvedValueOnce({ data: { ok: true } });
    expect(await encerrarLocacao(42, corpoComIntruso as Parameters<typeof encerrarLocacao>[1])).toEqual({ ok: true });
    expect(post).toHaveBeenCalledWith('/api/locacoes/42/devolucao', { idConfiguracao: 3, versaoEsperada: 7, totalConfirmado: 360 });
  });

  it('o erro do encerramento chega sem ser escondido (502 e 409 para a tela tratar)', async () => {
    const erro = new ErroApi({ status: 502, mensagem: 'Provedor fora.' });
    post.mockRejectedValueOnce(erro);
    await expect(encerrarLocacao(42, { idConfiguracao: 3, versaoEsperada: 7, totalConfirmado: 360 })).rejects.toBe(erro);
  });

  it('as cobranças da locação são um GET simples, sem corpo', async () => {
    get.mockResolvedValueOnce({ data: { idLocacao: 42, total: 0, totalStr: 'R$ 0,00', cobrancas: [] } });
    expect(await buscarCobrancasLocacao(42)).toEqual({ idLocacao: 42, total: 0, totalStr: 'R$ 0,00', cobrancas: [] });
    expect(get).toHaveBeenCalledWith('/api/locacoes/42/cobrancas', { signal: undefined });
  });
});

describe('API da lista de devoluções pendentes', () => {
  beforeEach(() => vi.clearAllMocks());

  it('consulta GET /api/locacoes com o status e a paginação, e devolve a página', async () => {
    const pagina = { itens: [], total: 0, pagina: 2, tamanhoPagina: 10 };
    get.mockResolvedValueOnce({ data: pagina });
    expect(await listarDevolucoesPendentes({ status: 'EM_ANDAMENTO', pagina: 2, tamanhoPagina: 10 })).toEqual(pagina);
    expect(get).toHaveBeenCalledWith('/api/locacoes', { params: { status: 'EM_ANDAMENTO', pagina: 2, tamanhoPagina: 10 } });
  });

  it('o filtro ATRASADA também sai como vai para a API', async () => {
    get.mockResolvedValueOnce({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });
    await listarDevolucoesPendentes({ status: 'ATRASADA', pagina: 1, tamanhoPagina: 10 });
    expect(get).toHaveBeenCalledWith('/api/locacoes', { params: { status: 'ATRASADA', pagina: 1, tamanhoPagina: 10 } });
  });
});
