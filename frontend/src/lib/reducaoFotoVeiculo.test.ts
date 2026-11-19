import { afterEach, describe, expect, it, vi } from 'vitest';
import { lerCabecalhoFoto, dimensoesOrientadas, type CabecalhoFoto } from './cabecalhoFoto';
import {
  type AmbienteReducao,
  type BitmapDecodificado,
  calcularDimensoesReduzidas,
  COR_FUNDO_FOTO_ENVIADA,
  criarAmbienteReducaoDoNavegador,
  decidirPreparo,
  detectarSuporteReducao,
  ehOffscreenCanvas,
  IMAGEM_TESTE_SONDA_A,
  IMAGEM_TESTE_SONDA_B,
  LADO_MAXIMO_FOTO_ENVIADA_PIXELS,
  memorizarSuporteReducao,
  nomeDoArquivoReduzido,
  prepararFotoParaEnvio,
  QUALIDADE_JPEG_FOTO_ENVIADA,
  type SuporteReducao,
} from './reducaoFotoVeiculo';

const SUPORTE_COMPLETO: SuporteReducao = { reduz: true, aplicaOrientacao: true };

function cabecalhoJpeg(largura: number, altura: number, orientacao: CabecalhoFoto['orientacao'] = 1): CabecalhoFoto {
  return { formato: 'jpeg', largura, altura, orientacao };
}

function cabecalhoPng(largura: number, altura: number): CabecalhoFoto {
  return { formato: 'png', largura, altura, orientacao: 1 };
}

/** Arquivo de teste com tamanho e data controlados (o conteúdo é só um texto). */
function arquivoFoto(nome: string, tipo: string, tamanho = 5_000_000, lastModified = 1_700_000_000_000): File {
  const arquivo = new File(['conteudo'], nome, { type: tipo, lastModified });
  Object.defineProperty(arquivo, 'size', { value: tamanho });
  return arquivo;
}

function bitmapFalso(largura: number, altura: number) {
  return { width: largura, height: altura, close: vi.fn<() => void>() };
}

/** Ambiente falso: o bitmap sai com a dimensão pedida (ou a original, sem redimensionar). */
function criarAmbienteFalso(largura: number, altura: number, sobrescrever: Partial<AmbienteReducao> = {}) {
  const bitmaps: Array<ReturnType<typeof bitmapFalso>> = [];
  const ambiente: AmbienteReducao = {
    criarBitmap: vi.fn(async (_arquivo: Blob, opcoes: ImageBitmapOptions) => {
      const bitmap = bitmapFalso(opcoes.resizeWidth ?? largura, opcoes.resizeHeight ?? altura);
      bitmaps.push(bitmap);
      return bitmap;
    }),
    codificarJpeg: vi.fn(async () => new Blob([new Uint8Array(100)], { type: 'image/jpeg' })),
    lerCorDoPixel: vi.fn((): [number, number, number] | null => null),
    criarArquivo: vi.fn((partes: BlobPart[], nome: string, opcoes: FilePropertyBag) => new File(partes, nome, opcoes)),
    ...sobrescrever,
  };
  return { ambiente, bitmaps };
}

interface ConfigNavegador {
  ignoraQualidade?: boolean;
  recusaOrientacao?: boolean;
  naoGira?: boolean;
  coresTrocadas?: boolean;
  larguraSonda2?: number;
  semCor?: boolean;
}

/**
 * Navegador de mentira para a sonda: lê as opções (o que o navegador de verdade faz), entrega o tamanho pedido e
 * as cores da imagem de teste B (vermelho em cima, azul embaixo) ou as cores que o caso pede.
 */
