import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { buscarComparacaoSaida, buscarContextoDevolucao, registrarSaida } from './devolucao';
import { ErroApi } from '@/types/api';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));
const get = vi.mocked(api.get);
const post = vi.mocked(api.post);
describe('API da devolução', () => {
  beforeEach(() => vi.clearAllMocks());
  it('consome os dois GETs contratados e propaga sinal de cancelamento', async () => {
    get.mockResolvedValue({ data: { teste: true } });
    const signal = new AbortController().signal;
    expect(await buscarContextoDevolucao(42, signal)).toEqual({ teste: true });
    expect(get).toHaveBeenCalledWith('/api/locacoes/42/devolucao', { signal });
    await buscarComparacaoSaida(42);
    expect(get).toHaveBeenCalledWith('/api/locacoes/42/vistorias/saida');
  });
  it('envia uma origem e uma nova sem copiar manualmente dados preexistentes', async () => {
    const dados = { kmRegistrado: 120, nivelCombustivel: 0, observacoes: null, idsAvariasEntrada: [8], novasAvarias: [{ descricao: 'Risco', gravidade: 'LEVE' as const, valorEstimado: 0 }] };
    post.mockResolvedValueOnce({ data: { avariasPreexistentes: [{ idAvariaEntrada: 8 }], avariasNovas: [{ descricao: 'Risco' }] } });
    const resposta = await registrarSaida(42, dados);
    expect(post).toHaveBeenCalledWith('/api/locacoes/42/vistorias/saida', dados);
    expect(resposta.avariasNovas).toHaveLength(1);
  });
  it.each([0, 400, 401, 403, 404, 409, 500])('propaga erro %s sem mascarar como ausência', async status => {
    const erro = new ErroApi({ status, mensagem: 'Falha na consulta.' });
    get.mockRejectedValueOnce(erro); post.mockRejectedValueOnce(erro);
    await expect(buscarContextoDevolucao(42)).rejects.toBe(erro);
    await expect(registrarSaida(42, { kmRegistrado: 0, nivelCombustivel: 0, observacoes: null, idsAvariasEntrada: [], novasAvarias: [] })).rejects.toBe(erro);
  });
});
