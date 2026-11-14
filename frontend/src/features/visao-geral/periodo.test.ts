import { describe, expect, it } from 'vitest';
import { PERIODO_PADRAO_VISAO_GERAL, PERIODOS_VISAO_GERAL } from '@/types/painel';
import { OPCOES_PERIODO, rotuloPeriodo } from './periodo';

describe('OPCOES_PERIODO', () => {
  it('são os quatro períodos do contrato, na ordem do desenho', () => {
    expect(OPCOES_PERIODO).toEqual([
      { valor: 'HOJE', rotulo: 'Hoje' },
      { valor: 'SETE_DIAS', rotulo: '7 dias' },
      { valor: 'TRINTA_DIAS', rotulo: '30 dias' },
      { valor: 'MES_ATUAL', rotulo: 'Este mês' },
    ]);
  });

  it('cobre exatamente os valores do enum do contrato', () => {
    expect(OPCOES_PERIODO.map((opcao) => opcao.valor)).toEqual([...PERIODOS_VISAO_GERAL]);
  });
});

describe('rotuloPeriodo', () => {
  it('converte o valor do contrato no rótulo visível', () => {
    expect(rotuloPeriodo('HOJE')).toBe('Hoje');
    expect(rotuloPeriodo('MES_ATUAL')).toBe('Este mês');
  });
});

describe('período padrão', () => {
  it('é 30 dias, como no contrato, e está entre as opções', () => {
    expect(PERIODO_PADRAO_VISAO_GERAL).toBe('TRINTA_DIAS');
    expect(OPCOES_PERIODO.map((opcao) => opcao.valor)).toContain(PERIODO_PADRAO_VISAO_GERAL);
  });
});