function navegadorDeTeste(config: ConfigNavegador = {}): AmbienteReducao {
  return {
    criarBitmap: vi.fn(async (_arquivo: Blob, opcoes: ImageBitmapOptions): Promise<BitmapDecodificado> => {
      void opcoes.imageOrientation;
      void opcoes.resizeWidth;
      void opcoes.resizeHeight;
      if (!config.ignoraQualidade) void opcoes.resizeQuality;
      if (config.recusaOrientacao) throw new TypeError('valor recusado');
      const largura =
        config.larguraSonda2 !== undefined && opcoes.resizeWidth === 4 ? config.larguraSonda2 : (opcoes.resizeWidth ?? 16);
      const altura = opcoes.resizeHeight ?? 8;
      return { width: largura, height: altura, close: vi.fn<() => void>() };
    }),
    codificarJpeg: vi.fn(async () => null),
    lerCorDoPixel: vi.fn((_bitmap: BitmapDecodificado, _x: number, y: number): [number, number, number] | null => {
      if (config.semCor) return null;
      if (config.naoGira) return [255, 0, 0];
      const emCima = y < 4;
      const vermelho: [number, number, number] = [255, 0, 0];
      const azul: [number, number, number] = [0, 0, 255];
      if (config.coresTrocadas) return emCima ? azul : vermelho;
      return emCima ? vermelho : azul;
    }),
    criarArquivo: vi.fn((partes: BlobPart[], nome: string, opcoes: FilePropertyBag) => new File(partes, nome, opcoes)),
  };
}

describe('calcularDimensoesReduzidas', () => {
  it('mantém a proporção e deixa o lado maior em até 1600', () => {
    expect(calcularDimensoesReduzidas(4000, 3000, 1600)).toEqual({ largura: 1600, altura: 1200 });
    expect(calcularDimensoesReduzidas(3000, 4000, 1600)).toEqual({ largura: 1200, altura: 1600 });
    expect(calcularDimensoesReduzidas(4001, 3000, 1600)).toEqual({ largura: 1600, altura: 1200 });
  });

  it('não amplia imagem pequena nem muda imagem que já cabe', () => {
    expect(calcularDimensoesReduzidas(1600, 1200, 1600)).toEqual({ largura: 1600, altura: 1200 });
    expect(calcularDimensoesReduzidas(800, 600, 1600)).toEqual({ largura: 800, altura: 600 });
  });

  it('nenhum lado sai menor que 1', () => {
    expect(calcularDimensoesReduzidas(10000, 1, 1600)).toEqual({ largura: 1600, altura: 1 });
    expect(calcularDimensoesReduzidas(8000, 1, 1600)).toEqual({ largura: 1600, altura: 1 });
  });

  it('zero, negativo, NaN, fracionário e infinito lançam RangeError', () => {
    expect(() => calcularDimensoesReduzidas(0, 10, 1600)).toThrow(RangeError);
    expect(() => calcularDimensoesReduzidas(10, -1, 1600)).toThrow(RangeError);
    expect(() => calcularDimensoesReduzidas(Number.NaN, 10, 1600)).toThrow(RangeError);
    expect(() => calcularDimensoesReduzidas(10.5, 10, 1600)).toThrow(RangeError);
    expect(() => calcularDimensoesReduzidas(10, 10, Number.POSITIVE_INFINITY)).toThrow(RangeError);
  });
});

describe('calcularDimensoesReduzidas: mesmos pares do teste do servidor (RegrasVeiculoFotoTests)', () => {
  it.each([
    [4000, 3000, 1600, 1200],
    [3000, 4000, 1200, 1600],
    [800, 600, 800, 600],
    [8000, 1, 1600, 1],
  ])('%dx%d com lado maior até 1600 vira %dx%d', (largura, altura, larguraEsperada, alturaEsperada) => {
    expect(calcularDimensoesReduzidas(largura, altura, 1600)).toEqual({ largura: larguraEsperada, altura: alturaEsperada });
  });
});

