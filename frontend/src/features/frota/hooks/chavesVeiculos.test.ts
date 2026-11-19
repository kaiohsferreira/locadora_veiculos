import { describe, expect, it } from 'vitest';
import {
  CHAVE_CATEGORIAS,
  CHAVE_VEICULOS,
  chaveCategorias,
  chaveFamiliaVeiculos,
  chaveFotosVeiculo,
  chaveListagemVeiculos,
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

describe('chave da listagem de veículos', () => {
  it('leva o filtro inteiro e fica sob a raiz dos veículos', () => {
    const filtro = { pagina: 1, tamanhoPagina: 10, status: 'LOCADO' as const };

    expect(chaveListagemVeiculos(filtro)).toEqual(['veiculos', 'lista', filtro]);
    expect(chaveListagemVeiculos(filtro)[0]).toBe(CHAVE_VEICULOS);
  });

  it('filtros diferentes geram chaves diferentes (página, busca)', () => {
    expect(chaveListagemVeiculos({ pagina: 1, tamanhoPagina: 10 })).not.toEqual(
      chaveListagemVeiculos({ pagina: 2, tamanhoPagina: 10 }),
    );
    expect(chaveListagemVeiculos({ pagina: 1, tamanhoPagina: 10, busca: 'a' })).not.toEqual(
      chaveListagemVeiculos({ pagina: 1, tamanhoPagina: 10, busca: 'b' }),
    );
  });
});
