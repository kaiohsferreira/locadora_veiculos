import { describe, expect, it } from 'vitest';
import { formatarPercentual, formatarVariacaoPercentual, pluralizar, sentidoVariacao } from './formatadoresPainel';

describe('sentidoVariacao', () => {
  it('alta, queda e neutra pelo sinal do valor', () => {
    expect(sentidoVariacao(12)).toBe('alta');
    expect(sentidoVariacao(-3.5)).toBe('queda');
    expect(sentidoVariacao(0)).toBe('neutra');
  });

  it('valor que arredonda para zero é neutro, em qualquer sinal', () => {
    expect(sentidoVariacao(0.04)).toBe('neutra');
    expect(sentidoVariacao(-0.04)).toBe('neutra');
  });

  it('a menor variação que aparece já tem sentido (0,1 e -0,1)', () => {
    expect(sentidoVariacao(0.1)).toBe('alta');
    expect(sentidoVariacao(-0.1)).toBe('queda');
  });

  it('nulo ou não numérico devolve null', () => {
    expect(sentidoVariacao(null)).toBeNull();
    expect(sentidoVariacao(undefined)).toBeNull();
    expect(sentidoVariacao(Number.NaN)).toBeNull();
  });
});

describe('formatarPercentual', () => {
  it('arredonda sem casas decimais por padrão', () => {
    expect(formatarPercentual(68)).toBe('68%');
    expect(formatarPercentual(63.6)).toBe('64%');
  });

  it('usa a quantidade de casas decimais pedida, com vírgula', () => {
    expect(formatarPercentual(63.6, 1)).toBe('63,6%');
  });

  it('nulo ou não numérico vira o valor ausente', () => {
    expect(formatarPercentual(null)).toBe('—');
    expect(formatarPercentual(undefined)).toBe('—');
    expect(formatarPercentual(Number.NaN)).toBe('—');
  });

  it('zero aparece como zero, não como ausente', () => {
    expect(formatarPercentual(0)).toBe('0%');
  });
});

describe('formatarVariacaoPercentual', () => {
  it('põe o sinal de mais em alta', () => {
    expect(formatarVariacaoPercentual(12)).toBe('+12%');
  });

  it('põe o sinal de menos em queda', () => {
    expect(formatarVariacaoPercentual(-3)).toBe('-3%');
  });

  it('zero não recebe sinal', () => {
    expect(formatarVariacaoPercentual(0)).toBe('0%');
  });

  it('mostra uma casa decimal em pt-BR nos três sinais', () => {
    expect(formatarVariacaoPercentual(0.3)).toBe('+0,3%');
    expect(formatarVariacaoPercentual(-3.5)).toBe('-3,5%');
    expect(formatarVariacaoPercentual(12.25)).toBe('+12,3%');
  });

  it('valor que arredonda para zero não mostra "+0%" nem "-0%"', () => {
    expect(formatarVariacaoPercentual(0.04)).toBe('0%');
    expect(formatarVariacaoPercentual(-0.04)).toBe('0%');
    expect(formatarVariacaoPercentual(-0)).toBe('0%');
  });

  it('o arredondamento da borda sobe para o próximo décimo e sai com o sinal certo', () => {
    expect(formatarVariacaoPercentual(0.05)).toBe('+0,1%');
    expect(formatarVariacaoPercentual(-0.05)).toBe('-0,1%');
    expect(formatarVariacaoPercentual(99.96)).toBe('+100%');
  });

  it('nulo devolve null: a linha de variação não aparece', () => {
    expect(formatarVariacaoPercentual(null)).toBeNull();
    expect(formatarVariacaoPercentual(undefined)).toBeNull();
  });

  it('não numérico devolve null', () => {
    expect(formatarVariacaoPercentual(Number.POSITIVE_INFINITY)).toBeNull();
  });
});

describe('pluralizar', () => {
  it('usa o singular só para exatamente um', () => {
    expect(pluralizar(1, 'cobrança', 'cobranças')).toBe('1 cobrança');
    expect(pluralizar(14, 'cobrança', 'cobranças')).toBe('14 cobranças');
  });

  it('zero usa o plural', () => {
    expect(pluralizar(0, 'veículo', 'veículos')).toBe('0 veículos');
  });

  it('separa milhar no número', () => {
    expect(pluralizar(1234, 'veículo', 'veículos')).toBe('1.234 veículos');
  });
});
