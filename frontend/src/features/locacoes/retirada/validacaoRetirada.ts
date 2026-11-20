import { z } from 'zod';
import type { ContextoRetirada } from '@/types/retirada';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
export interface ConferenciaRetirada { cpfConferido: boolean; cnhConferida: boolean; }
export const esquemaConferenciaRetirada = z.object({
  cpfConferido: z.boolean().refine(Boolean, M.conferir),
  cnhConferida: z.boolean().refine(Boolean, M.conferir),
});
export function lerIdReserva(valor: string | undefined): number | null {
  if (!valor || !/^[1-9]\d*$/.test(valor)) return null;
  const numero = Number(valor);
  return Number.isSafeInteger(numero) && numero <= 2147483647 ? numero : null;
}
export function retiradaDisponivel(contexto: ContextoRetirada): boolean {
  return (contexto.statusReserva === 'ATIVA' || contexto.statusReserva === 'CONFIRMADA')
    && contexto.statusVeiculo === 'RESERVADO' && contexto.idLocacaoExistente === null;
}
