import { describe, expect, it } from 'vitest';
import { podeSimular, temAteDuasCasas, ultimaParcelaDiferente } from './regrasSimulacao';

describe('temAteDuasCasas', () => {
  it.each([0, 1, 0.29, 0.1, 19999.99, 20000.5])('aceita %s', (valor) => {
    expect(temAteDuasCasas(valor)).toBe(true);
  });

  it.each([0.001, 1000.005, 3.333])('recusa %s', (valor) => {
    expect(temAteDuasCasas(valor)).toBe(false);
  });
});

describe('podeSimular', () => {
  it('aceita entrada zero e parcelas de 1 a 60', () => {
    expect(podeSimular({ valorEntrada: 0, numeroParcelas: 1 })).toBe(true);
    expect(podeSimular({ valorEntrada: 0, numeroParcelas: 60 })).toBe(true);
  });

  it.each([0, 61, -1, 2.5, Number.NaN])('recusa %s parcelas', (numeroParcelas) => {
    expect(podeSimular({ valorEntrada: 0, numeroParcelas })).toBe(false);
  });

  it('recusa entrada negativa, com 3 casas ou que não é número', () => {
    expect(podeSimular({ valorEntrada: -0.01, numeroParcelas: 12 })).toBe(false);
    expect(podeSimular({ valorEntrada: 10.005, numeroParcelas: 12 })).toBe(false);
    expect(podeSimular({ valorEntrada: Number.NaN, numeroParcelas: 12 })).toBe(false);
  });

  it('com o valor de venda conhecido, exige entrada menor que ele', () => {
    expect(podeSimular({ valorEntrada: 49999.99, numeroParcelas: 12 }, 50000)).toBe(true);
    expect(podeSimular({ valorEntrada: 50000, numeroParcelas: 12 }, 50000)).toBe(false);
  });

  it('sem parâmetros não simula', () => {
    expect(podeSimular(null)).toBe(false);
  });
});

describe('ultimaParcelaDiferente', () => {
  it('só é verdadeiro quando a última parcela muda', () => {
    expect(ultimaParcelaDiferente(3333.33, 3333.34)).toBe(true);
    expect(ultimaParcelaDiferente(5000, 5000)).toBe(false);
  });
});
