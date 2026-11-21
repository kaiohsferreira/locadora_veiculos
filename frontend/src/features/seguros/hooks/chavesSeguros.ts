import type { QueryKey } from '@tanstack/react-query';
import type { FiltroApolices } from '@/types/seguros';

/**
 * Família de chaves da gestão de seguros. Cadastrar e editar invalidam a raiz, o que atualiza a lista, os indicadores e
 * o detalhe aberto. As chaves ficam aqui, num arquivo próprio, para os hooks não dependerem uns dos outros.
 */
export const CHAVE_SEGUROS = 'seguros';

/** Raiz de todas as consultas de seguros: usada nas invalidações. */
export function chaveFamiliaSeguros(): QueryKey {
  return [CHAVE_SEGUROS];
}

/** Página da lista com os filtros aplicados. */
export function chaveApolices(filtro: FiltroApolices): QueryKey {
  return [CHAVE_SEGUROS, 'lista', filtro];
}

/** Total de um indicador ("todas" ou "a vencer"), sem filtro de veículo. */
export function chaveTotalApolices(apenasAVencer: boolean): QueryKey {
  return [CHAVE_SEGUROS, 'total', apenasAVencer];
}

/** Uma apólice pelo id. Sem id, a consulta não roda. */
export function chaveApolice(idSeguro: number | null): QueryKey {
  return [CHAVE_SEGUROS, 'detalhe', idSeguro];
}

/** Busca de veículos por placa (família própria: não muda quando uma apólice é salva). */
export function chaveBuscaPlaca(termo: string): QueryKey {
  return ['veiculos', 'busca-placa', termo];
}
