import { describe, expect, it, vi } from 'vitest';
import type { Pagina } from '@/types/api';
import { reunirTodasAsPaginas } from './paginas';

/** Monta uma página com os itens dados, como a listagem da API devolve. */
function pagina(itens: number[], total: number, numero: number): Pagina<number> {
  return { itens, total, pagina: numero, tamanhoPagina: 100 };
}

describe('reunirTodasAsPaginas', () => {
  it('junta as páginas na ordem até alcançar o total', async () => {
    const buscar = vi.fn(async (numero: number) => {
      if (numero === 1) return pagina([1, 2], 5, 1);
      if (numero === 2) return pagina([3, 4], 5, 2);
      return pagina([5], 5, 3);
    });

    await expect(reunirTodasAsPaginas(buscar)).resolves.toEqual([1, 2, 3, 4, 5]);
    expect(buscar).toHaveBeenCalledTimes(3);
    expect(buscar.mock.calls.map(([numero]) => numero)).toEqual([1, 2, 3]);
  });

  it('com uma única página, faz uma chamada só', async () => {
    const buscar = vi.fn(async () => pagina([7, 8], 2, 1));

    await expect(reunirTodasAsPaginas(buscar)).resolves.toEqual([7, 8]);
    expect(buscar).toHaveBeenCalledTimes(1);
  });

  it('frota vazia: devolve lista vazia sem repetir a busca', async () => {
    const buscar = vi.fn(async () => pagina([], 0, 1));

    await expect(reunirTodasAsPaginas(buscar)).resolves.toEqual([]);
    expect(buscar).toHaveBeenCalledTimes(1);
  });

  it('para numa página vazia, mesmo que o total diga que há mais (sem laço infinito)', async () => {
    const buscar = vi.fn(async (numero: number) => (numero === 1 ? pagina([1], 50, 1) : pagina([], 50, numero)));

    await expect(reunirTodasAsPaginas(buscar)).resolves.toEqual([1]);
    expect(buscar).toHaveBeenCalledTimes(2);
  });

  it('propaga o erro de uma página (a consulta falha por inteiro)', async () => {
    const erro = new Error('falha de rede');
    const buscar = vi.fn(async (numero: number) => {
      if (numero === 2) throw erro;
      return pagina([1], 150, 1);
    });

    await expect(reunirTodasAsPaginas(buscar)).rejects.toBe(erro);
  });
});
