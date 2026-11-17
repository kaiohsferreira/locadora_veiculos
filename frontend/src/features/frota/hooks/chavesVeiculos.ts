import type { QueryKey } from '@tanstack/react-query';

/**
 * Família de chaves dos veículos na frota. As mutações de veículo (cadastro, edição, remoção de foto e fim de lote de
 * fotos) invalidam esta raiz, o que atualiza o detalhe, as fotos e as listas que mostram a capa.
 * As chaves ficam aqui, num arquivo próprio, para os hooks não dependerem uns dos outros.
 */
export const CHAVE_VEICULOS = 'veiculos';

/** Família das categorias (lista do seletor). Global: não depende de locadora. */
export const CHAVE_CATEGORIAS = 'categorias';

/** Raiz de todos os veículos: usada nas invalidações. */
export function chaveFamiliaVeiculos(): QueryKey {
  return [CHAVE_VEICULOS];
}

/** Consulta de um veículo pelo id. Sem id, a chave fica sem veículo (a consulta não roda). */
export function chaveVeiculo(idVeiculo: number | null): QueryKey {
  return [CHAVE_VEICULOS, 'detalhe', idVeiculo];
}

/**
 * Lista de fotos de um veículo. Leva só o id: a URL assinada nunca entra na chave. Sem id (cadastro), a consulta
 * fica desligada.
 */
export function chaveFotosVeiculo(idVeiculo: number | null): QueryKey {
  return [CHAVE_VEICULOS, 'fotos', idVeiculo];
}

/** Lista de categorias para o seletor. */
export function chaveCategorias(): QueryKey {
  return [CHAVE_CATEGORIAS, 'lista'];
}
