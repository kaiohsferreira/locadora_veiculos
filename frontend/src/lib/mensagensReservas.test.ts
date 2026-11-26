import { describe, expect, it } from 'vitest';
import { mensagemReservasAnoModeloCor, mensagemReservasLocalOuDiarias, mensagemReservasPlaca } from './mensagensReservas';

describe('mensagemReservasLocalOuDiarias (última linha do cartão do histórico no celular)', () => {
  it('com cidade e placa: "cidade • Placa XXX"', () => {
    expect(mensagemReservasLocalOuDiarias('Rio de Janeiro - RJ', 'GHI4J56', 3)).toBe(
      'Rio de Janeiro - RJ • Placa GHI4J56',
    );
  });

  it('sem placa: "cidade • N diárias" (a cidade fica)', () => {
    expect(mensagemReservasLocalOuDiarias('Curitiba - PR', '', 3)).toBe('Curitiba - PR • 3 diárias');
    expect(mensagemReservasLocalOuDiarias('Curitiba - PR', '   ', 1)).toBe('Curitiba - PR • 1 diária');
  });

  it('sem cidade, com placa: só "Placa XXX" (sem diárias)', () => {
    expect(mensagemReservasLocalOuDiarias('', 'GHI4J56', 4)).toBe('Placa GHI4J56');
    expect(mensagemReservasLocalOuDiarias('  ', 'GHI4J56', 4)).toBe('Placa GHI4J56');
  });

  it('sem cidade e sem placa: só "N diárias"', () => {
    expect(mensagemReservasLocalOuDiarias('', '', 2)).toBe('2 diárias');
    expect(mensagemReservasLocalOuDiarias('', '   ', 1)).toBe('1 diária');
  });
});

describe('mensagemReservasPlaca', () => {
  it('põe o prefixo "Placa " antes da placa já formatada', () => {
    expect(mensagemReservasPlaca('BRA2E19')).toBe('Placa BRA2E19');
  });
});

describe('mensagemReservasAnoModeloCor', () => {
  it('junta o ano do modelo e a cor com o separador', () => {
    expect(mensagemReservasAnoModeloCor(2024, 'Prata')).toBe('Ano do modelo 2024 · Prata');
  });

  it('sem cor (nula ou vazia) devolve só o ano', () => {
    expect(mensagemReservasAnoModeloCor(2024, null)).toBe('Ano do modelo 2024');
    expect(mensagemReservasAnoModeloCor(2024, '')).toBe('Ano do modelo 2024');
  });

  it('cor só com espaços conta como vazia', () => {
    expect(mensagemReservasAnoModeloCor(2024, '   ')).toBe('Ano do modelo 2024');
  });
});
