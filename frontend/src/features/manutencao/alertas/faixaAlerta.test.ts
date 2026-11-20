import { describe, expect, it } from 'vitest';
import type { AlertaRevisaoRetorno } from '@/types/manutencao';
import {
  FAIXA_ALERTA_LARANJA_KM,
  FAIXA_ALERTA_VERMELHA_KM,
  faixaDoAlerta,
  larguraBarraCiclo,
  seloDoAlerta,
  textoCicloDoAlerta,
  textoFaltamDoAlerta,
} from './faixaAlerta';

function alerta(parcial: Partial<AlertaRevisaoRetorno>): AlertaRevisaoRetorno {
  return {
    idVeiculo: 1,
    placa: 'BRA2E19',
    marca: 'Chevrolet',
    modelo: 'Onix Plus LTZ',
    categoriaNome: 'Grupo B',
    kmAtual: 19820,
    kmUltimaPreventiva: 10000,
    kmProximaRevisao: 20000,
    faltamKm: 180,
    percentualCiclo: 98.2,
    situacao: 'PROXIMA',
    fotoCapa: null,
    erroFotoCapa: null,
    ...parcial,
  };
}

describe('constantes das faixas (números aprovados pelo Kaio em 20/11/2026)', () => {
  it('vermelha até 500 km e laranja até 1.200 km', () => {
    expect(FAIXA_ALERTA_VERMELHA_KM).toBe(500);
    expect(FAIXA_ALERTA_LARANJA_KM).toBe(1200);
  });
});

describe('faixaDoAlerta: vermelha', () => {
  it('revisão vencida é sempre vermelha, mesmo longe do limite', () => {
    expect(faixaDoAlerta(alerta({ situacao: 'VENCIDA', faltamKm: -300 }))).toBe('VERMELHA');
  });

  it('faltar zero km (limite exato da revisão) é vermelho', () => {
    expect(faixaDoAlerta(alerta({ situacao: 'VENCIDA', faltamKm: 0 }))).toBe('VERMELHA');
  });

  it('exatamente 500 km é vermelho (limite inclusivo)', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 500 }))).toBe('VERMELHA');
  });

  it('1 km é vermelho', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 1 }))).toBe('VERMELHA');
  });
});

describe('faixaDoAlerta: laranja', () => {
  it('501 km é laranja (logo acima do vermelho)', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 501 }))).toBe('LARANJA');
  });

  it('exatamente 1.200 km é laranja (limite inclusivo)', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 1200 }))).toBe('LARANJA');
  });

  it('1.060 km (cartão do desenho) é laranja', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 1060 }))).toBe('LARANJA');
  });
});

describe('faixaDoAlerta: azul-cinza (neutra)', () => {
  it('1.201 km é neutra (logo acima do laranja)', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 1201 }))).toBe('NEUTRA');
  });

  it('1.500 km (margem da API para PROXIMA) é neutra', () => {
    expect(faixaDoAlerta(alerta({ faltamKm: 1500 }))).toBe('NEUTRA');
  });
});

describe('textoFaltamDoAlerta', () => {
  it('vermelha com revisão ainda não vencida: "Faltam apenas X km!"', () => {
    expect(textoFaltamDoAlerta(alerta({ faltamKm: 180 }))).toBe('Faltam apenas 180 km!');
  });

  it('laranja: "Faltam X km" com separador de milhar', () => {
    expect(textoFaltamDoAlerta(alerta({ faltamKm: 1060 }))).toBe('Faltam 1.060 km');
  });

  it('neutra: "Faltam X km"', () => {
    expect(textoFaltamDoAlerta(alerta({ faltamKm: 1500 }))).toBe('Faltam 1.500 km');
  });

  it('revisão vencida: "Passou X km do limite" com o valor sem sinal', () => {
    expect(textoFaltamDoAlerta(alerta({ situacao: 'VENCIDA', faltamKm: -320 }))).toBe('Passou 320 km do limite');
  });
});

describe('textoCicloDoAlerta', () => {
  it('vermelha: "% do ciclo limite", arredondado', () => {
    expect(textoCicloDoAlerta(alerta({ faltamKm: 180, percentualCiclo: 98.2 }))).toBe('98% do ciclo limite');
  });

  it('laranja e neutra: "% do ciclo atingido"', () => {
    expect(textoCicloDoAlerta(alerta({ faltamKm: 1060, percentualCiclo: 89.4 }))).toBe('89% do ciclo atingido');
    expect(textoCicloDoAlerta(alerta({ faltamKm: 1500, percentualCiclo: 85 }))).toBe('85% do ciclo atingido');
  });

  it('percentual acima de 100 aparece como está, arredondado', () => {
    expect(textoCicloDoAlerta(alerta({ situacao: 'VENCIDA', faltamKm: -320, percentualCiclo: 103.2 }))).toBe(
      '103% do ciclo limite',
    );
  });
});

describe('larguraBarraCiclo', () => {
  it('segue o percentual', () => {
    expect(larguraBarraCiclo(89.4)).toBe(89.4);
  });

  it('trava em 100 quando a revisão já passou', () => {
    expect(larguraBarraCiclo(103.2)).toBe(100);
  });

  it('não fica abaixo de zero', () => {
    expect(larguraBarraCiclo(-4)).toBe(0);
  });
});

describe('seloDoAlerta', () => {
  it('vermelha não vencida: "Revisão Iminente"', () => {
    expect(seloDoAlerta(alerta({ faltamKm: 180 }))).toBe('Revisão Iminente');
  });

  it('vermelha vencida: "Revisão vencida"', () => {
    expect(seloDoAlerta(alerta({ situacao: 'VENCIDA', faltamKm: -320 }))).toBe('Revisão vencida');
  });

  it('laranja e neutra não têm selo', () => {
    expect(seloDoAlerta(alerta({ faltamKm: 1060 }))).toBeNull();
    expect(seloDoAlerta(alerta({ faltamKm: 1500 }))).toBeNull();
  });
});
