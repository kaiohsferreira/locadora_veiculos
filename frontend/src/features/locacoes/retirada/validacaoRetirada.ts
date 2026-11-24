import { z } from 'zod';
import type { ContextoRetirada } from '@/types/retirada';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';

/** Os três atestados do atendente. Ficam só na tela: nada disso é gravado no servidor. */
export interface ConferenciaRetirada { cnhOriginal: boolean; comprovanteResidencia: boolean; termoMultas: boolean; }
export type ItemConferencia = keyof ConferenciaRetirada;
/** Ordem do desenho. */
export const ITENS_CONFERENCIA: readonly ItemConferencia[] = ['cnhOriginal', 'comprovanteResidencia', 'termoMultas'];
export const SEM_CONFERENCIA: ConferenciaRetirada = { cnhOriginal: false, comprovanteResidencia: false, termoMultas: false };

export const esquemaConferenciaRetirada = z.object({
  cnhOriginal: z.boolean().refine(Boolean, M.conferir),
  comprovanteResidencia: z.boolean().refine(Boolean, M.conferir),
  termoMultas: z.boolean().refine(Boolean, M.conferir),
});

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
