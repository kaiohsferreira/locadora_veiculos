import { ROTULO_STATUS_RESERVA, type StatusReserva } from '@/types/enums';
import type { ContextoRetirada } from '@/types/retirada';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { ITENS_CONFERENCIA, type ConferenciaRetirada } from './validacaoRetirada';

/** Funções puras da tela 16: sem React, testadas direto. */

export function contarConferidos(valores: ConferenciaRetirada): number {
  return ITENS_CONFERENCIA.filter((item) => valores[item]).length;
}

function todosConferidos(valores: ConferenciaRetirada): boolean {
  return contarConferidos(valores) === ITENS_CONFERENCIA.length;
}

/** Botão principal: os três itens, a retirada disponível e nenhum envio ou recarga em andamento. */
export function botaoLigado(valores: ConferenciaRetirada, disponivel: boolean, ocupado: boolean): boolean {
  return todosConferidos(valores) && disponivel && !ocupado;
}

/** Selo "Pronto para despacho de pátio": mesma regra do botão, sem contar o envio em andamento. */
export function prontoParaDespacho(valores: ConferenciaRetirada, disponivel: boolean): boolean {
  return todosConferidos(valores) && disponivel;
}

export interface PassoRetirada { numero: number; nome: string; ativa: boolean; }

/** Quatro etapas fixas. Só a etapa 1 pode ficar ativa, e só com a retirada disponível. */
export function passosDaRetirada(disponivel: boolean): PassoRetirada[] {
  return M.passos.map((nome, indice) => ({ numero: indice + 1, nome, ativa: disponivel && indice === 0 }));
}

/** Aviso de CNH vencida, com a data que o servidor mandou. Nulo quando a CNH está em dia. */
export function textoAvisoCnhVencida(contexto: ContextoRetirada): string | null {
  return contexto.cnhVencida ? M.cnhVencida(contexto.cnhValidadeStr) : null;
}

/** Selo da situação: "Reserva " mais o rótulo real do status (a reserva chega ATIVA; nada grava CONFIRMADA). */
export function textoSituacaoReserva(status: StatusReserva): string {
  return `${M.situacaoReserva} ${ROTULO_STATUS_RESERVA[status]}`;
}

/** Selo escuro do desenho só para ATIVA e CONFIRMADA; os outros status usam a etiqueta comum. */
export function selodaSituacaoEscuro(status: StatusReserva): boolean {
  return status === 'ATIVA' || status === 'CONFIRMADA';
}

export function textoDiarias(quantidade: number): string {
  return quantidade === 1 ? M.diariaSingular : M.diarias(quantidade);
}

/** Linha de envio de documento: com data (Str preenchido) ou o texto de não enviado (Str vazio). */
export function linhaDeEnvio(dataStr: string, comData: (data: string) => string, semEnvio: string): string {
  return dataStr.trim() === '' ? semEnvio : comData(dataStr);
}

/** Linha verde do item marcado: diz quem conferiu nesta tela; sem nome, a variante sem nome. */
export function textoResultadoItem(nome: string | null | undefined): string {
  const limpo = nome?.trim() ?? '';
  return limpo === '' ? M.conferidoPeloAtendente : M.conferidoPor(limpo);
}

/** A linha "Total previsto" só aparece quando a reserva tem valor estimado gravado. */
export function mostrarTotalPrevisto(valorEstimadoStr: string): boolean {
  return valorEstimadoStr.trim() !== '';
}
