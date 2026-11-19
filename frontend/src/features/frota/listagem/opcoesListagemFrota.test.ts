import { describe, expect, it } from 'vitest';
import type { CategoriaVeiculo } from '@/types/categorias';
import { abreviarChassi, montarOpcoesCategoriaFrota, opcoesStatusListagemFrota } from './opcoesListagemFrota';

describe('opcoesStatusListagemFrota (filtro de status da tela 12)', () => {
  it('traz as quatro situações do filtro, com os rótulos singulares do projeto', () => {
    const opcoes = opcoesStatusListagemFrota();

    expect(opcoes.map((opcao) => opcao.valor)).toEqual(['DISPONIVEL', 'LOCADO', 'MANUTENCAO', 'RESERVADO']);
    expect(opcoes.map((opcao) => opcao.rotulo)).toEqual(['Disponível', 'Locado', 'Em manutenção', 'Reservado']);
  });

  it('não traz vendido, baixado nem o status de laudo (só aparecem em "Todos")', () => {
    const valores = opcoesStatusListagemFrota().map((opcao) => opcao.valor);

    expect(valores).not.toContain('VENDIDO');
    expect(valores).not.toContain('BAIXADO');
    expect(opcoesStatusListagemFrota().map((opcao) => opcao.rotulo).join(' ')).not.toContain('Laudo');
  });
});

describe('abreviarChassi', () => {
  it('chassi de 17 caracteres: 5 do começo, reticências e 4 do fim', () => {
    expect(abreviarChassi('9BD1237A8P849102')).toBe('9BD12...9102');
  });

  it('chassi com menos de 10 caracteres sai inteiro', () => {
    expect(abreviarChassi('123456789')).toBe('123456789');
  });

  it('chassi com exatamente 10 caracteres já é abreviado', () => {
    expect(abreviarChassi('ABCDEFGHIJ')).toBe('ABCDE...GHIJ');
  });

  it('vazio sai vazio, e espaços em volta não entram', () => {
    expect(abreviarChassi('')).toBe('');
    expect(abreviarChassi('  9BD1237A8P849102  ')).toBe('9BD12...9102');
  });
});

describe('montarOpcoesCategoriaFrota', () => {
  it('sem categorias (carregando), nenhuma opção', () => {
    expect(montarOpcoesCategoriaFrota(undefined)).toEqual([]);
  });

  it('a categoria vira opção com o id como valor e o nome com a descrição, como na tela 11', () => {
    const categorias: CategoriaVeiculo[] = [
      { idCategoria: 2, nome: 'Grupo B', descricao: 'Hatch Compacto 1.0', valorDiariaBase: 150, valorDiariaBaseStr: 'R$ 150,00' },
    ];

    expect(montarOpcoesCategoriaFrota(categorias)).toEqual([{ valor: '2', rotulo: 'Grupo B - Hatch Compacto 1.0' }]);
  });
});
