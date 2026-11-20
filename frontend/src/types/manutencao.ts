/**
 * Manutenção da frota (contrato manutencao.contract.json v2, P8-BE-01, P8-BE-02, P8-BE-03 e a listagem e o resumo da
 * semana 4). Nomes em português como no contrato. Datas no formato "AAAA-MM-DD" (DateOnly); valores em reais como número.
 */
import type { TipoManutencao } from '@/types/enums';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

/** Situação da ordem na listagem: aberta (sem data de saída) ou encerrada. */
export const SITUACAO_ORDEM_MANUTENCAO = ['ABERTA', 'ENCERRADA'] as const;
export type SituacaoOrdemManutencao = (typeof SITUACAO_ORDEM_MANUTENCAO)[number];

/** Situação de um alerta de revisão: VENCIDA ou PROXIMA (EM_DIA não entra na lista). */
export type SituacaoRevisao = 'VENCIDA' | 'PROXIMA';

/** Corpo do POST /api/manutencoes (AbrirManutencaoVO). A quilometragem não vai: o servidor usa a do veículo. */
export interface AbrirManutencaoRequisicao {
  idVeiculo: number;
  tipo: TipoManutencao;
  /** Ausente quando vazio. Até 100 caracteres. */
  oficina?: string;
  /** Ausente quando vazia. Até 200 caracteres. */
  descricao?: string;
  /** Ausente = hoje (pelo relógio do servidor). Data futura é recusada. */
  dataEntrada?: string;
}

/** Corpo do PATCH /api/manutencoes/{id}/encerrar (EncerrarManutencaoVO). */
export interface EncerrarManutencaoRequisicao {
  /** Quilometragem do veículo na saída (obrigatória, zero ou mais). */
  kmAtual: number;
  /** Ausente = hoje. Data futura e data anterior à entrada são recusadas. */
  dataSaida?: string;
  /** Ausente quando não informado. Zero ou mais, até duas casas decimais. */
  custo?: number;
}

/** Ordem devolvida pela abertura (201) e pelo encerramento (200), ReturnManutencaoVO. */
export interface ManutencaoRetorno {
  idManutencao: number;
  idVeiculo: number;
  placa: string;
  tipo: TipoManutencao;
  dataEntrada: string;
  dataEntradaStr: string;
  /** Nula enquanto a ordem estiver aberta. */
  dataSaida: string | null;
  dataSaidaStr: string;
  /** Quilometragem da abertura (não muda no encerramento). */
  kmRegistrado: number;
  oficina: string | null;
  descricao: string | null;
  /** Nulo enquanto não informado. */
  custo: number | null;
  custoStr: string;
  aberta: boolean;
}

/** Linha da listagem de ordens (ReturnManutencaoListaVO), com a capa do veículo. */
export interface ManutencaoListaRetorno {
  idManutencao: number;
  idVeiculo: number;
  placa: string;
  marca: string;
  modelo: string;
  /** Capa do veículo; nula quando não há foto ou quando o armazenamento falhou. */
  fotoCapa: FotoCapaVeiculo | null;
  /** Texto pronto do servidor quando a capa não pôde ser carregada. */
  erroFotoCapa: string | null;
  tipo: TipoManutencao;
  dataEntrada: string;
  dataEntradaStr: string;
  dataSaida: string | null;
  dataSaidaStr: string;
  kmRegistrado: number;
  oficina: string | null;
  descricao: string | null;
  custo: number | null;
  custoStr: string;
  aberta: boolean;
}

/** Filtros da listagem (GET /api/manutencoes). A ausência de um filtro vira parâmetro ausente. */
export interface FiltroManutencao {
  pagina: number;
  /** De 1 a 100 na API; a tela oferece 10, 20 e 50. */
  tamanhoPagina: number;
  /** Termo de busca (placa, marca, modelo ou número da OS). Vazio não vai à API. */
  busca?: string;
  tipo?: TipoManutencao;
  situacao?: SituacaoOrdemManutencao;
  /** Ordens de um veículo ("Ver Histórico"). */
  idVeiculo?: number;
}

/** Item do alerta de revisão (ReturnAlertaRevisaoVO), com a capa do veículo (ajuste aditivo da v2). */
export interface AlertaRevisaoRetorno {
  idVeiculo: number;
  placa: string;
  marca: string;
  modelo: string;
  categoriaNome: string;
  kmAtual: number;
  kmUltimaPreventiva: number;
  kmProximaRevisao: number;
  /** Quilômetros que faltam; negativo quando a revisão já venceu. */
  faltamKm: number;
  /** Percentual do ciclo já percorrido, com uma casa decimal; pode passar de 100. */
  percentualCiclo: number;
  situacao: SituacaoRevisao;
  fotoCapa: FotoCapaVeiculo | null;
  erroFotoCapa: string | null;
}

/** Lista de alertas (ReturnAlertasRevisaoVO), com o intervalo padrão que a tela mostra. */
export interface AlertasRevisaoRetorno {
  /** Quantos veículos estão VENCIDA ou PROXIMA (sem o teto da lista). */
  total: number;
  itens: AlertaRevisaoRetorno[];
  /** Intervalo padrão de revisão, em km (10000). */
  intervaloRevisaoKm: number;
}

/** Números do cabeçalho da tela (ReturnResumoManutencaoVO). O período é sempre o mês corrente. */
export interface ResumoManutencaoRetorno {
  /** Ordens abertas da locadora (não depende do período): "OS Ativas". */
  ordensAbertas: number;
  periodoInicio: string;
  periodoInicioStr: string;
  periodoFim: string;
  periodoFimStr: string;
  /** Soma do custo das ordens encerradas no período, pela data de saída: "Custo Mês". */
  custoPeriodo: number;
  custoPeriodoStr: string;
}
