/**
 * Decisão sobre o fechamento que não veio do React (evento "close" do `<dialog>`), sem React e sem DOM.
 *
 * O navegador pode fechar o diálogo sozinho (ex.: Esc repetido sem interação do usuário), mesmo com o "cancel"
 * cancelado. Com envio em curso, a gaveta volta a abrir. Sem envio, quem controla o estado é avisado para fechar
 * também: senão sobra a página com o `overflow` travado e a gaveta invisível. Elemento que já saiu da página não faz nada.
 *
 * Em desenvolvimento, o React (StrictMode) desmonta e remonta o efeito: a limpeza fecha o diálogo e a montagem seguinte
 * o abre de novo. O evento "close" dessa limpeza chega depois, com o diálogo já aberto; ele não é fechamento do
 * navegador e fica de fora (aberto = ignorar).
 */
export type DecisaoFechamentoNativo = 'reabrir' | 'avisar' | 'ignorar';

export interface EstadoFechamentoNativo {
  /** Envio em curso: a gaveta não pode fechar. */
  bloqueada: boolean;
  /** O `<dialog>` ainda está na página (quando a gaveta já saiu, o evento não tem o que fazer). */
  conectada: boolean;
  /** O `<dialog>` está aberto no momento do evento (um "close" atrasado de fechamento que o próprio código já desfez). */
  aberta: boolean;
}

export function decidirFechamentoNativo({ bloqueada, conectada, aberta }: EstadoFechamentoNativo): DecisaoFechamentoNativo {
  if (!conectada || aberta) return 'ignorar';
  return bloqueada ? 'reabrir' : 'avisar';
}
