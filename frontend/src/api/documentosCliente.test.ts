import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AxiosProgressEvent } from 'axios';
import { api } from '@/api/client';
import {
  CAMPO_ARQUIVO_DOCUMENTO,
  CAMPO_TIPO_DOCUMENTO,
  enviarDocumentoCliente,
  listarDocumentosCliente,
  TEMPO_LIMITE_ENVIO_DOCUMENTO_MS,
} from '@/api/documentosCliente';
import type { RespostaDocumentoCliente } from '@/types/documentosCliente';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);

function progresso(loaded: number, total?: number): AxiosProgressEvent {
  return { loaded, total, bytes: loaded, rate: 1, estimated: 0, upload: true } as unknown as AxiosProgressEvent;
}

/** Resposta do backend com link e prazo (o front não pode guardar nenhum dos dois). */
const DOCUMENTO: RespostaDocumentoCliente = {
  idClienteDocumento: 3,
  tipo: 'CNH',
  nomeArquivo: 'cnh.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 2048,
  dataEnvio: '19/11/2026 10:00',
  url: 'https://armazenamento.exemplo/leitura?assinatura=segredo',
  expiraEm: '19/11/2026 10:15:00',
};

/** O que a tela recebe: os campos que ela usa, sem link nem prazo. */
const GRAVADO = {
  idClienteDocumento: 3,
  tipo: 'CNH',
  nomeArquivo: 'cnh.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 2048,
  dataEnvio: '19/11/2026 10:00',
};

describe('enviarDocumentoCliente', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: DOCUMENTO } as never);
  });

  it('faz POST em /api/clientes/{id}/documentos com multipart de tipo e arquivo', async () => {
    const arquivo = new File(['%PDF-1.4'], 'cnh.pdf', { type: 'application/pdf' });

    const resposta = await enviarDocumentoCliente(7, 'CNH', arquivo);

    expect(resposta).toEqual(GRAVADO);
    expect(resposta).not.toHaveProperty('url');
    expect(resposta).not.toHaveProperty('expiraEm');
    expect(JSON.stringify(resposta)).not.toContain('assinatura');
    const [url, corpo, config] = postMock.mock.calls[0];
    expect(url).toBe('/api/clientes/7/documentos');
    expect(corpo).toBeInstanceOf(FormData);
    const formulario = corpo as FormData;
    expect(formulario.get(CAMPO_TIPO_DOCUMENTO)).toBe('CNH');
    expect(formulario.get(CAMPO_ARQUIVO_DOCUMENTO)).toBeInstanceOf(File);
    expect((formulario.get(CAMPO_ARQUIVO_DOCUMENTO) as File).name).toBe('cnh.pdf');
    expect(config?.timeout).toBe(TEMPO_LIMITE_ENVIO_DOCUMENTO_MS);
  });

  it('manda o tipo COMPROVANTE_RESIDENCIA como texto e repassa o sinal de cancelamento', async () => {
    const arquivo = new File(['%PDF-1.4'], 'conta.pdf', { type: 'application/pdf' });
    const controle = new AbortController();

    await enviarDocumentoCliente(9, 'COMPROVANTE_RESIDENCIA', arquivo, { sinal: controle.signal });

    const [url, corpo, config] = postMock.mock.calls[0];
    expect(url).toBe('/api/clientes/9/documentos');
    expect((corpo as FormData).get(CAMPO_TIPO_DOCUMENTO)).toBe('COMPROVANTE_RESIDENCIA');
    expect(config?.signal).toBe(controle.signal);
  });

  it('informa o progresso de 0 a 100 durante o envio', async () => {
    const arquivo = new File(['%PDF-1.4'], 'cnh.pdf', { type: 'application/pdf' });
    const percentuais: number[] = [];
    postMock.mockImplementationOnce(async (_url, _corpo, config) => {
      config?.onUploadProgress?.(progresso(1, 4));
      config?.onUploadProgress?.(progresso(4, 4));
      config?.onUploadProgress?.(progresso(3, 0));
      return { data: DOCUMENTO } as never;
    });

    await enviarDocumentoCliente(7, 'CNH', arquivo, { aoProgredir: (p) => percentuais.push(p) });

    expect(percentuais).toEqual([25, 100]);
  });
});

describe('listarDocumentosCliente', () => {
  beforeEach(() => {
    getMock.mockReset();
  });

  it('faz GET em /api/clientes/{id}/documentos e devolve a lista sem o link de leitura', async () => {
    getMock.mockResolvedValue({ data: [DOCUMENTO] } as never);

    const lista = await listarDocumentosCliente(7);

    expect(lista).toEqual([GRAVADO]);
    expect(lista[0]).not.toHaveProperty('url');
    expect(lista[0]).not.toHaveProperty('expiraEm');
    expect(getMock).toHaveBeenCalledWith('/api/clientes/7/documentos');
  });

  it('resposta que não é lista vira lista vazia', async () => {
    getMock.mockResolvedValue({ data: null } as never);

    expect(await listarDocumentosCliente(7)).toEqual([]);
  });
});
