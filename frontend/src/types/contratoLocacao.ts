/** Contrato digital da locação (contrato-locacao.contract.json v5): documento montado, assinatura e situação. */

export type SituacaoContrato = 'PENDENTE_ASSINATURA' | 'ASSINADO';

export interface ContratoLocadora {
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  telefone: string;
  email: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string | null;
  bairro: string;
  cidade: string;
  uf: string;
}

export interface ContratoLocatario {
  nome: string;
  cpfMascarado: string;
  cnhMascarada: string;
  cnhCategoria: string;
  cnhValidadeStr: string;
}

export interface ContratoVeiculo {
  marca: string;
  modelo: string;
  placa: string;
  chassi: string;
  renavam: string;
  anoFabricacao: number;
  anoModelo: number;
  cor: string | null;
}

export interface ContratoDadosLocacao {
  numero: number;
  dataRetiradaStr: string;
  dataDevolucaoPrevistaStr: string;
  valorDiariaStr: string;
  /** Nulo quando a locação não tem caução (a tela também aceita a omissão do campo). */
  valorCaucaoStr?: string | null;
  /** Diárias previstas, da retirada real até a devolução prevista (v5, O-12). */
  diariasPrevistas: number;
  /** Total previsto em texto de moeda: só diárias, sem extras, caução ou taxas (v5, O-12). */
  valorTotalPrevistoStr: string;
}

export interface ContratoVistoriaEntrada {
  dataHoraStr: string;
  kmRegistrado: number;
  /** Nível de 0 a 1 com 2 casas (N1); a tela mostra em percentual. Nulo quando a vistoria não registrou o nível (VO decimal?). */
  nivelCombustivel: number | null;
}

/** Configuração de cobranças da categoria usada no contrato (RN-12). Nula quando a categoria não tem configuração. */
export interface ContratoTabelaCobrancas {
  versaoConfiguracao: number;
  franquiaKm: number;
  /** TOTAL (franquia no total da locação) ou POR_DIARIA. */
  baseFranquia: string;
  valorKmExcedenteStr: string;
  valorTanqueCompletoStr: string;
  toleranciaAtrasoMinutos: number;
  unidadeAtraso: string;
  fracionamentoAtraso: string;
  valorUnidadeAtrasoStr: string;
  baseAvarias: string;
}

export interface ContratoClausula {
  numero: number;
  titulo: string;
  texto: string;
}

export interface ContratoDocumento {
  versaoDocumento: number;
  locadora: ContratoLocadora;
  locatario: ContratoLocatario;
  veiculo: ContratoVeiculo;
  locacao: ContratoDadosLocacao;
  vistoriaEntrada: ContratoVistoriaEntrada;
  /** Nulo quando a categoria não tem configuração; a tela também aceita a omissão do campo. */
  tabelaCobrancas?: ContratoTabelaCobrancas | null;
  clausulas: ContratoClausula[];
}

export interface ContratoAssinatura {
  nomeAssinante: string;
  aceiteCondicoesGerais: boolean;
  aceitePoliticaCombustivel: boolean | null;
  assinaturaPngBase64: string;
  /** dd/MM/yyyy HH:mm, como o servidor grava. */
  assinadoEmStr: string;
  idFuncionarioColetor: number;
  nomeFuncionarioColetor: string;
}

/** Retorno de GET e POST de /api/locacoes/{id}/contrato (ReturnContratoLocacaoVO). */
export interface ContratoLocacao {
  idLocacao: number;
  situacao: SituacaoContrato;
  hashSha256: string | null;
  documento: ContratoDocumento;
  assinatura: ContratoAssinatura | null;
}

/** Corpo do POST de assinatura (AssinarContratoLocacaoVO). Nenhum valor de documento, data ou id sai daqui. */
export interface AssinarContratoLocacao {
  nomeAssinante: string;
  aceiteCondicoesGerais: boolean;
  /** Nulo quando a cláusula de política de combustível não consta do documento (O-6). */
  aceitePoliticaCombustivel: boolean | null;
  assinaturaPngBase64: string;
}
