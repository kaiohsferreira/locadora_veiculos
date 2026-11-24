import { describe, expect, it } from 'vitest';
import { CHAVE_PAINEL_OPERACIONAL, chaveFamiliaPainelOperacional, chaveRetiradasDeHoje } from './chavesPainelOperacional';

describe('chaves do painel operacional (lista de retiradas de hoje)', () => {
  it('a raiz é painel-operacional e a lista de hoje fica dentro dela', () => {
    expect(CHAVE_PAINEL_OPERACIONAL).toBe('painel-operacional');
    expect(chaveFamiliaPainelOperacional()).toEqual(['painel-operacional']);
    expect(chaveRetiradasDeHoje()).toEqual(['painel-operacional', 'hoje']);
  });
});
