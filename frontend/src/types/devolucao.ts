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
  /** Nome abreviado de quem registrou (ex.: "Rogério V."); nulo quando o cadastro não tem nome. */
  operadorStr: string | null;
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

/** Ação da caução no encerramento (caucao-devolucao.contract.json, tabela de decisão). Só existe depois do encerramento. */
export type AcaoCaucaoDevolucao = 'SEM_CAUCAO' | 'BAIXADA' | 'RETIDA' | 'RETIDA_PARCIAL' | 'NAO_CONFIRMADA_NO_PROVEDOR';

/** Situação da caução antes do encerramento, lida do banco (o GET não chama o provedor). */
export interface CaucaoContexto {
  /** Status do pagamento CAUCAO (PENDENTE, PAGO, CANCELADO) ou SEM_CAUCAO quando a locação não tem caução. */
  situacao: string;
  /** Ação do encerramento; nula enquanto a locação não foi encerrada. */
  acao: AcaoCaucaoDevolucao | null;
}

/** Contexto da devolução (GET /api/locacoes/{id}/devolucao): vistoria-saida.contract.json v4 e devolucao-encerramento v4. */
export interface ContextoDevolucao {
  locacao: { idLocacao: number; idReserva: number; idVeiculo: number; kmSaida: number; status: StatusLocacao; valorCaucao: number };
  entrada: VistoriaDevolucao | null;
  avariasEntrada: AvariaComparacao[];
  comparacao: ComparacaoSaida | null;
  placa: string;
  marca: string;
  modelo: string;
  diariasPrevistas: number;
  valorDiaria: number;
  valorDiariaStr: string;
  totalDiarias: number;
  totalDiariasStr: string;
  /** Total das cobranças extras apuradas; nulo sem vistoria de saída ou sem política da categoria. */
  totalExtras: number | null;
  totalExtrasStr: string | null;
  /** Total que o encerramento confirma; nulo nas mesmas condições de totalExtras. A tela nunca recalcula este valor. */
  valorTotalPrevisto: number | null;
  valorTotalPrevistoStr: string | null;
  /** Política de cobranças da categoria e a versão que a tela confirma; nulos sem política. */
  idConfiguracao: number | null;
  versaoEsperada: number | null;
  caucao: CaucaoContexto;
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

/** Corpo do POST /api/locacoes/{id}/devolucao: só estes três campos (devolucao-encerramento v4, request). */
export interface EncerrarLocacao {
  idConfiguracao: number;
  versaoEsperada: number;
  totalConfirmado: number;
}

/** Resposta do encerramento (ReturnEncerramentoLocacaoVO). Sem id_transacao e sem dado de cartão. */
export interface ResultadoEncerramento {
  locacao: {
    idLocacao: number;
    status: StatusLocacao;
    kmSaida: number;
    kmChegada: number;
    dataDevolucaoEfetiva: string;
    dataDevolucaoEfetivaStr: string;
    diariasPrevistas: number;
    valorDiaria: number;
    valorDiariaStr: string;
    valorTotal: number;
    valorTotalStr: string;
  };
  veiculo: { idVeiculo: number; status: 'DISPONIVEL' | 'RESERVADO'; kmAtual: number };
  cobrancas: {
    totalExtras: number;
    totalExtrasStr: string;
    totalDiarias: number;
    totalDiariasStr: string;
    idPagamentoCobrancaExtra: number | null;
    idPagamentoDiaria: number | null;
  };
  caucao: {
    acao: AcaoCaucaoDevolucao;
    valorReservado: number;
    valorReservadoStr: string;
    valorRetido: number;
    valorRetidoStr: string;
    valorLiberado: number;
    valorLiberadoStr: string;
    statusPagamento: string;
    tratarAMao: boolean;
  };
  /** Avisos já redigidos pelo servidor (textos aprovados). Vazia na repetição. */
  avisos: string[];
}

export type TipoCobrancaExtra = 'KM_EXCEDENTE' | 'COMBUSTIVEL' | 'ATRASO' | 'AVARIA' | 'MULTA_TRANSITO' | 'LIMPEZA';

export interface CobrancaLocacao {
  idCobranca: number;
  idApuracao: number | null;
  tipo: TipoCobrancaExtra;
  descricao: string;
  valor: number;
  valorStr: string;
  dataLancamento: string;
  dataLancamentoStr: string;
}
/** GET /api/locacoes/{id}/cobrancas (ReturnCobrancasLocacaoVO). Lista vazia com 200 quando não há apuração. */
export interface CobrancasLocacao {
  idLocacao: number;
  total: number;
  totalStr: string;
  cobrancas: CobrancaLocacao[];
}

/** Situações aceitas pela lista de devoluções pendentes (locacao.contract.json, v6 e v7): só em andamento e atrasada. */
export type StatusDevolucaoPendente = 'EM_ANDAMENTO' | 'ATRASADA';

/** Linha da lista de devoluções pendentes (ReturnDevolucaoPendenteVO). Sem CPF, e-mail, telefone ou CNH. */
export interface DevolucaoPendente {
  idLocacao: number;
  codigoReserva: string | null;
  nomeCliente: string;
  placa: string;
  marca: string;
  modelo: string;
  status: StatusDevolucaoPendente;
  atrasada: boolean;
  devolucaoPrevistaEfetiva: string;
  devolucaoPrevistaEfetivaStr: string;
}
export interface FiltroDevolucoesPendentes {
  status: StatusDevolucaoPendente;
  pagina: number;
  tamanhoPagina: number;
}
