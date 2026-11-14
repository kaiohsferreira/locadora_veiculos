import { describe, expect, it } from 'vitest';
import { escolherVariante } from './variante';

describe('escolherVariante', () => {
  it('primeiro acesso verdadeiro: tela 40', () => {
    expect(escolherVariante({ primeiroAcesso: true })).toBe('primeiro-acesso');
  });

  it('primeiro acesso falso: tela 39', () => {
    expect(escolherVariante({ primeiroAcesso: false })).toBe('visao-geral');
  });
});
