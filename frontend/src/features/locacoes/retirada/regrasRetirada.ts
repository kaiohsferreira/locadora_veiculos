import { ROTULO_STATUS_RESERVA, type StatusReserva } from '@/types/enums';
import type { ContextoRetirada } from '@/types/retirada';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { caucaoLiberaAbertura, SEM_CAUCAO_NA_TELA, type EstadoCaucaoNaTela } from './regrasCaucaoRetirada';
import { itensDaConferencia, type ConferenciaRetirada, type ItemConferencia } from './validacaoRetirada';

/** Funções puras da tela 16: sem React, testadas direto. */

/** Itens ligados da lista em uso (3 sem caução, 4 com a caução exigida). O total do contador é o tamanho da lista. */
export function contarConferidos(valores: ConferenciaRetirada, itens: readonly ItemConferencia[]): number {
  return itens.filter((item) => valores[item]).length;
}

function todosConferidos(valores: ConferenciaRetirada, itens: readonly ItemConferencia[]): boolean {
  return contarConferidos(valores, itens) === itens.length;
}

/**
 * Selo "Pronto para despacho de pátio": os itens em uso, a retirada disponível e, com a caução exigida, a caução que pode
 * ser tentada pela tela, o cargo da sessão que inicia com caução e uma saída da simulação escolhida (caucaoLiberaAbertura).
 * Sem caução (padrão), a regra de antes, para qualquer cargo.
 */
export function prontoParaDespacho(valores: ConferenciaRetirada, disponivel: boolean, caucao: EstadoCaucaoNaTela = SEM_CAUCAO_NA_TELA): boolean {
  return todosConferidos(valores, itensDaConferencia(caucao.exigida)) && disponivel && caucaoLiberaAbertura(caucao);
}

/** Botão principal: a mesma regra do "Pronto para despacho" e nenhum envio ou recarga em andamento. */
export function botaoLigado(valores: ConferenciaRetirada, disponivel: boolean, ocupado: boolean, caucao: EstadoCaucaoNaTela = SEM_CAUCAO_NA_TELA): boolean {
  return prontoParaDespacho(valores, disponivel, caucao) && !ocupado;
}

/** Subtítulo da página: o do desenho, com "garantias", só com a caução exigida; senão (e sem dados), o de hoje. */
export function subtituloDaRetirada(caucaoExigida: boolean): string {
  return caucaoExigida ? M.subtituloComCaucao : M.subtitulo;
}

/** Status de erro que já trazem o motivo completo no texto do servidor (com a caução exigida, sem complemento). */
const STATUS_SEM_COMPLEMENTO_COM_CAUCAO: readonly number[] = [400, 403, 429];
const STATUS_CONFLITO = 409;

/**
 * Complemento do erro da abertura, mostrado abaixo do texto do servidor. Sem caução, como antes: 409 pede para conferir os
 * dados atualizados e o resto avisa que a consulta foi refeita. Com a caução exigida: 409 com a retirada ainda disponível
 * pede uma nova escolha na simulação (recusa, em análise ou nova tentativa); 409 com a retirada indisponível é conflito;
 * 400, 403 e 429 não têm complemento; os outros (0, 404, 408, 500, 502, 503, 504...) avisam que a consulta foi refeita.
 */
export function complementoErroAbertura(status: number, caucaoExigida: boolean, retiradaDisponivelAgora: boolean): string | null {
  if (!caucaoExigida) return status === STATUS_CONFLITO ? M.conflito : M.recuperar;
  if (status === STATUS_CONFLITO) return retiradaDisponivelAgora ? M.caucaoTenteNovamente : M.conflito;
  if (STATUS_SEM_COMPLEMENTO_COM_CAUCAO.includes(status)) return null;
  return M.recuperar;
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
