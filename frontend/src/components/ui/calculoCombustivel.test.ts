import { describe, expect, it } from 'vitest';
import { descreverNivelCombustivel, limitarNivelCombustivel, oitavosDoNivel } from './calculoCombustivel';

describe('oitavosDoNivel', () => {
  it('arredonda para o oitavo mais próximo', () => {
    expect(oitavosDoNivel(0)).toBe(0);
    expect(oitavosDoNivel(0.5)).toBe(4);
    expect(oitavosDoNivel(0.33)).toBe(3);
    expect(oitavosDoNivel(0.99)).toBe(8);
    expect(oitavosDoNivel(1)).toBe(8);
  });
});

describe('limitarNivelCombustivel', () => {
  it('limita ao intervalo de 0 a 1 e trata valor não finito como vazio', () => {
    expect(limitarNivelCombustivel(1.7)).toBe(1);
    expect(limitarNivelCombustivel(-0.3)).toBe(0);
    expect(limitarNivelCombustivel(Number.NaN)).toBe(0);
    expect(limitarNivelCombustivel(Number.POSITIVE_INFINITY)).toBe(0);
    expect(limitarNivelCombustivel(0.4)).toBe(0.4);
  });
});

describe('descreverNivelCombustivel', () => {
  it('pontos conhecidos trazem o nome: 8, 6, 4, 2 e 0 oitavos', () => {
    expect(descreverNivelCombustivel(1)).toEqual({
      oitavos: 8,
      percentual: 100,
      nome: 'Tanque Cheio',
      texto: '8/8 • Tanque Cheio (100%)',
    });
    expect(descreverNivelCombustivel(0.75).texto).toBe('6/8 • 3/4 de Tanque (75%)');
    expect(descreverNivelCombustivel(0.5).texto).toBe('4/8 • 1/2 Tanque (50%)');
    expect(descreverNivelCombustivel(0.25).texto).toBe('2/8 • 1/4 de Tanque (25%)');
    expect(descreverNivelCombustivel(0).texto).toBe('0/8 • Tanque Vazio (0%)');
  });

  it('oitavos sem nome mostram só a fração e o percentual', () => {
    const descricao = descreverNivelCombustivel(0.33);
    expect(descricao.oitavos).toBe(3);
    expect(descricao.nome).toBeUndefined();
    expect(descricao.texto).toBe('3/8 (33%)');
  });

  it('o percentual vem do valor exato, mesmo quando o oitavo é outro', () => {
    expect(descreverNivelCombustivel(0.99)).toMatchObject({ oitavos: 8, percentual: 99, texto: '8/8 • Tanque Cheio (99%)' });
  });

  it('aceita o separador da tela 06', () => {
    expect(descreverNivelCombustivel(1, ' - ').texto).toBe('8/8 - Tanque Cheio (100%)');
  });

  it('valor fora da faixa é limitado antes de descrever', () => {
    expect(descreverNivelCombustivel(2)).toMatchObject({ oitavos: 8, percentual: 100 });
    expect(descreverNivelCombustivel(-1)).toMatchObject({ oitavos: 0, percentual: 0 });
  });
});
