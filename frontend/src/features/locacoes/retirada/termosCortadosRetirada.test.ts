import { describe, expect, it } from 'vitest';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import {
  termosCortadosPresentesRetirada,
  termosDaCaucaoPresentesRetirada,
  TERMOS_CORTADOS_RETIRADA,
  TERMOS_DA_CAUCAO_RETIRADA,
} from './termosCortadosRetirada';

describe('termos cortados da tela 16', () => {
  it('acha um termo cortado no texto visível', () => {
    expect(termosCortadosPresentesRetirada('<p>Cliente Gold</p>')).toEqual(['Cliente Gold']);
  });
  it('não conta o termo que aparece só em atributo class ou src', () => {
    expect(termosCortadosPresentesRetirada('<div class="Cliente Gold">Retirada</div><img src="Mastercard.png"/>')).toEqual([]);
    expect(termosDaCaucaoPresentesRetirada('<div class="Garantia">Retirada</div><img src="Caução.png"/>')).toEqual([]);
  });
  it('compara sem caixa e sem acento', () => {
    expect(termosCortadosPresentesRetirada('<span>balcao 04</span>')).toEqual(['Balcão 04']);
    expect(termosCortadosPresentesRetirada('<span>BALCÃO 04</span>')).toEqual(['Balcão 04']);
    expect(termosDaCaucaoPresentesRetirada('<span>CAUCAO</span>')).toEqual(['Caução']);
  });
  it('acha o termo no começo de palavra, inclusive no plural', () => {
    expect(termosCortadosPresentesRetirada('<span>NSU #849103 - Aprovado</span>')).toEqual(['NSU']);
    expect(termosCortadosPresentesRetirada('<span>Categoria: B (Ativa)</span>')).toEqual(['(Ativa)']);
    expect(termosDaCaucaoPresentesRetirada('<p>documentação, garantias e validação</p>')).toEqual(['Garantia']);
  });
  it('não acha o termo no meio de outra palavra ("NSU" dentro de "consultados")', () => {
    expect(termosCortadosPresentesRetirada(`<p>${M.recuperar}</p>`)).toEqual([]);
  });
  it('a lista de sempre mantém os cortes do cartão: bandeira, final, NSU e caução aprovada', () => {
    for (const termo of ['Mastercard', 'NSU', 'AutoVia Pay', 'Cartão físico', 'Pré-autorizado']) {
      expect(TERMOS_CORTADOS_RETIRADA).toContain(termo);
    }
    for (const termo of ['Cliente Gold', 'Box Totem', 'Serpro', 'Score de 98%', 'Salvar Rascunho', 'c/ taxas']) {
      expect(TERMOS_CORTADOS_RETIRADA).toContain(termo);
    }
  });
  it('"Garantia", "Caução", "Pré-autorização" e "Titularidade do cartão" ficam só na lista da caução', () => {
    expect(TERMOS_DA_CAUCAO_RETIRADA).toEqual(['Garantia', 'Caução', 'Pré-autorização', 'Titularidade do cartão']);
    for (const termo of TERMOS_DA_CAUCAO_RETIRADA) expect(TERMOS_CORTADOS_RETIRADA).not.toContain(termo);
  });
  it('"Pré-autorizado" (caução aprovada, cortado) não é achado em "Pré-autorização" (caução exigida)', () => {
    expect(termosCortadosPresentesRetirada(`<p>${M.preAutorizacaoTitulo('R$ 1.200,00')}</p>`)).toEqual([]);
    expect(termosCortadosPresentesRetirada('<p>Pré-autorizado no Mastercard final 4018</p>')).toEqual(['Pré-autorizado', 'Mastercard']);
  });
});
