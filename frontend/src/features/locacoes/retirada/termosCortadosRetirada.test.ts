import { describe, expect, it } from 'vitest';
import { termosCortadosPresentesRetirada, TERMOS_CORTADOS_RETIRADA } from './termosCortadosRetirada';

describe('termos cortados da tela 16', () => {
  it('acha um termo cortado no texto visível', () => {
    expect(termosCortadosPresentesRetirada('<p>Cliente Gold</p>')).toEqual(['Cliente Gold']);
  });
  it('não conta o termo que aparece só em atributo class ou src', () => {
    expect(termosCortadosPresentesRetirada('<div class="Cliente Gold">Retirada</div><img src="Caução.png"/>')).toEqual([]);
  });
  it('compara sem caixa e sem acento', () => {
    expect(termosCortadosPresentesRetirada('<span>balcao 04</span>')).toEqual(['Balcão 04']);
    expect(termosCortadosPresentesRetirada('<span>BALCÃO 04</span>')).toEqual(['Balcão 04']);
  });
  it('inclui os termos do desenho que a tela não pode mostrar', () => {
    for (const termo of ['Cliente Gold', 'Box Totem', 'Caução', 'Serpro', 'Score de 98%', 'Salvar Rascunho', 'c/ taxas']) {
      expect(TERMOS_CORTADOS_RETIRADA).toContain(termo);
    }
  });
});
