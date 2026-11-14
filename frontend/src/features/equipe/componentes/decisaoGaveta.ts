/**
 * Decisão sobre o fechamento que não veio do React (evento "close" do `<dialog>`), sem React e sem DOM.
 *
 * O navegador pode fechar o diálogo sozinho (ex.: Esc repetido sem interação do usuário), mesmo com o "cancel"
 * cancelado. Com envio em curso, a gaveta volta a abrir. Sem envio, quem controla o estado é avisado para fechar
 * também: senão sobra a página com o `overflow` travado e a gaveta invisível. Elemento que já saiu da página não faz nada.
 */
export type DecisaoFechamentoNativo = 'reabrir' | 'avisar' | 'ignorar';

export interface EstadoFechamentoNativo {
  /** Envio em curso: a gaveta não pode fechar. */
  bloqueada: boolean;
  /** O `<dialog>` ainda está na página (quando a gaveta já saiu, o evento não tem o que fazer). */
  conectada: boolean;
}

export function decidirFechamentoNativo({ bloqueada, conectada }: EstadoFechamentoNativo): DecisaoFechamentoNativo {
  if (!conectada) return 'ignorar';
  return bloqueada ? 'reabrir' : 'avisar';
}
