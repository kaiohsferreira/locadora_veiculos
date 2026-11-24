import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { ROTULO_STATUS, type StatusDominio } from '@/types/enums';
import type { ItemPainelOperacional } from '@/types/painelOperacional';

/** Para onde a linha leva: a retirada da reserva (tela 16) ou a vistoria da locação já aberta. */
export interface AcaoLinhaEntrada { tipo: 'retirada' | 'vistoria'; id: number }

/**
 * Ação de cada linha da lista de hoje. Origem RESERVA com idReserva leva à retirada; origem LOCACAO com idLocacao leva à
 * vistoria. Item sem o id da própria origem não tem ação.
 */
export function acaoDaLinha(item: ItemPainelOperacional): AcaoLinhaEntrada | null {
  if (item.origem === 'RESERVA' && item.idReserva !== null) return { tipo: 'retirada', id: item.idReserva };
  if (item.origem === 'LOCACAO' && item.idLocacao !== null) return { tipo: 'vistoria', id: item.idLocacao };
  return null;
}

/**
 * Texto da coluna Reserva: o codigoReserva como veio do servidor (sem montar nada no front). Só o item de locação sem código
 * (locação gravada sem reserva de origem) mostra o número da locação.
 */
export function textoColunaReserva(item: ItemPainelOperacional): string {
  if (item.codigoReserva !== null) return item.codigoReserva;
  return item.idLocacao !== null ? M.locacaoNumero(item.idLocacao) : '';
}

/** Só status com rótulo do domínio viram etiqueta com a cor do status; o resto aparece como texto neutro. */
export function ehStatusConhecido(status: string): status is StatusDominio {
  return Object.prototype.hasOwnProperty.call(ROTULO_STATUS, status);
}
