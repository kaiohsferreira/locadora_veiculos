export type UnidadeAtraso = 'HORA' | 'DIARIA';
export type FracionamentoAtraso = 'PROPORCIONAL' | 'INICIADA';

import type { ConfiguracaoCobrancasExtras } from './configuracoesCobrancasExtras';
export interface MemoriaQuilometragem {
  kmEntrada: number; kmSaida: number; pacoteKmTotal: number; kmExcedentes: number;
  valorKmExcedente: number; valor: number;
}
export interface MemoriaCombustivel {
  nivelEntrada: number; nivelSaida: number; fracaoFaltante: number; valorTanqueCompleto: number; valor: number;
}
export interface MemoriaAtraso {
  dataPrevistaStr: string; dataSaidaStr: string; toleranciaAtrasoMinutos: number;
  /** Previsão efetiva (data combinada na hora da retirada). Nula em apuração gravada antes da correção do atraso. */
  dataPrevista?: string | null;
  unidadeAtraso: UnidadeAtraso; fracionamentoAtraso: FracionamentoAtraso;
  unidadesAposTolerancia: number; unidadesCobradas: number; valorUnidadeAtraso: number; valor: number;
}
export interface EstimativaAvaria { idAvaria: number; descricao: string; valorEstimado: number }
export interface MemoriaAvarias { valoresEstimados: true; itens: EstimativaAvaria[]; valor: number }
export interface CalculoCobrancasExtras {
  idLocacao: number; idVistoriaEntrada: number; idVistoriaSaida: number;
  simulacao: boolean; provisorio: boolean; aviso: string;
  configuracao: ConfiguracaoCobrancasExtras; diariasPrevistas: number;
  quilometragem: MemoriaQuilometragem; combustivel: MemoriaCombustivel;
  atraso: MemoriaAtraso; avarias: MemoriaAvarias; total: number;
}
export interface SimulacaoCobrancasExtras extends CalculoCobrancasExtras { simulacao: true; provisorio: true }
export interface ConfirmarApuracaoCobrancasExtras { idConfiguracao: number; versaoEsperada: number }
export interface CobrancaExtraApurada {
  idCobranca: number; tipo: 'KM_EXCEDENTE' | 'COMBUSTIVEL' | 'ATRASO' | 'AVARIA';
  descricao: string; valor: number; dataLancamentoStr: string;
}
export interface ApuracaoCobrancasExtras {
  idApuracao: number; idLocacao: number; idConfiguracao: number; versaoConfiguracao: number;
  dataApuracaoStr: string; simulacao: false; configuracao: ConfiguracaoCobrancasExtras;
  calculo: CalculoCobrancasExtras; cobrancas: CobrancaExtraApurada[]; total: number;
  /** Pagamento lançado pela apuração (cobrança extra PENDENTE). Nulo quando o total foi zero ou a apuração é anterior. */
  idPagamentoLancado?: number | null;
}
