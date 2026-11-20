import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { buscarContextoVistoriaEntrada, registrarVistoriaEntrada } from './vistorias';
import { ErroApi } from '@/types/api';

vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));
const get = vi.mocked(api.get);
const post = vi.mocked(api.post);
const locacao = { idLocacao: 42, idReserva: 8, idVeiculo: 3 };
const vistoria = { idVistoria: 5, idLocacao: 42, tipo: 'ENTRADA' };

describe('contrato da vistoria de entrada', () => {
  beforeEach(() => { get.mockReset(); post.mockReset(); });

  it('consulta primeiro a locação e retorna uma vistoria já cadastrada', async () => {
    get.mockResolvedValueOnce({ data: locacao }).mockResolvedValueOnce({ data: vistoria });
    expect(await buscarContextoVistoriaEntrada(42)).toEqual({ locacao, vistoria });
    expect(get.mock.calls.map(([url]) => url)).toEqual(['/api/locacoes/42', '/api/locacoes/42/vistorias/entrada']);
  });

  it('404 da vistoria após locação válida libera formulário', async () => {
    get.mockResolvedValueOnce({ data: locacao }).mockRejectedValueOnce(new ErroApi({ status: 404, mensagem: 'Não encontrada.' }));
    expect(await buscarContextoVistoriaEntrada(42)).toEqual({ locacao, vistoria: null });
  });

  it.each([401, 403, 404, 500])('erro %s da locação não é tratado como vistoria ausente', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Não foi possível consultar.' });
    get.mockRejectedValueOnce(erro);
    await expect(buscarContextoVistoriaEntrada(42)).rejects.toBe(erro);
    expect(get).toHaveBeenCalledTimes(1);
  });

  it.each([0, 401, 403, 500])('erro %s da vistoria não libera formulário', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Não foi possível consultar.' });
    get.mockResolvedValueOnce({ data: locacao }).mockRejectedValueOnce(erro);
    await expect(buscarContextoVistoriaEntrada(42)).rejects.toBe(erro);
  });

  it('envia POST para rota contratada e propaga 409', async () => {
    const dados = { kmRegistrado: 0, nivelCombustivel: 0, observacoes: null };
    post.mockResolvedValueOnce({ data: vistoria });
    expect(await registrarVistoriaEntrada(42, dados)).toEqual(vistoria);
    expect(post).toHaveBeenCalledWith('/api/locacoes/42/vistorias/entrada', dados);
    const erro = new ErroApi({ status: 409, mensagem: 'Já existe vistoria.' });
    post.mockRejectedValueOnce(erro);
    await expect(registrarVistoriaEntrada(42, dados)).rejects.toBe(erro);
  });
});
