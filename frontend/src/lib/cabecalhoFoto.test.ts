import { describe, expect, it, vi } from 'vitest';
import {
  BYTES_LIDOS_DO_CABECALHO,
  dimensoesOrientadas,
  lerCabecalhoDoArquivo,
  lerCabecalhoFoto,
  type CabecalhoFoto,
} from './cabecalhoFoto';

/** Segmento JPEG: marcador, comprimento (inclui os dois bytes do próprio comprimento) e dados. */
function segmento(marcador: number, dados: number[]): number[] {
  const comprimento = dados.length + 2;
  return [0xff, marcador, (comprimento >> 8) & 0xff, comprimento & 0xff, ...dados];
}

/** Quadro SOF com precisão 8, altura e largura (16 bits cada) e três componentes 4:4:4. */
function quadro(marcador: number, largura: number, altura: number): number[] {
  return segmento(marcador, [8, altura >> 8, altura & 0xff, largura >> 8, largura & 0xff, 3, 1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0]);
}

const EXIF = [0x45, 0x78, 0x69, 0x66, 0x00, 0x00];

/** Exif em ordem Intel com a marca de orientação (tipo 3, contagem 1). */
function exifIntel(orientacao: number): number[] {
  return segmento(0xe1, [
    ...EXIF, 0x49, 0x49, 0x2a, 0x00, 0x08, 0x00, 0x00, 0x00,
    0x01, 0x00, 0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientacao, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00,
  ]);
}

/** Exif em ordem Motorola com a marca de orientação. */
function exifMotorola(orientacao: number): number[] {
  return segmento(0xe1, [
    ...EXIF, 0x4d, 0x4d, 0x00, 0x2a, 0x00, 0x00, 0x00, 0x08,
    0x00, 0x01, 0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, orientacao, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00,
  ]);
}

/** Exif sem nenhuma entrada no diretório: a orientação é 1. */
function exifSemMarca(): number[] {
  return segmento(0xe1, [...EXIF, 0x49, 0x49, 0x2a, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
}

function jpeg(...partes: number[][]): Uint8Array<ArrayBuffer> {
  return new Uint8Array([0xff, 0xd8, ...partes.flat()]);
}

function u32(valor: number): number[] {
  return [(valor >>> 24) & 0xff, (valor >>> 16) & 0xff, (valor >>> 8) & 0xff, valor & 0xff];
}

function bloco(tipo: string, dados: number[]): number[] {
  return [...u32(dados.length), ...[...tipo].map((letra) => letra.charCodeAt(0)), ...dados, 0, 0, 0, 0];
}

function ihdr(largura: number, altura: number): number[] {
  return bloco('IHDR', [...u32(largura), ...u32(altura), 8, 2, 0, 0, 0]);
}

function png(...partes: number[][]): Uint8Array<ArrayBuffer> {
  return new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, ...partes.flat()]);
}

describe('lerCabecalhoFoto: PNG', () => {
  it('PNG mínimo válido: largura, altura e orientação 1', () => {
    const cabecalho = lerCabecalhoFoto(png(ihdr(2, 3), bloco('IDAT', [1, 2]), bloco('IEND', [])));

    expect(cabecalho).toEqual({ formato: 'png', largura: 2, altura: 3, orientacao: 1 });
  });

  it('eXIf antes do IDAT: orientação nula', () => {
    const cabecalho = lerCabecalhoFoto(png(ihdr(2, 2), bloco('eXIf', [0, 0]), bloco('IDAT', [1])));

    expect(cabecalho).toEqual({ formato: 'png', largura: 2, altura: 2, orientacao: null });
  });

  it('IDAT que não aparece nos bytes lidos: orientação nula', () => {
    const cabecalho = lerCabecalhoFoto(png(ihdr(2, 2), bloco('tEXt', [1, 2, 3])));

    expect(cabecalho).toEqual({ formato: 'png', largura: 2, altura: 2, orientacao: null });
  });

  it('dimensão zero: nulo', () => {
    expect(lerCabecalhoFoto(png(ihdr(0, 2), bloco('IDAT', [1])))).toBeNull();
    expect(lerCabecalhoFoto(png(ihdr(2, 0), bloco('IDAT', [1])))).toBeNull();
  });

  it('dimensão acima de 2147483647: nulo', () => {
    expect(lerCabecalhoFoto(png(ihdr(0x80000000, 2), bloco('IDAT', [1])))).toBeNull();
  });

  it('IHDR com comprimento diferente de 13: nulo', () => {
    const ihdrErrado = bloco('IHDR', [...u32(2), ...u32(2), 8, 2, 0, 0, 0, 0]);

    expect(lerCabecalhoFoto(png(ihdrErrado, bloco('IDAT', [1])))).toBeNull();
  });

  it('PNG cortado antes da altura: nulo', () => {
    const cortado = png(ihdr(2, 2)).slice(0, 20);

    expect(lerCabecalhoFoto(cortado)).toBeNull();
  });
});

