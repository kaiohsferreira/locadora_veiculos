/**
 * Cartões da tela 12 (listagem da frota): Veículos Disponíveis, Locados, Em Manutenção e Taxa de Ocupação. Funções puras,
 * sem React. Os números vêm da visão geral (GET /api/painel/visao-geral), a mesma consulta da tela 11 (cache compartilhado).
 */
import { formatarNumero } from '@/lib/formatters';
import { formatarPercentual } from '@/lib/formatadoresPainel';
import {
  MENSAGEM_FROTA_NENHUMA_ENCERRA_HOJE,
  MENSAGEM_FROTA_UMA_ENCERRA_HOJE,
  mensagemFrotaEncerramHoje,
} from '@/lib/mensagens';
import type { StatusVeiculo } from '@/types/enums';
import type { RespostaVisaoGeral } from '@/types/painel';

/** Números dos quatro cartões, já tirados da visão geral. */
export interface ResumoCartoesFrota {
  disponiveis: number;
  locados: number;
  emManutencao: number;
  /** Taxa de ocupação do servidor, em porcentagem (ex.: 78.5). */
  taxaOcupacaoPercentual: number;
  /** Veículos com devolução prevista para hoje (linha "N encerram hoje" do cartão Locados). */
  devolucoesPrevistasHoje: number;
}

/** Quantidade de veículos numa situação. Situação ausente da lista = zero (nenhum veículo nela). */
function quantidadeNaSituacao(
  frotaPorSituacao: RespostaVisaoGeral['frotaPorSituacao'],
  situacao: StatusVeiculo,
): number {
  return frotaPorSituacao
    .filter((item) => item.situacao === situacao)
    .reduce((soma, item) => soma + item.quantidade, 0);
}

/**
 * Números dos cartões a partir da visão geral. Sem resposta (carregando ou erro), nulo: a tela mostra o valor ausente e
 * não inventa número nenhum.
 */
export function montarResumoCartoesFrota(resposta: RespostaVisaoGeral | undefined): ResumoCartoesFrota | null {
  if (resposta === undefined) return null;
  return {
    disponiveis: quantidadeNaSituacao(resposta.frotaPorSituacao, 'DISPONIVEL'),
    locados: quantidadeNaSituacao(resposta.frotaPorSituacao, 'LOCADO'),
    emManutencao: quantidadeNaSituacao(resposta.frotaPorSituacao, 'MANUTENCAO'),
    taxaOcupacaoPercentual: resposta.taxaOcupacaoPercentual,
    devolucoesPrevistasHoje: resposta.devolucoesPrevistasHoje,
  };
}

/**
 * Proporção do anel e da barra da taxa de ocupação: a porcentagem limitada a 0 e 100 (acima de 100 o anel fica cheio;
 * abaixo de 0 ou sem número, vazio).
 */
export function proporcaoAnelFrota(percentual: number): number {
  if (!Number.isFinite(percentual) || percentual <= 0) return 0;
  return percentual >= 100 ? 100 : percentual;
}

/** Taxa de ocupação com uma casa decimal e vírgula: "78,5%" (zero sai "0,0%"). */
export function formatarTaxaOcupacaoFrota(percentual: number): string {
  return formatarPercentual(percentual, 1);
}

/** "Nenhuma encerra hoje", "1 encerra hoje" ou "31 encerram hoje". */
export function textoEncerramHojeFrota(quantidade: number): string {
  if (quantidade === 0) return MENSAGEM_FROTA_NENHUMA_ENCERRA_HOJE;
  if (quantidade === 1) return MENSAGEM_FROTA_UMA_ENCERRA_HOJE;
  return mensagemFrotaEncerramHoje(formatarNumero(quantidade));
}
