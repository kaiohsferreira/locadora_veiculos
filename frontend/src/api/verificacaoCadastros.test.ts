import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  buscarVerificacaoCliente,
  decidirVerificacaoCliente,
  listarDocumentosVerificacao,
  listarFilaVerificacao,
  montarParametrosFilaVerificacao,
} from '@/api/verificacaoCadastros';
import { requisicaoDecisao } from '@/features/clientes/verificacao/regras';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const patchMock = vi.mocked(api.patch);

describe('montarParametrosFilaVerificacao', () => {
  it('leva página e tamanho, e o status só quando escolhido', () => {
    expect(montarParametrosFilaVerificacao({ pagina: 2, tamanhoPagina: 10 })).toEqual({
      pagina: 2,
      tamanhoPagina: 10,
    });
    expect(montarParametrosFilaVerificacao({ pagina: 1, tamanhoPagina: 10, status: 'PENDENTE' })).toEqual({
      pagina: 1,
      tamanhoPagina: 10,
      status: 'PENDENTE',
    });
  });

  it('apara a busca e não envia busca vazia nem só com espaços', () => {
    expect(montarParametrosFilaVerificacao({ pagina: 1, tamanhoPagina: 10, busca: '  Ana  ' })).toMatchObject({
      busca: 'Ana',
    });
    expect(montarParametrosFilaVerificacao({ pagina: 1, tamanhoPagina: 10, busca: '   ' })).not.toHaveProperty('busca');
    expect(montarParametrosFilaVerificacao({ pagina: 1, tamanhoPagina: 10, busca: '' })).not.toHaveProperty('busca');
  });
});

describe('listarFilaVerificacao', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });
  });

  it('faz GET em /api/clientes/verificacoes com os parâmetros da fila', async () => {
    const pagina = await listarFilaVerificacao({ pagina: 1, tamanhoPagina: 10, status: 'APROVADO', busca: 'x@y.com' });

    expect(getMock.mock.calls[0][0]).toBe('/api/clientes/verificacoes');
    expect(getMock.mock.calls[0][1]).toEqual({
      params: { pagina: 1, tamanhoPagina: 10, status: 'APROVADO', busca: 'x@y.com' },
    });
    expect(pagina.total).toBe(0);
  });
});

describe('buscarVerificacaoCliente', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { idCliente: 7 } });
  });

  it('faz GET em /api/clientes/{id}/verificacao', async () => {
    const cliente = await buscarVerificacaoCliente(7);

    expect(getMock.mock.calls[0][0]).toBe('/api/clientes/7/verificacao');
    expect(cliente).toEqual({ idCliente: 7 });
  });
});

describe('listarDocumentosVerificacao', () => {
  beforeEach(() => {
    getMock.mockReset();
  });

  it('faz GET em /api/clientes/{id}/verificacao/documentos e devolve a lista com o link', async () => {
    getMock.mockResolvedValue({ data: [{ tipo: 'CNH', url: 'https://arquivo.local/a' }] });

    const documentos = await listarDocumentosVerificacao(7);

    expect(getMock.mock.calls[0][0]).toBe('/api/clientes/7/verificacao/documentos');
    expect(documentos).toEqual([{ tipo: 'CNH', url: 'https://arquivo.local/a' }]);
  });

  it('resposta que não é lista vira lista vazia', async () => {
    getMock.mockResolvedValue({ data: null });

    expect(await listarDocumentosVerificacao(7)).toEqual([]);
  });
});

describe('decidirVerificacaoCliente', () => {
  beforeEach(() => {
    patchMock.mockReset();
    patchMock.mockResolvedValue({ data: undefined });
  });

  it('recusa com a justificativa aparada no corpo do PATCH', async () => {
    await decidirVerificacaoCliente(7, requisicaoDecisao('RECUSADO', '  Foto ilegível  '));

    expect(patchMock.mock.calls[0][0]).toBe('/api/clientes/7/verificacao');
    expect(patchMock.mock.calls[0][1]).toEqual({ decisao: 'RECUSADO', justificativa: 'Foto ilegível' });
  });

  it('aprova sem justificativa no corpo', async () => {
    await decidirVerificacaoCliente(7, requisicaoDecisao('APROVADO', 'texto que não deve ir'));

    expect(patchMock.mock.calls[0][1]).toEqual({ decisao: 'APROVADO' });
  });
});
