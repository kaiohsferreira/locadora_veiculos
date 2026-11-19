import { describe, expect, it, vi } from 'vitest';
import type { CabecalhoFoto } from './cabecalhoFoto';
import {
  MENSAGEM_FOTOS_ERRO_DIMENSAO,
  MENSAGEM_FOTOS_ERRO_TAMANHO,
  MENSAGEM_FOTOS_ERRO_TIPO,
} from './mensagensFotosVeiculo';
import { analisarArquivosFoto } from './arquivosFotoVeiculo';
import { TAMANHO_MAXIMO_FOTO_BYTES } from './fotosVeiculo';

function arquivoComTipo(nome: string, tipo: string, tamanho = 100): File {
  const arquivo = new File(['x'], nome, { type: tipo });
  Object.defineProperty(arquivo, 'size', { value: tamanho });
  return arquivo;
}

function cabecalho(largura: number, altura: number): CabecalhoFoto {
  return { formato: 'jpeg', largura, altura, orientacao: 1 };
}

/** Dependências com o cabeçalho de cada arquivo, na ordem em que são lidos (null = ilegível). */
function dependencias(cabecalhos: Array<CabecalhoFoto | null>) {
  const fila = [...cabecalhos];
  return {
    criarPrevia: vi.fn((arquivo: File) => `blob:${arquivo.name}`),
    lerCabecalho: vi.fn(async () => fila.shift() ?? null),
  };
}

describe('analisarArquivosFoto', () => {
  it('segue para a fila o que passa tipo, tamanho e dimensão, na ordem em que foi escolhido', async () => {
    const deps = dependencias([cabecalho(1600, 1200), cabecalho(800, 600)]);

    const resultado = await analisarArquivosFoto(
      [arquivoComTipo('a.jpg', 'image/jpeg'), arquivoComTipo('b.png', 'image/png')],
      deps,
    );

    expect(resultado.validos.map((v) => v.arquivo.name)).toEqual(['a.jpg', 'b.png']);
    expect(resultado.validos[0].previa).toBe('blob:a.jpg');
    expect(resultado.validos[0].cabecalho).toEqual(cabecalho(1600, 1200));
    expect(resultado.recusas).toEqual([]);
  });

  it('recusa por tipo antes de criar prévia ou ler o cabeçalho', async () => {
    const deps = dependencias([]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('planilha.pdf', 'application/pdf')], deps);

    expect(resultado.validos).toEqual([]);
    expect(resultado.recusas).toEqual([{ nome: 'planilha.pdf', mensagem: MENSAGEM_FOTOS_ERRO_TIPO }]);
    expect(deps.criarPrevia).not.toHaveBeenCalled();
    expect(deps.lerCabecalho).not.toHaveBeenCalled();
  });

  it('recusa por tamanho acima de 10 MiB, sem ler o cabeçalho', async () => {
    const deps = dependencias([]);

    const resultado = await analisarArquivosFoto(
      [arquivoComTipo('grande.png', 'image/png', TAMANHO_MAXIMO_FOTO_BYTES + 1)],
      deps,
    );

    expect(resultado.recusas).toEqual([{ nome: 'grande.png', mensagem: MENSAGEM_FOTOS_ERRO_TAMANHO }]);
    expect(deps.lerCabecalho).not.toHaveBeenCalled();
  });

  it('recusa por dimensão lida do cabeçalho e não cria prévia para o arquivo recusado', async () => {
    const deps = dependencias([cabecalho(9000, 100)]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('larga.png', 'image/png')], deps);

    expect(resultado.validos).toEqual([]);
    expect(resultado.recusas).toEqual([{ nome: 'larga.png', mensagem: MENSAGEM_FOTOS_ERRO_DIMENSAO }]);
    expect(deps.criarPrevia).not.toHaveBeenCalled();
  });

  it('recusa lado de 8193 px lido do cabeçalho', async () => {
    const deps = dependencias([cabecalho(8193, 100)]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('limite.jpg', 'image/jpeg')], deps);

    expect(resultado.recusas).toEqual([{ nome: 'limite.jpg', mensagem: MENSAGEM_FOTOS_ERRO_DIMENSAO }]);
    expect(deps.criarPrevia).not.toHaveBeenCalled();
  });

  it('recusa mais de 16.777.216 pixels mesmo com lados dentro de 8192 px', async () => {
    const deps = dependencias([cabecalho(4097, 4097)]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('muitos.jpg', 'image/jpeg')], deps);

    expect(resultado.recusas).toEqual([{ nome: 'muitos.jpg', mensagem: MENSAGEM_FOTOS_ERRO_DIMENSAO }]);
  });

  it('aceita 8192 px de lado (o limite não muda)', async () => {
    const deps = dependencias([cabecalho(8192, 2048)]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('no-limite.jpg', 'image/jpeg')], deps);

    expect(resultado.validos.map((v) => v.arquivo.name)).toEqual(['no-limite.jpg']);
    expect(resultado.recusas).toEqual([]);
  });

  it('imagem cujo cabeçalho não pode ser lido segue para o servidor, que decide', async () => {
    const deps = dependencias([null]);

    const resultado = await analisarArquivosFoto([arquivoComTipo('estranha.jpg', 'image/jpeg')], deps);

    expect(resultado.validos.map((v) => v.arquivo.name)).toEqual(['estranha.jpg']);
    expect(resultado.validos[0].cabecalho).toBeNull();
    expect(resultado.recusas).toEqual([]);
  });
});
