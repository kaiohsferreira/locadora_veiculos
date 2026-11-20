import type { StatusLocacao, GravidadeAvaria } from './enums';

export interface VistoriaDevolucao {
  idVistoria: number;
  idLocacao: number;
  idFuncionario: number;
  tipo: 'ENTRADA' | 'SAIDA';
  dataHoraStr: string;
  kmRegistrado: number;
  nivelCombustivel: number | null;
  observacoes: string | null;
}
export interface AvariaComparacao {
  idAvaria: number;
  idVistoria: number;
  idAvariaEntrada: number | null;
  descricao: string;
  gravidade: GravidadeAvaria;
  valorEstimado: number | null;
  valorEstimadoStr: string;
}
export interface ComparacaoSaida {
  entrada: VistoriaDevolucao | null;
  saida: VistoriaDevolucao;
  comparacaoRealizada: boolean;
  avariasPreexistentes: AvariaComparacao[];
  avariasNovas: AvariaComparacao[];
  avariasIndeterminadas: AvariaComparacao[];
}
export interface ContextoDevolucao {
  locacao: { idLocacao: number; idReserva: number; idVeiculo: number; kmSaida: number; status: StatusLocacao };
  entrada: VistoriaDevolucao | null;
  avariasEntrada: AvariaComparacao[];
  comparacao: ComparacaoSaida | null;
}
export interface NovaAvariaSaida {
  descricao: string;
  gravidade: GravidadeAvaria;
  /** Obrigatório no envio; 0 é válido e não gera cobrança. */
  valorEstimado: number;
}
export interface RegistrarSaida {
  kmRegistrado: number;
  nivelCombustivel: number;
  observacoes: string | null;
  idsAvariasEntrada: number[];
  novasAvarias: NovaAvariaSaida[];
}
