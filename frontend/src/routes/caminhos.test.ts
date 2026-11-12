import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CAMINHOS, obterCaminhoRetorno } from './caminhos';

const ORIGEM = 'https://autovia.test';
const PADRAO = '/padrao-de-teste';

beforeEach(() => {
  vi.stubGlobal('window', { location: { origin: ORIGEM } });
});

afterEach(() => {
  vi.unstubAllGlobals();
});

/** Estado de redirecionamento com o caminho informado. */
function estadoCom(de: string): { de: string } {
  return { de };
}

describe('obterCaminhoRetorno: caminhos aceitos', () => {
  it('aceita caminho interno simples', () => {
    expect(obterCaminhoRetorno(estadoCom('/minhas-reservas'), PADRAO)).toBe('/minhas-reservas');
  });

  it('aceita caminho com percent-encoding que continua na mesma origem', () => {
    expect(obterCaminhoRetorno(estadoCom('/%2F%2Fsite'), PADRAO)).toBe('/%2F%2Fsite');
  });

  it('aceita caminho com query que contém "//" depois do "?"', () => {
    expect(obterCaminhoRetorno(estadoCom('/veiculos?x=//site'), PADRAO)).toBe('/veiculos?x=//site');
  });
});

describe('obterCaminhoRetorno: caminhos rejeitados (devolvem o padrão)', () => {
  it('rejeita tabulação no meio do caminho', () => {
    expect(obterCaminhoRetorno(estadoCom('/\t/site'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita quebra de linha no meio do caminho', () => {
    expect(obterCaminhoRetorno(estadoCom('/\n/site'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita espaço', () => {
    expect(obterCaminhoRetorno(estadoCom('/ veiculos'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita caractere DEL (0x7F)', () => {
    expect(obterCaminhoRetorno(estadoCom('/veiculos\u007f'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita "//site" (URL de protocolo relativo)', () => {
    expect(obterCaminhoRetorno(estadoCom('//site'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita "/\\site" (barra invertida)', () => {
    expect(obterCaminhoRetorno(estadoCom('/\\site'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita URL absoluta "https://site"', () => {
    expect(obterCaminhoRetorno(estadoCom('https://site'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita "javascript:alert(1)"', () => {
    expect(obterCaminhoRetorno(estadoCom('javascript:alert(1)'), PADRAO)).toBe(PADRAO);
  });

  it('rejeita a própria tela de login, para não voltar a ela depois de entrar', () => {
    expect(obterCaminhoRetorno(estadoCom(CAMINHOS.entrar), PADRAO)).toBe(PADRAO);
    expect(obterCaminhoRetorno(estadoCom('/entrar?motivo=sessao'), PADRAO)).toBe(PADRAO);
  });

  it('não confunde outro caminho que só começa com "/entrar"', () => {
    expect(obterCaminhoRetorno(estadoCom('/entrarx'), PADRAO)).toBe('/entrarx');
  });
});

describe('obterCaminhoRetorno: estado sem retorno', () => {
  it('estado nulo devolve o padrão', () => {
    expect(obterCaminhoRetorno(null, PADRAO)).toBe(PADRAO);
  });

  it('estado sem o campo de retorno devolve o padrão', () => {
    expect(obterCaminhoRetorno({ outro: '/x' }, PADRAO)).toBe(PADRAO);
  });

  it('retorno que não é texto devolve o padrão', () => {
    expect(obterCaminhoRetorno({ de: 42 }, PADRAO)).toBe(PADRAO);
  });

  it('sem padrão informado, usa o início', () => {
    expect(obterCaminhoRetorno(undefined)).toBe(CAMINHOS.inicio);
  });
});
