/**
 * Painel financeiro (contrato painel-financeiro v1, P7-BE-05; tela 09 em versão mínima) e a baixa de cobrança (contrato
 * pagamento v5). Os nomes seguem o JSON real do backend (camelCase). Datas e valores vêm com o campo Str ao lado: é o que
 * a tela mostra.
 */
import type { Pagina } from '@/types/api';
import type { FormaPagamento, StatusPagamento } from '@/types/enums';

/** Abas da tabela de títulos (parâmetro `aba`). */
export const ABAS_TITULOS_FINANCEIROS = ['TODOS', 'EM_ATRASO', 'VENCE_HOJE', 'PAGOS_RECENTES'] as const;
export type AbaTitulosFinanceiros = (typeof ABAS_TITULOS_FINANCEIROS)[number];

/** Situação mostrada na linha: PENDENTE vencido já vem como ATRASADO. */
export type SituacaoTituloFinanceiro = Extract<StatusPagamento, 'PENDENTE' | 'ATRASADO' | 'PAGO'>;

/** Resposta de GET /api/painel/financeiro (ReturnPainelFinanceiroVO): os quatro indicadores e o período usado. */
export interface RespostaIndicadoresFinanceiros {
  /** Início e fim do período (AAAA-MM-DD), para conferência. A tela mostra os campos Str. */
  de: string;
  ate: string;
  /** Datas do período em dd/MM/yyyy. */
  deStr: string;
  ateStr: string;
  recebidoValor: number;
  recebidoStr: string;
  quantidadeRecebido: number;
  aReceberValor: number;
  aReceberStr: string;
  quantidadeAReceber: number;
  emAtrasoValor: number;
  emAtrasoStr: string;
  quantidadeEmAtraso: number;
  ticketMedioValor: number;
  ticketMedioStr: string;
  quantidadeTicket: number;
}

/**
 * Uma linha da tabela de títulos (ReturnTituloFinanceiroVO). O CPF sai mascarado (NNN.***.***-NN) ou nulo. `tipo` é texto
 * do servidor; CAUCAO nunca chega aqui, mas a tela ainda descarta esse tipo por segurança.
 */
export interface TituloFinanceiro {
  idPagamento: number;
  /** LOCACAO ou VENDA. */
  origem: string;
  /** "Locação nº {id}" ou "Venda nº {id}". */
  origemStr: string;
  /** DIARIA, COBRANCA_EXTRA ou VENDA. */
  tipo: string;
  /** Diária, Cobrança extra ou Venda. */
  tipoStr: string;
  clienteNome: string;
  clienteCpfMascarado: string | null;
  valor: number;
  valorStr: string;
  dataVencimentoStr: string;
  /** Vencimento menos hoje: 0 vence hoje, positivo falta, negativo já venceu. */
  diasParaVencer: number;
  situacao: SituacaoTituloFinanceiro;
  dataPagamentoStr: string | null;
  forma: FormaPagamento | null;
  /** Verdadeiro quando a situação é PENDENTE ou ATRASADO. */
  podeDarBaixa: boolean;
}

/** Resposta de GET /api/painel/financeiro/titulos (ReturnTitulosFinanceirosVO): a página da aba e os quatro contadores. */
export interface RespostaTitulosFinanceiros {
  quantidadeTodos: number;
  quantidadeEmAtraso: number;
  quantidadeVenceHoje: number;
  quantidadePagosRecentes: number;
  pagina: Pagina<TituloFinanceiro>;
}

/** Parâmetros da lista de títulos. Sem período: vale o mês corrente pelo servidor. */
export interface FiltroTitulosFinanceiros {
  aba: AbaTitulosFinanceiros;
  pagina: number;
  tamanhoPagina: number;
}

/** Corpo de PATCH /api/pagamentos/{id}/baixa (BaixarPagamentoVO). */
export interface BaixarPagamentoRequisicao {
  forma: FormaPagamento;
  /** Confirmação para dar baixa por outra forma quando a cobrança tem PIX gerado. Padrão do servidor: falso. */
  confirmarBaixaComPixGerado: boolean;
}

/** Resposta de PATCH /api/pagamentos/{id}/baixa (ReturnPagamentoVO). Só o que a tela usa fica tipado aqui. */
export interface RespostaBaixaPagamento {
  idPagamento: number;
  tipo: string;
  valor: number;
  valorStr: string;
  dataVencimentoStr: string;
  dataPagamentoStr: string | null;
  forma: FormaPagamento | null;
  status: string;
}
