import type { QueryKey } from '@tanstack/react-query';
import type { FiltroTitulosFinanceiros } from '@/types/painelFinanceiro';

/**
 * Família das consultas do painel financeiro. Dar baixa invalida esta raiz, o que recarrega os indicadores e a tabela
 * (sem atualização automática por tempo).
 */
export const CHAVE_PAINEL_FINANCEIRO = 'painel-financeiro';

/** Raiz de todas as consultas do painel financeiro: usada nas invalidações. */
export function chaveFamiliaPainelFinanceiro(): QueryKey {
  return [CHAVE_PAINEL_FINANCEIRO];
}

/** Indicadores (recebido, a receber, em atraso e ticket médio). */
export function chaveIndicadoresFinanceiros(): QueryKey {
  return [CHAVE_PAINEL_FINANCEIRO, 'indicadores'];
}

/** Página da tabela para uma aba e uma página: cada combinação tem a sua chave. */
export function chaveTitulosFinanceiros(filtro: FiltroTitulosFinanceiros): QueryKey {
  return [CHAVE_PAINEL_FINANCEIRO, 'titulos', filtro];
}