describe('decidirPreparo', () => {
  it('sem suporte: navegador_sem_suporte', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(4000, 3000), { reduz: false, aplicaOrientacao: false })).toEqual({
      acao: 'enviar_original',
      motivo: 'navegador_sem_suporte',
    });
  });

  it('cabeçalho ilegível: sem_cabecalho', () => {
    expect(decidirPreparo('image/jpeg', null, SUPORTE_COMPLETO)).toEqual({ acao: 'enviar_original', motivo: 'sem_cabecalho' });
  });

  it('formato do cabeçalho diferente do tipo do arquivo: tipo_divergente', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoPng(4000, 3000), SUPORTE_COMPLETO)).toEqual({
      acao: 'enviar_original',
      motivo: 'tipo_divergente',
    });
  });

  it('orientação desconhecida: orientacao_desconhecida', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(4000, 3000, null), SUPORTE_COMPLETO)).toEqual({
      acao: 'enviar_original',
      motivo: 'orientacao_desconhecida',
    });
  });

  it('orientação diferente de 1 sem suporte à orientação: orientacao_sem_suporte', () => {
    expect(
      decidirPreparo('image/jpeg', cabecalhoJpeg(4000, 3000, 6), { reduz: true, aplicaOrientacao: false }),
    ).toEqual({ acao: 'enviar_original', motivo: 'orientacao_sem_suporte' });
  });

  it('JPEG com lado de até 1600 não é recodificado: jpeg_pequeno', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(1600, 1200), SUPORTE_COMPLETO)).toEqual({
      acao: 'enviar_original',
      motivo: 'jpeg_pequeno',
    });
  });

  it('JPEG 1200x1600 com orientação 6 e suporte: jpeg_pequeno (o servidor aplica a orientação)', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(1200, 1600, 6), SUPORTE_COMPLETO)).toEqual({
      acao: 'enviar_original',
      motivo: 'jpeg_pequeno',
    });
  });

  it('JPEG 4000x3000 com orientação 1: reduz para 1600x1200 com redimensionamento', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO)).toEqual({
      acao: 'reduzir',
      largura: 1600,
      altura: 1200,
      redimensiona: true,
    });
  });

  it('JPEG 4000x3000 com orientação 6 e suporte: reduz para 1200x1600', () => {
    expect(decidirPreparo('image/jpeg', cabecalhoJpeg(4000, 3000, 6), SUPORTE_COMPLETO)).toEqual({
      acao: 'reduzir',
      largura: 1200,
      altura: 1600,
      redimensiona: true,
    });
  });

  it('PNG pequeno vira JPEG sem redimensionar', () => {
    expect(decidirPreparo('image/png', cabecalhoPng(800, 600), SUPORTE_COMPLETO)).toEqual({
      acao: 'reduzir',
      largura: 800,
      altura: 600,
      redimensiona: false,
    });
  });

  it('PNG 4000x3000 reduz para 1600x1200', () => {
    expect(decidirPreparo('image/png', cabecalhoPng(4000, 3000), SUPORTE_COMPLETO)).toEqual({
      acao: 'reduzir',
      largura: 1600,
      altura: 1200,
      redimensiona: true,
    });
  });
});

describe('nomeDoArquivoReduzido', () => {
  it('troca a extensão por .jpg, acrescenta quando não há e usa foto.jpg para nome vazio', () => {
    expect(nomeDoArquivoReduzido('IMG_1.PNG')).toBe('IMG_1.jpg');
    expect(nomeDoArquivoReduzido('a.b.jpeg')).toBe('a.b.jpg');
    expect(nomeDoArquivoReduzido('foto')).toBe('foto.jpg');
    expect(nomeDoArquivoReduzido('')).toBe('foto.jpg');
  });
});

