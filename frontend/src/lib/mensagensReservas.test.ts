import { describe, expect, it } from 'vitest';
import { mensagemReservasAnoModeloCor } from './mensagensReservas';

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
