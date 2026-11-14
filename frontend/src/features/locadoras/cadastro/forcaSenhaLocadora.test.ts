import { describe, expect, it } from 'vitest';
import { avaliarRequisitosSenha, nivelSenha, TOTAL_BARRAS_FORCA_SENHA } from './forcaSenhaLocadora';

describe('avaliarRequisitosSenha: os três requisitos do desenho 37', () => {
  it('senha vazia não atende nenhum requisito', () => {
    expect(avaliarRequisitosSenha('')).toEqual({ tamanho: false, maiuscula: false, numero: false });
  });

  it('senha do desenho atende os três', () => {
    expect(avaliarRequisitosSenha('Rotasul@2025!')).toEqual({ tamanho: true, maiuscula: true, numero: true });
  });

  it('sete caracteres não atendem o tamanho mínimo, mesmo com maiúscula e número', () => {
    expect(avaliarRequisitosSenha('Abcdef1').tamanho).toBe(false);
  });

  it('a maiúscula e o número são checados em qualquer posição', () => {
    expect(avaliarRequisitosSenha('abcdefgH')).toEqual({ tamanho: true, maiuscula: true, numero: false });
    expect(avaliarRequisitosSenha('1abcdefg')).toEqual({ tamanho: true, maiuscula: false, numero: true });
  });
});

describe('nivelSenha: uma barra por requisito atendido', () => {
  it('conta os requisitos atendidos (0 a 3)', () => {
    expect(nivelSenha({ tamanho: false, maiuscula: false, numero: false })).toBe(0);
    expect(nivelSenha({ tamanho: true, maiuscula: false, numero: true })).toBe(2);
    expect(nivelSenha({ tamanho: true, maiuscula: true, numero: true })).toBe(3);
  });

  it('o medidor tem quatro barras, como no desenho', () => {
    expect(TOTAL_BARRAS_FORCA_SENHA).toBe(4);
  });
});
