/**
 * Visão geral da locadora (contrato painel.contract.json, P11-BE-01/02, telas 39 e 40). Nomes em português como no contrato.
 * Os campos em reais chegam nulos quando o cargo não pode ver valores; o front respeita `valoresVisiveis` e não decide sozinho.
 */
import type { StatusVeiculo } from '@/types/enums';

/** Períodos aceitos pelo parâmetro `periodo` (enum PeriodoVisaoGeral do backend). */
export const PERIODOS_VISAO_GERAL = ['HOJE', 'SETE_DIAS', 'TRINTA_DIAS', 'MES_ATUAL'] as const;
export type PeriodoVisaoGeral = (typeof PERIODOS_VISAO_GERAL)[number];

/** Período usado quando a pessoa não escolheu outro (padrão do contrato). */
export const PERIODO_PADRAO_VISAO_GERAL: PeriodoVisaoGeral = 'TRINTA_DIAS';

/** Quantidade de veículos em cada situação da frota. */
export interface FrotaPorSituacao {
  situacao: StatusVeiculo;
  quantidade: number;
}

/** Resposta de GET /api/painel/visao-geral (ReturnVisaoGeralVO). */
export interface RespostaVisaoGeral {
  periodo: PeriodoVisaoGeral;
  /** Datas do período já formatadas pelo servidor (dd/MM/yyyy). Não são usadas na tela: o front formata com `@/lib/formatters`. */
  periodoInicioStr: string;
  periodoFimStr: string;
  /** Verdadeiro quando a locadora ainda não tem veículo: a tela mostra a variante de primeiro acesso (tela 40). */
  primeiroAcesso: boolean;
  /** Verdadeiro quando o cargo pode ver valores em reais (ADMINISTRADOR ou FINANCEIRO). */
  valoresVisiveis: boolean;
  /** Nulo sem permissão de valores. */
  receitaPeriodo: number | null;
  receitaPeriodoStr: string;
  /** Variação em relação ao período anterior. Nulo quando não há base de comparação. */
  variacaoReceitaPercentual?: number | null;
  aReceber: number | null;
  aReceberStr: string;
  quantidadeCobrancasEmAberto: number;
  emAtraso: number | null;
  emAtrasoStr: string;
  quantidadeCobrancasVencidas: number;
  taxaOcupacaoPercentual: number;
  veiculosLocados: number;
  totalVeiculos: number;
  locacoesAtivas: number;
  devolucoesPrevistasHoje: number;
  frotaPorSituacao: FrotaPorSituacao[];
  /**
   * Veículos da locadora cadastrados no mês corrente (painel v2, aditivo, usado na tela 11). Ausente em servidor
   * antigo: a tela não mostra a linha "novos neste mês" nesse caso, e não quebra.
   */
  veiculosNovosNoMes?: number;
}
