import { describe, expect, it, vi } from 'vitest';
import { AxiosError, type AxiosAdapter, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { api } from './client';
import { MENSAGEM_ERRO_SERVIDOR } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';

vi.mock('@/lib/sessao', () => ({
  obterToken: () => null,
  limparSessao: vi.fn(),
}));

/** Adaptador que responde com erro HTTP, sem rede: o axios aceita um adaptador por requisição. */
function respondendoComErro(status: number, dados: unknown): AxiosAdapter {
  return (config: InternalAxiosRequestConfig) => {
    const resposta = { data: dados, status, statusText: '', headers: {}, config } as unknown as AxiosResponse;
    return Promise.reject(new AxiosError('Request failed', 'ERR_BAD_RESPONSE', config, null, resposta));
  };
}

async function erroDaChamada(status: number, dados: unknown): Promise<ErroApi> {
  try {
    await api.get('/api/teste', { adapter: respondendoComErro(status, dados) });
  } catch (erro: unknown) {
    if (erro instanceof ErroApi) return erro;
    throw erro;
  }
  throw new Error('A chamada deveria ter falhado.');
}

describe('client: ErroApi carrega o corpo do ProblemDetails', () => {
  it('502 de foto de avaria: idAvaria e retryUploadUrl chegam ao ErroApi', async () => {
    const erro = await erroDaChamada(502, {
      title: 'Falha em serviço externo',
      status: 502,
      detail: 'A avaria foi criada, mas o upload da foto falhou. Repita o envio pela rota informada.',
      idAvaria: 7,
      retryUploadUrl: '/api/vistorias/2/avarias/7/foto',
    });
    expect(erro).toBeInstanceOf(ErroApi);
    expect(erro.status).toBe(502);
    expect(erro.problema?.idAvaria).toBe(7);
    expect(erro.problema?.retryUploadUrl).toBe('/api/vistorias/2/avarias/7/foto');
  });

  it.each([
    ['texto', '7'],
    ['zero', 0],
    ['negativo', -3],
    ['decimal', 2.5],
  ])('idAvaria inválido (%s) é descartado', async (_nome, valor) => {
    const erro = await erroDaChamada(502, { status: 502, idAvaria: valor });
    expect(erro.problema?.idAvaria).toBeUndefined();
  });

  it('corpo que não é ProblemDetails deixa o campo problema ausente', async () => {
    const erro = await erroDaChamada(502, 'texto solto');
    expect(erro.problema).toBeUndefined();
  });

  it('erro comum continua com a mensagem do detail, sem mudar', async () => {
    const erro = await erroDaChamada(404, { status: 404, detail: 'Registro não encontrado.' });
    expect(erro.status).toBe(404);
    expect(erro.mensagem).toBe('Registro não encontrado.');
    expect(erro.problema?.detail).toBe('Registro não encontrado.');
  });

  it('500 continua com a mensagem genérica, sem corpo aproveitado na mensagem', async () => {
    const erro = await erroDaChamada(500, { status: 500, detail: 'Erro interno do servidor.' });
    expect(erro.mensagem).toBe(MENSAGEM_ERRO_SERVIDOR);
  });
});
