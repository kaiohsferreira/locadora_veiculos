/**
 * Formulário e decisões da baixa de cobrança (diálogo "Dar baixa", tela 09). A validação roda no front antes de chamar a
 * API (regra F4); o servidor continua sendo a autoridade (409 e 502 chegam com a mensagem dele).
 */
import { z } from 'zod';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGEM_FINANCEIRO_ERRO_FORMA, MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO } from '@/lib/mensagens';
import { FORMA_PAGAMENTO, type FormaPagamento } from '@/types/enums';
import { ErroApi } from '@/types/api';
import type { BaixarPagamentoRequisicao } from '@/types/painelFinanceiro';

/** Valores do formulário como o usuário escolhe: a forma (texto) e a confirmação da caixa do PIX. */
export interface ValoresBaixa {
  forma: string;
  confirmarPix: boolean;
}

export const VALORES_INICIAIS_BAIXA: ValoresBaixa = { forma: '', confirmarPix: false };

/** Confere se o texto é uma das formas da lista fechada do contrato pagamento. */
export function ehFormaPagamento(valor: string): valor is FormaPagamento {
  return (FORMA_PAGAMENTO as readonly string[]).includes(valor);
}

/** Esquema do diálogo: a forma é obrigatória e precisa estar na lista fechada. */
export function criarEsquemaBaixa() {
  return z.object({
    forma: z.string().refine(ehFormaPagamento, { error: MENSAGEM_FINANCEIRO_ERRO_FORMA }),
    confirmarPix: z.boolean(),
  });
}

/** Corpo do PATCH. Só chama depois da validação, então a forma já é uma da lista. */
export function montarRequisicaoBaixa(valores: ValoresBaixa): BaixarPagamentoRequisicao {
  return {
    forma: valores.forma as FormaPagamento,
    confirmarBaixaComPixGerado: valores.confirmarPix,
  };
}

/**
 * Verdadeiro quando o servidor recusou a baixa por haver PIX gerado (409). Aí o diálogo mostra a caixa "Confirmo a baixa
 * mesmo com o PIX gerado." e reenvia com a confirmação marcada. O ProblemDetails não traz código: a checagem usa o status
 * e o texto que o servidor manda (ver MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO).
 */
export function ehErroPixGeradoSemConfirmacao(erro: unknown): boolean {
  return erro instanceof ErroApi && erro.status === 409 && erro.mensagem === MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO;
}

/** O que o diálogo faz depois de uma recusa: a caixa do PIX (só no 409 do PIX gerado) e a mensagem que aparece. */
export interface DecisaoErroBaixa {
  mostrarCaixaPix: boolean;
  /** Sempre a mensagem do servidor (ou a padrão, quando o erro não é da API). Nunca texto próprio. */
  mensagem: string;
}

/** Decide, sem React, o que o diálogo mostra depois da falha da baixa. */
export function decidirAposErroBaixa(falha: unknown): DecisaoErroBaixa {
  return { mostrarCaixaPix: ehErroPixGeradoSemConfirmacao(falha), mensagem: obterMensagemErro(falha) };
}
