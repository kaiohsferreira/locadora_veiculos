import { describe, expect, it, vi } from 'vitest';
import {
  MENSAGEM_FOTOS_ERRO_DIMENSAO,
  MENSAGEM_FOTOS_ERRO_TAMANHO,
  MENSAGEM_FOTOS_ERRO_TIPO,
} from './mensagensFotosVeiculo';
import { analisarArquivosFoto, lerDimensoesFoto } from './arquivosFotoVeiculo';
import { TAMANHO_MAXIMO_FOTO_BYTES } from './fotosVeiculo';

function arquivoComTipo(nome: string, tipo: string, tamanho = 100): File {
  const arquivo = new File(['x'], nome, { type: tipo });
  Object.defineProperty(arquivo, 'size', { value: tamanho });
  return arquivo;
}

/** Imagem falsa: dispara onload ou onerror quando o teste manda. */
function imagemFalsa(largura: number, altura: number) {
  const imagem = {
    naturalWidth: largura,
    naturalHeight: altura,
    src: '',
    onload: null as (() => void) | null,
    onerror: null as (() => void) | null,
  };
  return imagem;
}

describe('lerDimensoesFoto', () => {
  it('lê largura e altura quando a imagem carrega', async () => {
    const imagem = imagemFalsa(1600, 1200);
    const leitura = lerDimensoesFoto('blob:foto', () => imagem as unknown as HTMLImageElement);

    imagem.onload?.();

    expect(imagem.src).toBe('blob:foto');
    await expect(leitura).resolves.toEqual({ largura: 1600, altura: 1200 });
  });

  it('devolve nulo quando o navegador não consegue decodificar a imagem', async () => {
    const imagem = imagemFalsa(0, 0);
    const leitura = lerDimensoesFoto('blob:ruim', () => imagem as unknown as HTMLImageElement);

    imagem.onerror?.();

    await expect(leitura).resolves.toBeNull();
  });
});

describe('analisarArquivosFoto', () => {
  function dependencias(dimensoes: Array<{ largura: number; altura: number } | null>) {
    const fila = [...dimensoes];
    return {
      criarPrevia: vi.fn((arquivo: File) => `blob:${arquivo.name}`),
      liberarPrevia: vi.fn<(previa: string) => void>(),
      lerDimensoes: vi.fn(async () => fila.shift() ?? null),
    };
  }

  it('segue para a fila o que passa tipo, tamanho e dimensão, na ordem em que foi escolhido', async () => {
    const deps = dependencias([{ largura: 1600, altura: 1200 }, { largura: 800, altura: 600 }]);

    const resultado = await analisarArquivosFoto(
      [arquivoComTipo('a.jpg', 'image/jpeg'), arquivoComTipo('b.png', 'image/png')],
      deps,
    );

    expect(resultado.validos.map((v) => v.arquivo.name)).toEqual(['a.jpg', 'b.png']);
    expect(resultado.validos[0].previa).toBe('blob:a.jpg');
    expect(resultado.recusas).toEqual([]);
    expect(deps.liberarPrevia).not.toHaveBeenCalled();
  });

  it('recusa por tipo antes de criar prévia ou ler dimensão', async () => {
    const deps = dependencias([]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('planilha.pdf', 'application/pdf')], deps);

    expect(resultado.validos).toEqual([]);
    expect(resultado.recusas).toEqual([{ nome: 'planilha.pdf', mensagem: MENSAGEM_FOTOS_ERRO_TIPO }]);
    expect(deps.criarPrevia).not.toHaveBeenCalled();
    expect(deps.lerDimensoes).not.toHaveBeenCalled();
  });

  it('recusa por tamanho acima de 10 MiB, sem ler a imagem', async () => {
    const deps = dependencias([]);

    const resultado = await analisarArquivosFoto(
      [arquivoComTipo('grande.png', 'image/png', TAMANHO_MAXIMO_FOTO_BYTES + 1)],
      deps,
    );

    expect(resultado.recusas).toEqual([{ nome: 'grande.png', mensagem: MENSAGEM_FOTOS_ERRO_TAMANHO }]);
    expect(deps.lerDimensoes).not.toHaveBeenCalled();
  });

  it('recusa por dimensão e libera a prévia criada', async () => {
    const deps = dependencias([{ largura: 9000, altura: 100 }]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('larga.png', 'image/png')], deps);

    expect(resultado.validos).toEqual([]);
    expect(resultado.recusas).toEqual([{ nome: 'larga.png', mensagem: MENSAGEM_FOTOS_ERRO_DIMENSAO }]);
    expect(deps.liberarPrevia).toHaveBeenCalledWith('blob:larga.png');
  });

  it('imagem que o navegador não consegue ler segue para o servidor, que decide', async () => {
    const deps = dependencias([null]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('estranha.jpg', 'image/jpeg')], deps);

    expect(resultado.validos.map((v) => v.arquivo.name)).toEqual(['estranha.jpg']);
    expect(resultado.recusas).toEqual([]);
  });
});
