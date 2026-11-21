/**
 * Tipos da gestão de seguros (P9-BE-01, /api/seguros, só funcionário). Os nomes seguem exatamente o retorno da API.
 */
import type { Pagina } from './api';

/** Apólice de seguro de um veículo (GET /api/seguros e /api/seguros/{id}, retorno do POST). */
export interface Apolice {
  idSeguro: number;
  idVeiculo: number;
  placaVeiculo: string;
  seguradora: string;
  numeroApolice: string;
  /** Início da vigência, "yyyy-MM-dd". */
  dataInicio: string;
  dataInicioStr: string;
  /** Fim da vigência, "yyyy-MM-dd". */
  dataFim: string;
  dataFimStr: string;
  valorPremio: number;
  valorPremioStr: string;
  cobertura: string | null;
  /** Dias de hoje até o fim da vigência: 0 = vence hoje; negativo = já venceu. Calculado pelo servidor. */
  diasParaVencer: number;
  /** Vence em até 60 dias (0 a 60), para a tela destacar. Calculado pelo servidor. */
  venceEm60Dias: boolean;
}

/** Corpo do POST e do PUT de /api/seguros. */
export interface ApoliceRequisicao {
  idVeiculo: number;
  seguradora: string;
  numeroApolice: string;
  /** "yyyy-MM-dd". */
  dataInicio: string;
  /** "yyyy-MM-dd"; deve ser posterior ao início. */
  dataFim: string;
  valorPremio: number;
  cobertura: string | null;
}

/** Filtros da listagem (query de GET /api/seguros). */
export interface FiltroApolices {
  pagina: number;
  tamanhoPagina: number;
  idVeiculo?: number;
  /** Só as que vencem de hoje até 60 dias. */
  apenasAVencer: boolean;
}

/** Página de apólices, ordenada pelo fim da vigência (as que vencem antes primeiro). */
export type RespostaApolices = Pagina<Apolice>;

/** Veículo encontrado na busca por placa (resumo do GET /api/veiculos). */
export interface VeiculoPlaca {
  idVeiculo: number;
  placa: string;
  marca: string;
  modelo: string;
}
