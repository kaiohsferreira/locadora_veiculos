import type { FracionamentoAtraso, UnidadeAtraso } from './cobrancasExtras';

export type BaseFranquia = 'TOTAL' | 'POR_DIARIA';
export interface DadosConfiguracaoCobrancasExtras {
  idCategoria: number;
  baseFranquia: BaseFranquia;
  franquiaKm: number;
  valorKmExcedente: number;
  valorTanqueCompleto: number;
  toleranciaAtrasoMinutos: number;
  unidadeAtraso: UnidadeAtraso;
  fracionamentoAtraso: FracionamentoAtraso;
  valorUnidadeAtraso: number;
  baseAvarias: 'ESTIMATIVA_VISTORIA';
}
export interface ConfiguracaoCobrancasExtras extends DadosConfiguracaoCobrancasExtras {
  idConfiguracao: number;
  versao: number;
  categoriaNome: string;
  updatedAtStr: string;
}
export interface EditarConfiguracaoCobrancasExtras extends DadosConfiguracaoCobrancasExtras { versaoEsperada: number }
export interface FiltroConfiguracoesCobrancasExtras { pagina: number; tamanhoPagina: number; idCategoria?: number }
