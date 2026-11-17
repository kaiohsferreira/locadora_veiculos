import { describe, expect, it } from 'vitest';
import {
  CHAVE_CATEGORIAS,
  CHAVE_VEICULOS,
  chaveCategorias,
  chaveFamiliaVeiculos,
  chaveFotosVeiculo,
  chaveVeiculo,
} from './chavesVeiculos';

describe('chaves dos veículos', () => {
  it('a família de veículos é a raiz que as mutações invalidam', () => {
    expect(CHAVE_VEICULOS).toBe('veiculos');
    expect(chaveFamiliaVeiculos()).toEqual(['veiculos']);
  });

  it('detalhe e fotos ficam sob a raiz, com o id do veículo', () => {
    expect(chaveVeiculo(7)).toEqual(['veiculos', 'detalhe', 7]);
    expect(chaveFotosVeiculo(7)).toEqual(['veiculos', 'fotos', 7]);
    expect(chaveFotosVeiculo(null)).toEqual(['veiculos', 'fotos', null]);
  });

  it('a chave de fotos leva só o id, sem nenhuma URL', () => {
    expect(JSON.stringify(chaveFotosVeiculo(7))).not.toContain('http');
  });

  it('categorias têm família própria, fora da raiz dos veículos', () => {
    expect(CHAVE_CATEGORIAS).toBe('categorias');
    expect(chaveCategorias()).toEqual(['categorias', 'lista']);
    expect(chaveCategorias()[0]).not.toBe(CHAVE_VEICULOS);
  });
});
