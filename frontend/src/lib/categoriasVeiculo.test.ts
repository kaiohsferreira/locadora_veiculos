import { describe, expect, it } from 'vitest';
import type { CategoriaVeiculo } from '@/types/categorias';
import { opcoesCategoriaVeiculo, situacaoCategoriasVeiculo } from './listasVeiculo';

function categoria(idCategoria: number, nome: string, descricao: string | null): CategoriaVeiculo {
  return { idCategoria, nome, descricao, valorDiariaBase: 100, valorDiariaBaseStr: 'R$ 100,00' };
}

const ATIVAS: CategoriaVeiculo[] = [
  categoria(1, 'Grupo B', 'Econômico Hatch Compacto'),
  categoria(2, 'Grupo G', null),
];

describe('opcoesCategoriaVeiculo', () => {
  it('mostra "nome - descrição" quando há descrição e só o nome quando não há', () => {
    expect(opcoesCategoriaVeiculo(ATIVAS, null)).toEqual([
      { valor: '1', rotulo: 'Grupo B - Econômico Hatch Compacto' },
      { valor: '2', rotulo: 'Grupo G' },
    ]);
  });

  it('categoria do veículo que não está entre as ativas (desativada) entra como opção extra no fim', () => {
    expect(opcoesCategoriaVeiculo(ATIVAS, { idCategoria: 9, nome: 'Grupo Antigo' })).toEqual([
      { valor: '1', rotulo: 'Grupo B - Econômico Hatch Compacto' },
      { valor: '2', rotulo: 'Grupo G' },
      { valor: '9', rotulo: 'Grupo Antigo' },
    ]);
  });

  it('categoria atual que já está na lista não duplica', () => {
    const opcoes = opcoesCategoriaVeiculo(ATIVAS, { idCategoria: 2, nome: 'Grupo G' });
    expect(opcoes).toHaveLength(2);
  });
});

describe('situacaoCategoriasVeiculo', () => {
  it('carregando tem prioridade sobre tudo', () => {
    expect(situacaoCategoriasVeiculo({ carregando: true, erro: true, quantidadeOpcoes: 0 })).toBe('carregando');
  });

  it('erro, sem carregar, mostra o erro com "Tentar de novo"', () => {
    expect(situacaoCategoriasVeiculo({ carregando: false, erro: true, quantidadeOpcoes: 0 })).toBe('erro');
  });

  it('sem nenhuma opção (lista vazia e sem categoria extra): vazio', () => {
    expect(situacaoCategoriasVeiculo({ carregando: false, erro: false, quantidadeOpcoes: 0 })).toBe('vazio');
  });

  it('com opções: pronto', () => {
    expect(situacaoCategoriasVeiculo({ carregando: false, erro: false, quantidadeOpcoes: 2 })).toBe('pronto');
  });
});
