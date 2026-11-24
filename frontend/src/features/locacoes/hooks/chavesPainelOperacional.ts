import type { QueryKey } from '@tanstack/react-query';

/**
 * Família das consultas do painel operacional (GET /api/painel-operacional/hoje). Abrir a locação pela tela 16 invalida
 * esta raiz, para a lista de retiradas de hoje não mostrar como reserva o que já virou locação.
 */
export const CHAVE_PAINEL_OPERACIONAL = 'painel-operacional';

/** Raiz de todas as consultas do painel operacional: usada nas invalidações. */
export function chaveFamiliaPainelOperacional(): QueryKey {
  return [CHAVE_PAINEL_OPERACIONAL];
}

/** Lista de retiradas previstas para hoje (a página de entrada da retirada). */
export function chaveRetiradasDeHoje(): QueryKey {
  return [CHAVE_PAINEL_OPERACIONAL, 'hoje'];
}
