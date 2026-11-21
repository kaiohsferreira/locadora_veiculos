/**
 * Regras de exibição do painel financeiro (tela 09). Funções puras, sem React. Não há regra de negócio aqui: o período, os
 * limites, o ticket e o CPF mascarado vêm prontos do servidor. Estas funções só escolhem o texto e o botão da linha.
 */
import {
  MENSAGEM_FINANCEIRO_VENCE_AMANHA,
  MENSAGEM_FINANCEIRO_VENCE_HOJE,
  MENSAGEM_FINANCEIRO_VENCIDO_UM_DIA,
  mensagemFinanceiroLinhaAReceber,
  mensagemFinanceiroLinhaEmAtraso,
  mensagemFinanceiroLinhaRecebido,
  mensagemFinanceiroVenceEm,
  mensagemFinanceiroVencidoHa,
} from '@/lib/mensagens';
import { formatarNumero } from '@/lib/formatters';
import type { SituacaoTituloFinanceiro, TituloFinanceiro } from '@/types/painelFinanceiro';

/** Tipo de lançamento que não é título do painel (caução é retenção, não dívida). */
export const TIPO_TITULO_CAUCAO = 'CAUCAO';

/** Só o que a tela usa: a situação e os dias até o vencimento. */
type CamposSituacao = Pick<TituloFinanceiro, 'situacao' | 'diasParaVencer'>;

/**
 * Situação mostrada na linha. PENDENTE com vencimento passado vira ATRASADO (a mesma regra do servidor, igual à tela 30).
 * Quando o servidor já manda ATRASADO, a função devolve o mesmo valor.
 */
export function situacaoExibida(titulo: CamposSituacao): SituacaoTituloFinanceiro {
  if (titulo.situacao === 'PENDENTE' && titulo.diasParaVencer < 0) return 'ATRASADO';
  return titulo.situacao;
}

/** Caução nunca entra na lista da tela. */
export function ehTituloExibivel(titulo: Pick<TituloFinanceiro, 'tipo'>): boolean {
  return titulo.tipo !== TIPO_TITULO_CAUCAO;
}

/** O botão "Dar baixa" aparece só em PENDENTE e ATRASADO, e só quando o servidor também libera a baixa. */
export function podeMostrarDarBaixa(titulo: TituloFinanceiro): boolean {
  const situacao = situacaoExibida(titulo);
  return titulo.podeDarBaixa && (situacao === 'PENDENTE' || situacao === 'ATRASADO');
}

/**
 * Texto do vencimento na linha: "Vence hoje" (0), "Vence amanhã" (1), "Vence em N dias" (N acima de 1), "Vencido há N
 * dias" (negativo) e "Vencido há 1 dia" (um dia de atraso).
 */
export function textoVencimento(diasParaVencer: number): string {
  if (diasParaVencer === 0) return MENSAGEM_FINANCEIRO_VENCE_HOJE;
  if (diasParaVencer === 1) return MENSAGEM_FINANCEIRO_VENCE_AMANHA;
  if (diasParaVencer > 1) return mensagemFinanceiroVenceEm(String(diasParaVencer));
  const diasVencido = -diasParaVencer;
  return diasVencido === 1
    ? MENSAGEM_FINANCEIRO_VENCIDO_UM_DIA
    : mensagemFinanceiroVencidoHa(String(diasVencido));
}

/** Linha do cartão "Recebido no Mês": singular para exatamente um pagamento. */
export function textoLinhaRecebido(quantidade: number): string {
  return mensagemFinanceiroLinhaRecebido(quantidade, formatarNumero(quantidade));
}

/** Linha do cartão "A receber no mês": "1 título" ou "142 títulos". */
export function textoLinhaAReceber(quantidade: number): string {
  return mensagemFinanceiroLinhaAReceber(quantidade, formatarNumero(quantidade));
}

/** Linha do cartão "Em atraso": "1 título vencido" ou "24 títulos vencidos". */
export function textoLinhaEmAtraso(quantidade: number): string {
  return mensagemFinanceiroLinhaEmAtraso(quantidade, formatarNumero(quantidade));
}

/**
 * Página que ainda existe para o total: quando a última linha de uma página sai da lista (ex.: depois de dar baixa), a
 * página pedida passa a não ter nada. Volta para a última página com linhas (ou para a 1, quando não há nenhuma). É a
 * mesma aritmética da paginação da tela de equipe; não é regra de negócio.
 */
export function ajustarPaginaAoTotal(pagina: number, total: number, tamanhoPagina: number): number {
  const ultimaPagina = Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
  return pagina > ultimaPagina ? ultimaPagina : pagina;
}
