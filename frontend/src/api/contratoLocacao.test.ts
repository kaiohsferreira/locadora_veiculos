import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { assinarContratoLocacao, buscarContratoLocacao } from './contratoLocacao';
import { ErroApi } from '@/types/api';
import { CONTRATO_PENDENTE } from '@/features/locacoes/contrato/contratoLocacao.fixture';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));
const get = vi.mocked(api.get);
const post = vi.mocked(api.post);

describe('API do contrato digital da locação', () => {
  beforeEach(() => vi.clearAllMocks());

  it('lê o contrato pelo GET contratado e propaga o sinal de cancelamento', async () => {
    get.mockResolvedValue({ data: CONTRATO_PENDENTE });
    const signal = new AbortController().signal;
    expect(await buscarContratoLocacao(15, signal)).toEqual(CONTRATO_PENDENTE);
    expect(get).toHaveBeenCalledWith('/api/locacoes/15/contrato', { signal });
  });

  it('envia só nome, aceites e PNG no POST de assinatura, sem campos que o servidor define', async () => {
    post.mockResolvedValueOnce({ data: CONTRATO_PENDENTE });
    await assinarContratoLocacao(15, {
      nomeAssinante: 'João Pereira Silva',
      aceiteCondicoesGerais: true,
      aceitePoliticaCombustivel: null,
      assinaturaPngBase64: 'iVBORw0KGgo=',
    });
    expect(post).toHaveBeenCalledWith('/api/locacoes/15/contrato/assinatura', {
      nomeAssinante: 'João Pereira Silva',
      aceiteCondicoesGerais: true,
      aceitePoliticaCombustivel: null,
      assinaturaPngBase64: 'iVBORw0KGgo=',
    });
  });

  it('propaga o 400 com os erros por campo do servidor (nomeAssinante e aceiteCondicoesGerais)', async () => {
    const erros = {
      nomeAssinante: ['O nome do assinante deve ter no máximo 100 caracteres.'],
      aceiteCondicoesGerais: ['Marque os aceites obrigatórios para assinar o contrato.'],
    };
    const erro = new ErroApi({ status: 400, mensagem: 'Requisição inválida.', erros });
    post.mockRejectedValueOnce(erro);
    await expect(
      assinarContratoLocacao(15, { nomeAssinante: 'Ana', aceiteCondicoesGerais: true, aceitePoliticaCombustivel: null, assinaturaPngBase64: 'x' }),
    ).rejects.toMatchObject({ status: 400, erros });
  });

  it.each([403, 404, 409])('propaga o erro %s sem mascarar', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Texto do servidor.' });
    get.mockRejectedValueOnce(erro);
    post.mockRejectedValueOnce(erro);
    await expect(buscarContratoLocacao(15)).rejects.toBe(erro);
    await expect(
      assinarContratoLocacao(15, { nomeAssinante: 'Ana', aceiteCondicoesGerais: true, aceitePoliticaCombustivel: null, assinaturaPngBase64: 'x' }),
    ).rejects.toBe(erro);
  });
});
