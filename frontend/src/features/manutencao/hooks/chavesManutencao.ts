import type { QueryKey } from '@tanstack/react-query';
import type { FiltroManutencao } from '@/types/manutencao';

/**
 * Família das consultas de manutenção. Abrir ou encerrar uma ordem invalida esta raiz, o que atualiza a lista, o resumo
 * e os alertas. As chaves ficam aqui, num arquivo próprio, como as da frota.
 */
export const CHAVE_MANUTENCOES = 'manutencoes';

/** Raiz de todas as consultas de manutenção: usada nas invalidações. */
export function chaveFamiliaManutencao(): QueryKey {
  return [CHAVE_MANUTENCOES];
}

/** Listagem de ordens para um filtro. Cada combinação de página, tamanho, busca, tipo, situação e veículo tem a sua chave. */
export function chaveListagemManutencoes(filtro: FiltroManutencao): QueryKey {
  return [CHAVE_MANUTENCOES, 'lista', filtro];
}

/** Números do cabeçalho (OS Ativas e Custo Mês). */
export function chaveResumoManutencao(): QueryKey {
  return [CHAVE_MANUTENCOES, 'resumo'];
}

/** Alertas de revisão (cartões). */
export function chaveAlertasRevisao(): QueryKey {
  return [CHAVE_MANUTENCOES, 'alertas'];
}

/** Lista completa de veículos do seletor do modal de abrir ordem. */
export function chaveVeiculosParaOrdem(): QueryKey {
  return [CHAVE_MANUTENCOES, 'veiculos-ordem'];
}
