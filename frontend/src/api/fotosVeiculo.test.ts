import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AxiosProgressEvent } from 'axios';
import { api } from '@/api/client';
import {
  CAMPO_FOTO_VEICULO,
  enviarFotoVeiculo,
  listarFotosVeiculo,
  removerFotoVeiculo,
  TEMPO_LIMITE_ENVIO_FOTO_MS,
} from '@/api/fotosVeiculo';
import { ErroApi } from '@/types/api';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const deleteMock = vi.mocked(api.delete);

/** Evento de progresso do Axios com a forma mínima usada pelo código. */
function progresso(loaded: number, total?: number): AxiosProgressEvent {
  return { loaded, total, bytes: loaded, rate: 1, estimated: 0, upload: true } as unknown as AxiosProgressEvent;
}

const FOTO: FotoDoVeiculo = {
  idVeiculoFoto: 42,
  ordem: 1,
  url: 'https://armazenamento.exemplo/foto.jpg?assinatura=abc',
  urlMiniatura: null,
  expiraEm: '2026-11-16T13:15:00.0000000+00:00',
};

describe('listarFotosVeiculo', () => {
  beforeEach(() => {
    getMock.mockReset();
  });

  it('faz GET em /api/veiculos/{id}/fotos e devolve a lista', async () => {
    getMock.mockResolvedValueOnce({ data: [FOTO] });

    await expect(listarFotosVeiculo(7)).resolves.toEqual([FOTO]);
    expect(getMock.mock.calls[0]).toEqual(['/api/veiculos/7/fotos']);
  });

  it('resposta que não é lista vira lista vazia', async () => {
    getMock.mockResolvedValueOnce({ data: '' });
    await expect(listarFotosVeiculo(7)).resolves.toEqual([]);

    getMock.mockResolvedValueOnce({ data: { sem: 'lista' } });
    await expect(listarFotosVeiculo(7)).resolves.toEqual([]);
  });

  it('erro da API propaga como ErroApi, sem ser engolido', async () => {
    const erro = new ErroApi({ status: 404, mensagem: 'O veículo não foi encontrado.' });
    getMock.mockRejectedValueOnce(erro);

    await expect(listarFotosVeiculo(7)).rejects.toBe(erro);
  });
});

describe('enviarFotoVeiculo', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: FOTO });
  });

  it('envia um FormData com o arquivo no campo "foto" e devolve a foto gravada', async () => {
    const arquivo = new File(['imagem'], 'frente.png', { type: 'image/png' });

    await expect(enviarFotoVeiculo(7, arquivo)).resolves.toEqual(FOTO);

    const [url, corpo, configuracao] = postMock.mock.calls[0];
    expect(url).toBe('/api/veiculos/7/fotos');
    expect(corpo).toBeInstanceOf(FormData);
    expect(CAMPO_FOTO_VEICULO).toBe('foto');
    const enviado = (corpo as FormData).get(CAMPO_FOTO_VEICULO);
    expect(enviado).toBeInstanceOf(File);
    expect((enviado as File).name).toBe('frente.png');
    expect(configuracao?.headers).toBeUndefined();
  });

  it('tem tempo limite próprio de 120 s, maior que o padrão do cliente', async () => {
    const arquivo = new File(['imagem'], 'frente.png', { type: 'image/png' });
    await enviarFotoVeiculo(7, arquivo);

    expect(postMock.mock.calls[0][2]?.timeout).toBe(TEMPO_LIMITE_ENVIO_FOTO_MS);
    expect(TEMPO_LIMITE_ENVIO_FOTO_MS).toBe(120_000);
  });

  it('repassa o cancelamento e converte o progresso do envio para percentual de 0 a 100', async () => {
    const arquivo = new File(['imagem'], 'frente.png', { type: 'image/png' });
    const aoProgredir = vi.fn<(percentual: number) => void>();
    const controle = new AbortController();

    await enviarFotoVeiculo(7, arquivo, { aoProgredir, sinal: controle.signal });

    const configuracao = postMock.mock.calls[0][2];
    expect(configuracao?.signal).toBe(controle.signal);

    configuracao?.onUploadProgress?.(progresso(50, 200));
    configuracao?.onUploadProgress?.(progresso(200, 200));
    expect(aoProgredir.mock.calls.map((chamada) => chamada[0])).toEqual([25, 100]);
  });

  it('progresso sem total conhecido é ignorado', async () => {
    const arquivo = new File(['imagem'], 'frente.png', { type: 'image/png' });
    const aoProgredir = vi.fn<(percentual: number) => void>();

    await enviarFotoVeiculo(7, arquivo, { aoProgredir });
    postMock.mock.calls[0][2]?.onUploadProgress?.(progresso(50));

    expect(aoProgredir).not.toHaveBeenCalled();
  });

  it('erro do envio (ex.: 400, 409, 502) propaga como ErroApi', async () => {
    const arquivo = new File(['imagem'], 'frente.png', { type: 'image/png' });
    const erro = new ErroApi({ status: 409, mensagem: 'Limite de 15 fotos atingido.' });
    postMock.mockRejectedValueOnce(erro);

    await expect(enviarFotoVeiculo(7, arquivo)).rejects.toBe(erro);
  });
});

describe('removerFotoVeiculo', () => {
  beforeEach(() => {
    deleteMock.mockReset();
    deleteMock.mockResolvedValue({ data: undefined });
  });

  it('faz DELETE em /api/veiculos/{id}/fotos/{idFoto}, sem corpo', async () => {
    await removerFotoVeiculo(7, 42);

    expect(deleteMock.mock.calls[0]).toEqual(['/api/veiculos/7/fotos/42']);
  });

  it('erro (ex.: 502 do armazenamento) propaga como ErroApi', async () => {
    const erro = new ErroApi({ status: 502, mensagem: 'Armazenamento indisponível.' });
    deleteMock.mockRejectedValueOnce(erro);

    await expect(removerFotoVeiculo(7, 42)).rejects.toBe(erro);
  });
});
