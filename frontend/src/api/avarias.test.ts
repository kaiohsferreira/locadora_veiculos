import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { CAMPO_FOTO_AVARIA, enviarFotoAvaria, listarAvarias, registrarAvaria, removerFotoAvaria, TEMPO_LIMITE_ENVIO_FOTO_AVARIA_MS } from '@/api/avarias';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { MENSAGEM_AVARIA_ERRO_LISTA } from '@/lib/mensagensVistoriaAvarias';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const putMock = vi.mocked(api.put);
const deleteMock = vi.mocked(api.delete);

const AVARIA: AvariaRegistrada = {
  idAvaria: 5, idVistoria: 2, descricao: 'Risco', gravidade: 'LEVE', valorEstimado: null, angulo: 'FRENTE',
  posX: 10, posY: 20, foto: null, erroFoto: null,
};

function dados(parcial: Partial<DadosNovaAvaria> = {}): DadosNovaAvaria {
  return {
    descricao: 'Risco', gravidade: 'LEVE', valorEstimado: null, angulo: null, posX: null, posY: null,
    foto: new File(['bytes'], 'foto.jpg', { type: 'image/jpeg' }), ...parcial,
  };
}

describe('listarAvarias', () => {
  beforeEach(() => vi.clearAllMocks());

  it('lê a lista da vistoria pela rota do contrato', async () => {
    getMock.mockResolvedValue({ data: [AVARIA] } as never);
    await expect(listarAvarias(2)).resolves.toEqual([AVARIA]);
    expect(getMock).toHaveBeenCalledWith('/api/vistorias/2/avarias');
  });

  it('resposta que não é lista vira erro da lista, e não uma lista vazia que pareça "sem avarias"', async () => {
    getMock.mockResolvedValue({ data: { erro: 'x' } } as never);
    await expect(listarAvarias(2)).rejects.toMatchObject({ name: 'ErroApi', mensagem: MENSAGEM_AVARIA_ERRO_LISTA });
  });

  it('lista vazia de verdade continua sendo lista vazia', async () => {
    getMock.mockResolvedValue({ data: [] } as never);
    await expect(listarAvarias(2)).resolves.toEqual([]);
  });
});

describe('registrarAvaria', () => {
  beforeEach(() => vi.clearAllMocks());

  it('envia o formulário com a foto e só os campos preenchidos, com o limite de tempo próprio', async () => {
    postMock.mockResolvedValue({ data: AVARIA } as never);
    await registrarAvaria(2, dados({ valorEstimado: '150.50', angulo: 'FRENTE', posX: '10.00', posY: '20.00' }));
    const [caminho, formulario, opcoes] = postMock.mock.calls[0] as unknown as [string, FormData, { timeout: number }];
    expect(caminho).toBe('/api/vistorias/2/avarias');
    expect(formulario.get('descricao')).toBe('Risco');
    expect(formulario.get('gravidade')).toBe('LEVE');
    expect(formulario.get('valorEstimado')).toBe('150.50');
    expect(formulario.get('angulo')).toBe('FRENTE');
    expect(formulario.get('posX')).toBe('10.00');
    expect(formulario.get(CAMPO_FOTO_AVARIA)).toBeInstanceOf(File);
    expect(opcoes.timeout).toBe(TEMPO_LIMITE_ENVIO_FOTO_AVARIA_MS);
  });

  it('sem valor, sem ângulo e sem posição, esses campos não vão no formulário', async () => {
    postMock.mockResolvedValue({ data: AVARIA } as never);
    await registrarAvaria(2, dados());
    const [, formulario] = postMock.mock.calls[0] as unknown as [string, FormData];
    expect(formulario.has('valorEstimado')).toBe(false);
    expect(formulario.has('angulo')).toBe(false);
    expect(formulario.has('posX')).toBe(false);
    expect(formulario.has('posY')).toBe(false);
  });
});

describe('enviarFotoAvaria', () => {
  beforeEach(() => vi.clearAllMocks());

  it('repete só a foto pela rota da avaria, com o campo foto', async () => {
    putMock.mockResolvedValue({ data: AVARIA } as never);
    const arquivo = new File(['bytes'], 'nova.png', { type: 'image/png' });
    await enviarFotoAvaria(2, 5, arquivo);
    const [caminho, formulario] = putMock.mock.calls[0] as unknown as [string, FormData];
    expect(caminho).toBe('/api/vistorias/2/avarias/5/foto');
    expect(formulario.get(CAMPO_FOTO_AVARIA)).toBeInstanceOf(File);
    expect(postMock).not.toHaveBeenCalled();
  });
});

describe('removerFotoAvaria', () => {
  beforeEach(() => vi.clearAllMocks());

  it('remove só o arquivo da foto pela rota da avaria, sem POST nem PUT', async () => {
    deleteMock.mockResolvedValue({} as never);
    await removerFotoAvaria(2, 5);
    expect(deleteMock).toHaveBeenCalledWith('/api/vistorias/2/avarias/5/foto');
    expect(postMock).not.toHaveBeenCalled();
    expect(putMock).not.toHaveBeenCalled();
  });
});
