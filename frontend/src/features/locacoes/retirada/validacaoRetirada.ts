import { z } from 'zod';
import type { ContextoRetirada } from '@/types/retirada';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';

/**
 * Os atestados do atendente. Ficam só na tela: nada disso é gravado no servidor. A titularidade do cartão só entra na lista
 * em uso com a caução exigida (locacao.contract.json v4, caucao_na_retirada_v4.tela_16_v4).
 */
export interface ConferenciaRetirada {
  cnhOriginal: boolean;
  titularidadeCartao: boolean;
  comprovanteResidencia: boolean;
  termoMultas: boolean;
}
export type ItemConferencia = keyof ConferenciaRetirada;
/** Sem caução: os três atestados de hoje, na ordem do desenho. */
export const ITENS_CONFERENCIA: readonly ItemConferencia[] = ['cnhOriginal', 'comprovanteResidencia', 'termoMultas'];
/** Com a caução exigida: a titularidade do cartão entra como quarto atestado, entre a CNH e o comprovante (ordem do desenho). */
export const ITENS_CONFERENCIA_COM_CAUCAO: readonly ItemConferencia[] = ['cnhOriginal', 'titularidadeCartao', 'comprovanteResidencia', 'termoMultas'];
export const SEM_CONFERENCIA: ConferenciaRetirada = { cnhOriginal: false, titularidadeCartao: false, comprovanteResidencia: false, termoMultas: false };

/** Lista de atestados em uso: 3 sem caução e 4 com a caução exigida. */
export function itensDaConferencia(caucaoExigida: boolean): readonly ItemConferencia[] {
  return caucaoExigida ? ITENS_CONFERENCIA_COM_CAUCAO : ITENS_CONFERENCIA;
}

const ITEM_CONFERIDO = z.boolean().refine(Boolean, M.conferir);

/** Esquema que exige cada item da lista em uso (os de fora da lista não contam). */
export function esquemaConferenciaRetirada(itens: readonly ItemConferencia[]) {
  const forma: Partial<Record<ItemConferencia, typeof ITEM_CONFERIDO>> = {};
  for (const item of itens) forma[item] = ITEM_CONFERIDO;
  return z.object(forma);
}

export function lerIdReserva(valor: string | undefined): number | null {
  if (!valor || !/^[1-9]\d*$/.test(valor)) return null;
  const numero = Number(valor);
  return Number.isSafeInteger(numero) && numero <= 2147483647 ? numero : null;
}

/** Mesma condição do servidor para abrir a locação. A CNH vencida e o cadastro do cliente não entram aqui. */
export function retiradaDisponivel(contexto: ContextoRetirada): boolean {
  return (contexto.statusReserva === 'ATIVA' || contexto.statusReserva === 'CONFIRMADA')
    && contexto.statusVeiculo === 'RESERVADO' && contexto.idLocacaoExistente === null;
}
