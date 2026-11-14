import { describe, expect, it } from 'vitest';
import { montarSaudacao, primeiroNome, saudacaoPorHora } from './saudacao';

describe('saudacaoPorHora', () => {
  it('manhã de 5h às 11h59', () => {
    expect(saudacaoPorHora(5)).toBe('Bom dia');
    expect(saudacaoPorHora(11)).toBe('Bom dia');
  });

  it('tarde de 12h às 17h59', () => {
    expect(saudacaoPorHora(12)).toBe('Boa tarde');
    expect(saudacaoPorHora(17)).toBe('Boa tarde');
  });

  it('noite a partir de 18h e até as 4h59', () => {
    expect(saudacaoPorHora(18)).toBe('Boa noite');
    expect(saudacaoPorHora(23)).toBe('Boa noite');
    expect(saudacaoPorHora(0)).toBe('Boa noite');
    expect(saudacaoPorHora(4)).toBe('Boa noite');
  });
});

describe('primeiroNome', () => {
  it('devolve a primeira palavra do nome', () => {
    expect(primeiroNome('Marina Costa')).toBe('Marina');
  });

  it('ignora espaços em volta e entre as palavras', () => {
    expect(primeiroNome('   Rafael    Toledo  ')).toBe('Rafael');
  });

  it('ausente ou só com espaços devolve undefined', () => {
    expect(primeiroNome(undefined)).toBeUndefined();
    expect(primeiroNome('   ')).toBeUndefined();
  });
});

describe('montarSaudacao', () => {
  it('junta a saudação da hora com o primeiro nome', () => {
    expect(montarSaudacao(9, 'Marina Costa')).toBe('Bom dia, Marina');
    expect(montarSaudacao(14, '  Rafael Toledo ')).toBe('Boa tarde, Rafael');
  });

  it('sem nome, fica só a saudação', () => {
    expect(montarSaudacao(20, undefined)).toBe('Boa noite');
    expect(montarSaudacao(8, '')).toBe('Bom dia');
  });
});
