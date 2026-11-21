/**
 * Baixa de cobrança pelo financeiro (contrato pagamento v5: PATCH /api/pagamentos/{id}/baixa). A tela 09 usa esta função
 * pelo hook do TanStack Query, nunca direto. Erros chegam como ErroApi (409 e 502 com a mensagem do servidor).
 */
import { api } from './client';
import type { BaixarPagamentoRequisicao, RespostaBaixaPagamento } from '@/types/painelFinanceiro';

/** Caminho da baixa de uma cobrança. */
export function caminhoBaixarPagamento(idPagamento: number): string {
  return `/api/pagamentos/${idPagamento}/baixa`;
}

/** Dá baixa na cobrança pela forma escolhida (200 com a cobrança baixada). */
export async function baixarPagamento(
  idPagamento: number,
  requisicao: BaixarPagamentoRequisicao,
): Promise<RespostaBaixaPagamento> {
  const resposta = await api.patch<RespostaBaixaPagamento>(caminhoBaixarPagamento(idPagamento), requisicao);
  return resposta.data;
}