describe('prepararFotoParaEnvio', () => {
  it('decisão de enviar o original não chama o ambiente', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');
    const { ambiente } = criarAmbienteFalso(4000, 3000);

    const resultado = await prepararFotoParaEnvio(
      arquivo,
      cabecalhoJpeg(4000, 3000),
      { reduz: false, aplicaOrientacao: false },
      ambiente,
    );

    expect(ambiente.criarBitmap).not.toHaveBeenCalled();
    expect(resultado.arquivo).toBe(arquivo);
    expect(resultado.reduzida).toBe(false);
  });

  it('ambiente nulo devolve o original', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, null);

    expect(resultado).toEqual({ arquivo, reduzida: false });
  });

  it('caminho feliz: JPEG 1600x1200 de 4000x3000, com as opções reais, fundo branco e qualidade 0,90', async () => {
    const arquivo = arquivoFoto('foto.jpg', 'image/jpeg', 5_000_000, 1_700_000_000_123);
    const { ambiente, bitmaps } = criarAmbienteFalso(4000, 3000);

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(ambiente.criarBitmap).toHaveBeenCalledWith(arquivo, {
      imageOrientation: 'from-image',
      resizeQuality: 'high',
      resizeWidth: 1600,
      resizeHeight: 1200,
    });
    expect(ambiente.codificarJpeg).toHaveBeenCalledWith(
      bitmaps[0],
      1600,
      1200,
      COR_FUNDO_FOTO_ENVIADA,
      QUALIDADE_JPEG_FOTO_ENVIADA,
    );
    expect(bitmaps[0].close).toHaveBeenCalledTimes(1);
    expect(resultado.reduzida).toBe(true);
    expect(resultado.arquivo.name).toBe('foto.jpg');
    expect(resultado.arquivo.type).toBe('image/jpeg');
    expect(resultado.arquivo.lastModified).toBe(1_700_000_000_123);
  });

  it('sem redimensionar, as opções não trazem resizeWidth nem resizeHeight', async () => {
    const { ambiente } = criarAmbienteFalso(800, 600);

    const resultado = await prepararFotoParaEnvio(
      arquivoFoto('foto.png', 'image/png'),
      cabecalhoPng(800, 600),
      SUPORTE_COMPLETO,
      ambiente,
    );

    const opcoes = vi.mocked(ambiente.criarBitmap).mock.calls[0][1];
    expect(opcoes).not.toHaveProperty('resizeWidth');
    expect(opcoes).not.toHaveProperty('resizeHeight');
    expect(resultado.reduzida).toBe(true);
    expect(resultado.arquivo.name).toBe('foto.jpg');
  });

  it('criarBitmap que rejeita devolve o original', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');
    const { ambiente } = criarAmbienteFalso(4000, 3000, {
      criarBitmap: vi.fn(async () => {
        throw new TypeError('falha');
      }),
    });

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(resultado).toEqual({ arquivo, reduzida: false });
  });

  it('bitmap com dimensão inesperada devolve o original e fecha o bitmap', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');
    const bitmap = bitmapFalso(1600, 1199);
    const { ambiente } = criarAmbienteFalso(4000, 3000, { criarBitmap: vi.fn(async () => bitmap) });

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(resultado).toEqual({ arquivo, reduzida: false });
    expect(ambiente.codificarJpeg).not.toHaveBeenCalled();
    expect(bitmap.close).toHaveBeenCalledTimes(1);
  });

  it.each([
    ['nulo', null],
    ['vazio', new Blob([], { type: 'image/jpeg' })],
    ['de outro tipo', new Blob([new Uint8Array(10)], { type: 'image/png' })],
  ])('codificação %s devolve o original', async (_nome, resultadoCodificacao) => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');
    const { ambiente } = criarAmbienteFalso(4000, 3000, { codificarJpeg: vi.fn(async () => resultadoCodificacao) });

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(resultado).toEqual({ arquivo, reduzida: false });
  });

  it('resultado maior ou igual ao original devolve o original', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg', 50);
    const { ambiente } = criarAmbienteFalso(4000, 3000);

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(resultado).toEqual({ arquivo, reduzida: false });
  });

  it('falha na codificação devolve o original e fecha o bitmap', async () => {
    const arquivo = arquivoFoto('a.jpg', 'image/jpeg');
    const { ambiente, bitmaps } = criarAmbienteFalso(4000, 3000, {
      codificarJpeg: vi.fn(async () => {
        throw new TypeError('falha');
      }),
    });

    const resultado = await prepararFotoParaEnvio(arquivo, cabecalhoJpeg(4000, 3000), SUPORTE_COMPLETO, ambiente);

    expect(resultado).toEqual({ arquivo, reduzida: false });
    expect(bitmaps[0].close).toHaveBeenCalledTimes(1);
  });
});

