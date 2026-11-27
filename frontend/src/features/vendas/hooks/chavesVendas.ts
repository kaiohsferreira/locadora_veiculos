import type { QueryKey } from '@tanstack/react-query';
import type { FiltroCatalogo, ParametrosSimulacao } from '@/types/vendas';

/**
 * Família de chaves da venda de seminovos. As chaves ficam aqui, num arquivo próprio, para os hooks não dependerem uns
 * dos outros.
 */
export const CHAVE_VENDAS = 'vendas';

/** Raiz de todas as consultas da venda: usada nas invalidações. */
export function chaveFamiliaVendas(): QueryKey {
  return [CHAVE_VENDAS];
}

/** Detalhe público de um seminovo. Sem id, a consulta não roda. */
export function chaveVeiculoAVenda(idVeiculo: number | null): QueryKey {
  return [CHAVE_VENDAS, 'veiculo', idVeiculo];
}

/** Simulação de um veículo com uma entrada e um número de parcelas. */
export function chaveSimulacao(idVeiculo: number | null, parametros: ParametrosSimulacao | null): QueryKey {
  return [CHAVE_VENDAS, 'simulacao', idVeiculo, parametros?.valorEntrada ?? null, parametros?.numeroParcelas ?? null];
}

/** Página do catálogo com os filtros aplicados. */
export function chaveCatalogo(filtro: FiltroCatalogo): QueryKey {
  return [CHAVE_VENDAS, 'catalogo', filtro];
}
