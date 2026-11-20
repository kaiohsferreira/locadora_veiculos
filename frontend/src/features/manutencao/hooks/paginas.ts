import type { Pagina } from '@/types/api';

/**
 * Junta todas as páginas de uma listagem paginada, na ordem. Para quando o que já chegou alcança o total ou quando uma
 * página vem vazia (proteção contra laço sem fim se o total mudar no meio). Recebe a função que busca cada página, então
 * não depende da API e pode ser testada sem rede.
 */
export async function reunirTodasAsPaginas<T>(buscarPagina: (pagina: number) => Promise<Pagina<T>>): Promise<T[]> {
  const itens: T[] = [];
  let pagina = 1;
  let total = Number.POSITIVE_INFINITY;
  while (itens.length < total) {
    const resposta = await buscarPagina(pagina);
    if (resposta.itens.length === 0) break;
    itens.push(...resposta.itens);
    total = resposta.total;
    pagina += 1;
  }
  return itens;
}