describe('detectarSuporteReducao', () => {
  it('sem ambiente: nada é reduzido', async () => {
    await expect(detectarSuporteReducao(null)).resolves.toEqual({ reduz: false, aplicaOrientacao: false });
  });

  it('navegador que ignora resizeQuality: não reduz', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ ignoraQualidade: true }))).resolves.toEqual({
      reduz: false,
      aplicaOrientacao: false,
    });
  });

  it('navegador que não gira a imagem: reduz, mas não aplica a orientação', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ naoGira: true }))).resolves.toEqual({
      reduz: true,
      aplicaOrientacao: false,
    });
  });

  it('navegador que devolve dimensão diferente de 4x8 na sonda: orientação falsa', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ larguraSonda2: 5 }))).resolves.toEqual({
      reduz: true,
      aplicaOrientacao: false,
    });
  });

  it('pixels trocados (cima azul, baixo vermelho): orientação falsa', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ coresTrocadas: true }))).resolves.toEqual({
      reduz: true,
      aplicaOrientacao: false,
    });
  });

  it('leitura de pixel nula: orientação falsa', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ semCor: true }))).resolves.toEqual({
      reduz: true,
      aplicaOrientacao: false,
    });
  });

  it('navegador que rejeita o valor from-image: os dois falsos', async () => {
    await expect(detectarSuporteReducao(navegadorDeTeste({ recusaOrientacao: true }))).resolves.toEqual({
      reduz: false,
      aplicaOrientacao: false,
    });
  });

  it('caminho feliz: vermelho em cima e azul embaixo, com as mesmas opções do caminho real', async () => {
    const ambiente = navegadorDeTeste();

    await expect(detectarSuporteReducao(ambiente)).resolves.toEqual({ reduz: true, aplicaOrientacao: true });
    expect(vi.mocked(ambiente.criarBitmap).mock.calls[1][1]).toEqual({
      imageOrientation: 'from-image',
      resizeWidth: 4,
      resizeHeight: 8,
      resizeQuality: 'high',
    });
  });

  it('criarBitmap que rejeita: os dois falsos', async () => {
    const ambiente = navegadorDeTeste();
    vi.mocked(ambiente.criarBitmap).mockRejectedValueOnce(new TypeError('falha'));

    await expect(detectarSuporteReducao(ambiente)).resolves.toEqual({ reduz: false, aplicaOrientacao: false });
  });
});

describe('memorizarSuporteReducao', () => {
  it('detecta uma vez e devolve a mesma promessa nas chamadas seguintes', async () => {
    const detectar = vi.fn(async (): Promise<SuporteReducao> => SUPORTE_COMPLETO);
    const obter = memorizarSuporteReducao(detectar);

    await expect(obter()).resolves.toEqual(SUPORTE_COMPLETO);
    await expect(obter()).resolves.toEqual(SUPORTE_COMPLETO);
    expect(detectar).toHaveBeenCalledTimes(1);
  });
});

describe('imagens de teste da sonda', () => {
  it('a imagem B tem 16x8 e orientação 6; com ela aplicada fica 8x16', () => {
    const cabecalho = lerCabecalhoFoto(IMAGEM_TESTE_SONDA_B);

    expect(cabecalho).toEqual({ formato: 'jpeg', largura: 16, altura: 8, orientacao: 6 });
    expect(cabecalho === null ? null : dimensoesOrientadas(cabecalho)).toEqual({ largura: 8, altura: 16 });
  });

  it('a imagem A é um PNG de 2x2', () => {
    expect(lerCabecalhoFoto(IMAGEM_TESTE_SONDA_A)).toEqual({ formato: 'png', largura: 2, altura: 2, orientacao: 1 });
  });

  it('o lado máximo do envio é 1600 px', () => {
    expect(LADO_MAXIMO_FOTO_ENVIADA_PIXELS).toBe(1600);
  });
});

describe('ehOffscreenCanvas (V3-01: navegador sem OffscreenCanvas não lança)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('navegador sem OffscreenCanvas: falso, sem ReferenceError', () => {
    expect(() => ehOffscreenCanvas({} as HTMLCanvasElement, false)).not.toThrow();
    expect(ehOffscreenCanvas({} as HTMLCanvasElement, false)).toBe(false);
  });

  it('navegador com OffscreenCanvas: verdadeiro só para a própria classe', () => {
    class ClasseOffscreenDeTeste {}
    vi.stubGlobal('OffscreenCanvas', ClasseOffscreenDeTeste);

    expect(ehOffscreenCanvas(new ClasseOffscreenDeTeste() as unknown as OffscreenCanvas, true)).toBe(true);
    expect(ehOffscreenCanvas({} as HTMLCanvasElement, true)).toBe(false);
  });
});

describe('criarAmbienteReducaoDoNavegador', () => {
  it('sem createImageBitmap (fora do navegador): nulo', () => {
    expect(criarAmbienteReducaoDoNavegador()).toBeNull();
  });
});
