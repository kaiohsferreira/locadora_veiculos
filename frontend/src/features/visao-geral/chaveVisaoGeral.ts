import type { QueryKey } from '@tanstack/react-query';

/**
 * Família de chaves da visão geral (cartões e pílulas da tela 11 vêm daqui). Fica num arquivo próprio, sem importar a
 * API, para que as mutações da frota invalidem a família sem carregar o cliente HTTP.
 */
export const CHAVE_VISAO_GERAL = 'painel-visao-geral';

/** Raiz da visão geral: usada nas invalidações (todos os períodos). */
export function chaveFamiliaVisaoGeral(): QueryKey {
  return [CHAVE_VISAO_GERAL];
}