describe('lerCabecalhoFoto: JPEG', () => {
  it('quadro C0: largura, altura e orientação 1 sem Exif', () => {
    expect(lerCabecalhoFoto(jpeg(quadro(0xc0, 16, 8)))).toEqual({ formato: 'jpeg', largura: 16, altura: 8, orientacao: 1 });
  });

  it('quadro progressivo (C2) também é lido', () => {
    expect(lerCabecalhoFoto(jpeg(quadro(0xc2, 640, 480)))).toEqual({ formato: 'jpeg', largura: 640, altura: 480, orientacao: 1 });
  });

  it('JFIF (E0) antes do Exif: a orientação vem do Exif', () => {
    const jfif = segmento(0xe0, [0x4a, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00]);

    expect(lerCabecalhoFoto(jpeg(jfif, exifIntel(6), quadro(0xc0, 4000, 3000)))).toEqual({
      formato: 'jpeg',
      largura: 4000,
      altura: 3000,
      orientacao: 6,
    });
  });

  it('segmento grande antes do quadro é pulado pelo comprimento', () => {
    expect(lerCabecalhoFoto(jpeg(segmento(0xe2, new Array<number>(300).fill(0x11)), quadro(0xc0, 10, 20)))).toEqual({
      formato: 'jpeg',
      largura: 10,
      altura: 20,
      orientacao: 1,
    });
  });

  it('bytes FF repetidos antes do marcador são ignorados', () => {
    expect(lerCabecalhoFoto(jpeg([0xff, 0xff, 0xff], quadro(0xc0, 16, 8)))).toEqual({
      formato: 'jpeg',
      largura: 16,
      altura: 8,
      orientacao: 1,
    });
  });

  it('marcador C4 não é quadro: a leitura segue até o quadro real', () => {
    const dht = segmento(0xc4, new Array<number>(17).fill(0));

    expect(lerCabecalhoFoto(jpeg(dht, quadro(0xc0, 16, 8)))).toEqual({
      formato: 'jpeg',
      largura: 16,
      altura: 8,
      orientacao: 1,
    });
  });

  it('Exif em ordem Intel com orientação 6', () => {
    expect(lerCabecalhoFoto(jpeg(exifIntel(6), quadro(0xc0, 4000, 3000)))?.orientacao).toBe(6);
  });

  it('Exif em ordem Motorola com orientação 8', () => {
    expect(lerCabecalhoFoto(jpeg(exifMotorola(8), quadro(0xc0, 4000, 3000)))?.orientacao).toBe(8);
  });

  it('Exif sem a marca de orientação: orientação 1', () => {
    expect(lerCabecalhoFoto(jpeg(exifSemMarca(), quadro(0xc0, 4000, 3000)))?.orientacao).toBe(1);
  });

  it('Exif malformado: nulo', () => {
    const malformado = segmento(0xe1, [...EXIF, 0x58, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    expect(lerCabecalhoFoto(jpeg(malformado, quadro(0xc0, 16, 8)))).toBeNull();
  });

  it('orientação 9 (fora de 1 a 8): nulo', () => {
    expect(lerCabecalhoFoto(jpeg(exifIntel(9), quadro(0xc0, 16, 8)))).toBeNull();
  });

  it('dois Exif com orientações diferentes: nulo; com a mesma: a orientação', () => {
    expect(lerCabecalhoFoto(jpeg(exifIntel(6), exifMotorola(8), quadro(0xc0, 16, 8)))).toBeNull();
    expect(lerCabecalhoFoto(jpeg(exifIntel(6), exifMotorola(6), quadro(0xc0, 16, 8)))?.orientacao).toBe(6);
  });

  it('JPEG cortado antes do quadro: nulo', () => {
    const cortado = jpeg(segmento(0xe2, new Array<number>(10).fill(0))).slice(0, 8);

    expect(lerCabecalhoFoto(cortado)).toBeNull();
  });

  it('quadro com altura zero: nulo', () => {
    expect(lerCabecalhoFoto(jpeg(quadro(0xc0, 16, 0)))).toBeNull();
  });

  it('início de dados (SOS) antes de qualquer quadro: nulo', () => {
    expect(lerCabecalhoFoto(jpeg(segmento(0xda, [3, 1, 0, 2, 0, 3, 0, 0, 63, 0])))).toBeNull();
  });
});

describe('lerCabecalhoFoto: outros formatos', () => {
  it('GIF, texto e vetor vazio: nulo', () => {
    const gif = Uint8Array.from([0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x02, 0x00, 0x02, 0x00, 0x80, 0x00, 0x00]);

    expect(lerCabecalhoFoto(gif)).toBeNull();
    expect(lerCabecalhoFoto(new TextEncoder().encode('texto qualquer sem imagem'))).toBeNull();
    expect(lerCabecalhoFoto(new Uint8Array(0))).toBeNull();
  });
});

/** Pares de dimensão do teste do servidor (RegrasVeiculoFotoTests), gerados aqui e lidos do cabeçalho. */
const PARES_DE_DIMENSAO_DO_SERVIDOR: ReadonlyArray<readonly [number, number]> = [
  [4000, 3000],
  [3000, 4000],
  [1600, 1200],
  [800, 600],
  [4001, 3000],
  [8000, 1],
];

describe('lerCabecalhoFoto: dimensões iguais às que o servidor lê para as mesmas imagens geradas no teste', () => {
  it.each(PARES_DE_DIMENSAO_DO_SERVIDOR)('JPEG %dx%d', (largura, altura) => {
    expect(lerCabecalhoFoto(jpeg(quadro(0xc0, largura, altura)))).toMatchObject({ formato: 'jpeg', largura, altura });
  });

  it.each(PARES_DE_DIMENSAO_DO_SERVIDOR)('PNG %dx%d', (largura, altura) => {
    expect(lerCabecalhoFoto(png(ihdr(largura, altura)))).toMatchObject({ formato: 'png', largura, altura });
  });
});

describe('lerCabecalhoDoArquivo', () => {
  it('arquivo menor que o bloco lido: lê tudo o que existe', async () => {
    const arquivo = new Blob([png(ihdr(3, 5), bloco('IDAT', [1]))]);

    await expect(lerCabecalhoDoArquivo(arquivo)).resolves.toEqual({ formato: 'png', largura: 3, altura: 5, orientacao: 1 });
  });

  it('leitura que rejeita devolve nulo, sem rejeitar', async () => {
    const arquivo = new Blob([png(ihdr(3, 5))]);

    await expect(lerCabecalhoDoArquivo(arquivo, () => Promise.reject(new Error('leitura')))).resolves.toBeNull();
  });

  it('navegador sem arrayBuffer no Blob devolve nulo', async () => {
    const arquivo = new Blob([png(ihdr(3, 5))]);

    await expect(
      lerCabecalhoDoArquivo(arquivo, () => {
        throw new TypeError('arrayBuffer ausente');
      }),
    ).resolves.toBeNull();
  });

  it('pede só o início do arquivo, nunca o arquivo inteiro', async () => {
    const arquivo = new Blob([new Uint8Array(2 * BYTES_LIDOS_DO_CABECALHO)]);
    const lerBytes = vi.fn(async (parte: Blob) => parte.arrayBuffer());

    await lerCabecalhoDoArquivo(arquivo, lerBytes);

    expect(lerBytes).toHaveBeenCalledTimes(1);
    expect(lerBytes.mock.calls[0][0].size).toBe(BYTES_LIDOS_DO_CABECALHO);
  });
});

describe('dimensoesOrientadas', () => {
  function cabecalho(orientacao: CabecalhoFoto['orientacao']): CabecalhoFoto {
    return { formato: 'jpeg', largura: 4000, altura: 3000, orientacao };
  }

  it('4000x3000 com orientação 6 vira 3000x4000', () => {
    expect(dimensoesOrientadas(cabecalho(6))).toEqual({ largura: 3000, altura: 4000 });
  });

  it('orientações 5 a 8 trocam largura por altura', () => {
    for (const orientacao of [5, 7, 8] as const) {
      expect(dimensoesOrientadas(cabecalho(orientacao))).toEqual({ largura: 3000, altura: 4000 });
    }
  });

  it('orientação 1, 3 e nula não trocam', () => {
    expect(dimensoesOrientadas(cabecalho(1))).toEqual({ largura: 4000, altura: 3000 });
    expect(dimensoesOrientadas(cabecalho(3))).toEqual({ largura: 4000, altura: 3000 });
    expect(dimensoesOrientadas(cabecalho(null))).toEqual({ largura: 4000, altura: 3000 });
  });
});
