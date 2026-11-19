import type { OpcaoSelect } from '@/components/ui';
import {
  mensagemLocacaoNumero,
  mensagemPagoVia,
  mensagemPeriodo,
  mensagemVencidaHa,
  mensagemVencimento,
  mensagemVendaNumero,
  mensagemFaixaPendencias,
} from '@/lib/mensagens';
import { FORMA_PAGAMENTO, ROTULO_FORMA_PAGAMENTO, type FormaPagamento } from '@/types/enums';
import {
  PERIODOS_PAGAMENTOS_CLIENTE,
  type AbaPagamentosCliente,
  type FiltroMeusPagamentos,
  type MeuPagamento,
  type PeriodoPagamentosCliente,
} from '@/types/pagamentos';

/** Itens por página da tabela (mesmo tamanho da aba Histórico de reservas). */
export const TAMANHO_PAGINA_PAGAMENTOS_CLIENTE = 10;

/** Filtro ao abrir a tela: aba Todos e período de 12 meses (as primeiras opções do desenho). */
export const FILTRO_INICIAL_PAGAMENTOS_CLIENTE: FiltroMeusPagamentos = {
  aba: 'TODOS',
  periodo: 'DOZE_MESES',
  pagina: 1,
  tamanhoPagina: TAMANHO_PAGINA_PAGAMENTOS_CLIENTE,
};

/** Ids das abas, para o foco e o aria-controls. */
export const ID_ABA_PAGAMENTOS_TODOS = 'pagamentos-aba-todos';
export const ID_ABA_PAGAMENTOS_PENDENTES = 'pagamentos-aba-pendentes';
export const ID_ABA_PAGAMENTOS_PAGOS = 'pagamentos-aba-pagos';
export const ID_PAINEL_PAGAMENTOS = 'pagamentos-painel';

/** Mudança de filtro vinda da tela: trocar aba ou período volta para a página 1. */
export type MudancaFiltroPagamentos =
  | { tipo: 'aba'; aba: AbaPagamentosCliente }
  | { tipo: 'periodo'; periodo: PeriodoPagamentosCliente }
  | { tipo: 'pagina'; pagina: number };

/** Aplica uma mudança de filtro. Só a mudança de página mantém a página pedida. */
export function aplicarMudancaFiltro(
  filtro: FiltroMeusPagamentos,
  mudanca: MudancaFiltroPagamentos,
): FiltroMeusPagamentos {
  if (mudanca.tipo === 'pagina') return { ...filtro, pagina: mudanca.pagina };
  if (mudanca.tipo === 'aba') return { ...filtro, aba: mudanca.aba, pagina: 1 };
  return { ...filtro, periodo: mudanca.periodo, pagina: 1 };
}

/** Última página existente para o total informado (sempre pelo menos 1). */
export function ultimaPaginaPagamentos(total: number, tamanhoPagina: number): number {
  return Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
}

/** Texto da faixa de pendência; nulo quando não há pendente (a faixa some). */
export function textoFaixaPendencias(quantidadePendentes: number): string | null {
  if (quantidadePendentes <= 0) return null;
  return mensagemFaixaPendencias(quantidadePendentes);
}

/** Pagar agora aparece só em cobrança pendente ou atrasada que não seja caução. */
export function podePagarNaLinha(pagamento: MeuPagamento): boolean {
  const pendente = pagamento.situacao === 'PENDENTE' || pagamento.situacao === 'ATRASADO';
  return pendente && pagamento.tipo !== 'CAUCAO';
}

/** Rótulo da forma usada na baixa; nulo quando a forma não é uma da lista fechada. */
export function rotuloForma(forma: string | null): string | null {
  if (forma === null) return null;
  if (!(FORMA_PAGAMENTO as readonly string[]).includes(forma)) return null;
  return ROTULO_FORMA_PAGAMENTO[forma as FormaPagamento];
}

/**
 * Subtexto embaixo do vencimento: pendente (vence hoje, amanhã ou em N dias), atrasado (vencida há N dias), paga (com
 * a forma usada). Cancelada não tem subtexto. Caução é bloqueio no cartão, não dívida: só mostra "Pago via" quando paga
 * com forma; em qualquer outra situação não tem subtexto de vencimento. Os dias vêm do servidor; a tela não recalcula
 * com o relógio do navegador.
 */
export function subtextoVencimento(pagamento: MeuPagamento): string | null {
  if (pagamento.situacao === 'PAGO') {
    const forma = rotuloForma(pagamento.forma);
    return forma === null ? null : mensagemPagoVia(forma);
  }
  if (pagamento.tipo === 'CAUCAO') return null;
  if (pagamento.situacao === 'PENDENTE') {
    return pagamento.diasParaVencer >= 0 ? mensagemVencimento(pagamento.diasParaVencer) : null;
  }
  if (pagamento.situacao === 'ATRASADO') {
    return pagamento.diasParaVencer < 0 ? mensagemVencidaHa(-pagamento.diasParaVencer) : null;
  }
  return null;
}

/** Linha da origem: "Locação nº 123" ou "Venda nº 45"; nulo quando não há origem. */
export function rotuloOrigem(pagamento: MeuPagamento): string | null {
  if (pagamento.idLocacao !== null) return mensagemLocacaoNumero(pagamento.idLocacao);
  if (pagamento.idVenda !== null) return mensagemVendaNumero(pagamento.idVenda);
  return null;
}

/** Opções do seletor de período, na ordem do desenho. O ano vem do servidor quando já chegou. */
export function opcoesPeriodo(ano: number): OpcaoSelect[] {
  return PERIODOS_PAGAMENTOS_CLIENTE.map((periodo) => ({
    valor: periodo,
    rotulo: mensagemPeriodo(periodo, ano),
  }));
}

/** Ícone do tipo da cobrança na linha da tabela (novo para cobrança extra e caução). */
export function iconeDoTipo(tipo: MeuPagamento['tipo']): string {
  switch (tipo) {
    case 'DIARIA':
    case 'VENDA':
      return 'directions_car';
    case 'COBRANCA_EXTRA':
      return 'receipt_long';
    case 'CAUCAO':
      return 'verified_user';
  }
}

/** Cobrança pendente, para o estilo do ícone e da data (PENDENTE ou ATRASADO). */
export function ehPendente(pagamento: MeuPagamento): boolean {
  return pagamento.situacao === 'PENDENTE' || pagamento.situacao === 'ATRASADO';
}

/** Há filtro quando a aba ou o período saíram do padrão (decide o texto do vazio). */
export function temFiltroAplicado(filtro: FiltroMeusPagamentos): boolean {
  return filtro.aba !== FILTRO_INICIAL_PAGAMENTOS_CLIENTE.aba || filtro.periodo !== FILTRO_INICIAL_PAGAMENTOS_CLIENTE.periodo;
}
