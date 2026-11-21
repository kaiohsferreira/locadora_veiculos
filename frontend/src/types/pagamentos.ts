import type { Pagina } from '@/types/api';
import type { FormaPagamento, StatusPagamento, TipoPagamento } from '@/types/enums';

/** Abas da tela (parâmetro `aba` de GET /api/pagamentos/meus). */
export const ABAS_PAGAMENTOS_CLIENTE = ['TODOS', 'PENDENTES', 'PAGOS'] as const;
export type AbaPagamentosCliente = (typeof ABAS_PAGAMENTOS_CLIENTE)[number];

/** Períodos do seletor (parâmetro `periodo`). Limitam só o começo do vencimento. */
export const PERIODOS_PAGAMENTOS_CLIENTE = ['DOZE_MESES', 'ANO_ATUAL', 'SEIS_MESES', 'TUDO'] as const;
export type PeriodoPagamentosCliente = (typeof PERIODOS_PAGAMENTOS_CLIENTE)[number];

/**
 * Uma cobrança do próprio cliente (ReturnMeuPagamentoVO, contrato pagamento). Datas e valor chegam com o campo Str ao
 * lado: é o que a tela mostra. `diasParaVencer` e `situacao` vêm calculados do servidor.
 */
export interface MeuPagamento {
  idPagamento: number;
  tipo: TipoPagamento;
  valor: number;
  valorStr: string;
  /** Data AAAA-MM-DD (não usada na tela; a tela mostra dataVencimentoStr). */
  dataVencimento: string;
  /** Data dd/MM/yyyy como veio do servidor. Nunca passar por new Date. */
  dataVencimentoStr: string;
  /** Data dd/MM/yyyy ou nulo. */
  dataPagamentoStr: string | null;
  /** Vencimento menos hoje: 0 vence hoje, negativo vencido. Calculado no servidor. */
  diasParaVencer: number;
  situacao: StatusPagamento;
  forma: FormaPagamento | null;
  idLocacao: number | null;
  idVenda: number | null;
  /** Marca e modelo do veículo da locação ou da venda, ou nulo. */
  veiculoDescricao: string | null;
}

/** Resposta paginada de GET /api/pagamentos/meus (PaginaVO). */
export type RespostaMeusPagamentos = Pagina<MeuPagamento>;

/** Parâmetros da listagem do cliente. */
export interface FiltroMeusPagamentos {
  aba: AbaPagamentosCliente;
  periodo: PeriodoPagamentosCliente;
  pagina: number;
  tamanhoPagina: number;
}

/**
 * Resposta de POST /api/pagamentos/meus/{id}/pix (ReturnCobrancaPixVO, contrato pagamento-pix). Gerar e reabrir devolvem
 * a mesma forma. Não traz identificador do provedor nem dado pessoal do pagador.
 */
export interface CobrancaPix {
  idPagamento: number;
  forma: 'PIX';
  valor: number;
  /** Valor no formato de moeda do servidor. Mostrado como veio. */
  valorStr: string;
  /** PNG em base64, sem prefixo `data:`. A tela monta `data:image/png;base64,`. */
  qrCodeBase64: string;
  copiaECola: string;
  /** dd/MM/yyyy HH:mm:ss. Só para exibição; a contagem usa `segundosParaExpirar`. */
  expiraEmStr: string;
  /** Segundos até a validade, calculados pelo servidor no momento da resposta. */
  segundosParaExpirar: number;
}

/** Resposta de GET /api/pagamentos/meus/resumo (ReturnResumoMeusPagamentosVO). */
export interface ResumoMeusPagamentos {
  /** Ano corrente, pelo servidor. */
  anoExercicio: number;
  totalLiquidadoAno: number;
  totalLiquidadoAnoStr: string;
  quantidadeQuitadasAno: number;
  totalPendente: number;
  totalPendenteStr: string;
  quantidadePendentes: number;
  quantidadeAbaPendentes: number;
  quantidadeAbaPagos: number;
}
