import { describe, expect, it } from 'vitest';
import {
  entradaDoPercentual,
  entradaMaximaDaBarra,
  percentualDaEntrada,
  podeSimular,
  simulacaoEhDosValores,
  temAteDuasCasas,
  ultimaParcelaDiferente,
} from './regrasSimulacao';

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

describe('atalhos e barra da entrada', () => {
  it('entrada do percentual arredonda ao centavo', () => {
    expect(entradaDoPercentual(104500, 30)).toBe(31350);
    expect(entradaDoPercentual(99999.99, 10)).toBe(10000);
  });

  it('o máximo da barra fica abaixo do valor de venda, no passo de R$ 100', () => {
    expect(entradaMaximaDaBarra(104500)).toBe(104400);
    expect(entradaMaximaDaBarra(104450.5)).toBe(104400);
    expect(entradaMaximaDaBarra(50)).toBe(0);
  });

  it('percentual inteiro da entrada', () => {
    expect(percentualDaEntrada(31350, 104500)).toBe(30);
    expect(percentualDaEntrada(0, 0)).toBe(0);
  });
});

describe('simulacaoEhDosValores', () => {
  const simulacao = { idVeiculo: 4, valorEntrada: 1000.1, numeroParcelas: 12 };

  it('só é verdadeiro quando carro, entrada e parcelas são os dos campos', () => {
    expect(simulacaoEhDosValores(simulacao, 4, 1000.1, 12)).toBe(true);
    expect(simulacaoEhDosValores(simulacao, 4, 1000.2, 12)).toBe(false);
    expect(simulacaoEhDosValores(simulacao, 4, 1000.1, 24)).toBe(false);
    expect(simulacaoEhDosValores(simulacao, 5, 1000.1, 12)).toBe(false);
  });

  it('sem simulação ou com campo nulo não é', () => {
    expect(simulacaoEhDosValores(undefined, 4, 0, 12)).toBe(false);
    expect(simulacaoEhDosValores(simulacao, 4, null, 12)).toBe(false);
  });
});
