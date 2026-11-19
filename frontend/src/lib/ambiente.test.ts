import { describe, expect, it } from 'vitest';
import { normalizarUrlApi } from './ambiente';

describe('normalizarUrlApi', () => {
  it('devolve vazio quando a variável falta, como antes', () => {
    expect(normalizarUrlApi(undefined)).toBe('');
    expect(normalizarUrlApi('')).toBe('');
    expect(normalizarUrlApi('   ')).toBe('');
  });

  it('tira as barras do fim para não gerar "//api/..."', () => {
    expect(normalizarUrlApi('https://api.exemplo.com/')).toBe('https://api.exemplo.com');
    expect(normalizarUrlApi('https://api.exemplo.com//')).toBe('https://api.exemplo.com');
  });

  it('mantém a URL quando já está limpa e tira espaços das pontas', () => {
    expect(normalizarUrlApi('https://api.exemplo.com')).toBe('https://api.exemplo.com');
    expect(normalizarUrlApi('  https://api.exemplo.com/  ')).toBe('https://api.exemplo.com');
  });

  it('não mexe em barras do meio nem em caminho', () => {
    expect(normalizarUrlApi('https://exemplo.com/api-base/')).toBe('https://exemplo.com/api-base');
  });
});
