import { describe, expect, it } from 'vitest';
import {
  CHAVE_PAINEL_FINANCEIRO,
  chaveFamiliaPainelFinanceiro,
  chaveIndicadoresFinanceiros,
  chaveTitulosFinanceiros,
} from './chavesPainelFinanceiro';

describe('chaves do painel financeiro', () => {
  it('a família é a raiz que a baixa invalida (indicadores e tabela)', () => {
    expect(CHAVE_PAINEL_FINANCEIRO).toBe('painel-financeiro');
    expect(chaveFamiliaPainelFinanceiro()).toEqual(['painel-financeiro']);
    expect(chaveIndicadoresFinanceiros()[0]).toBe(CHAVE_PAINEL_FINANCEIRO);
    expect(chaveTitulosFinanceiros({ aba: 'TODOS', pagina: 1, tamanhoPagina: 20 })[0]).toBe(CHAVE_PAINEL_FINANCEIRO);
  });

  it('a tabela leva o filtro inteiro, para cada aba e página ter a sua chave', () => {
    const filtro = { aba: 'EM_ATRASO' as const, pagina: 1, tamanhoPagina: 20 };

    expect(chaveTitulosFinanceiros(filtro)).toEqual(['painel-financeiro', 'titulos', filtro]);
    expect(chaveTitulosFinanceiros({ ...filtro, pagina: 2 })).not.toEqual(chaveTitulosFinanceiros(filtro));
    expect(chaveTitulosFinanceiros({ ...filtro, aba: 'TODOS' })).not.toEqual(chaveTitulosFinanceiros(filtro));
  });
});
