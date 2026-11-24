import type { FotoCapaVeiculo } from './fotosVeiculo';
import type { CategoriaCnh, StatusCadastroCliente, StatusLocacao, StatusReserva, StatusVeiculo } from './enums';

/**
 * GET /api/locacoes/retirada/{idReserva} (ReturnRetiradaVO, locacao.contract.json, leitura_da_retirada_v3).
 * Datas e valores vêm como texto pronto (Str); o bruto ao lado é só a data (DateOnly) ou o número.
 */
export interface ContextoRetirada {
  idReserva: number;
  codigoReserva: string;
  statusReserva: StatusReserva;
  idCliente: number;
  clienteNome: string;
  /** CPF já mascarado pelo servidor (ex.: 123.***.***-09). */
  clienteCpfMascarado: string;
  /** CNH inteira só para ATENDENTE e ADMINISTRADOR; os outros cargos recebem mascarada pelo servidor. */
  cnhNumero: string;
  cnhCategoria: CategoriaCnh;
  cnhValidade: string;
  cnhValidadeStr: string;
  /** Verdadeiro quando a validade da CNH é anterior a hoje. Só informa: não bloqueia a retirada. */
  cnhVencida: boolean;
  /** ISO 8601 sem fuso ou nulo quando o cliente não enviou a CNH pelo site. */
  cnhEnviadaEm: string | null;
  cnhEnviadaEmStr: string;
  comprovanteResidenciaEnviadoEm: string | null;
  comprovanteResidenciaEnviadoEmStr: string;
  statusCadastroLocadora: StatusCadastroCliente | null;
  idVeiculo: number;
  categoriaNome: string;
  anoModelo: number;
  marca: string;
  modelo: string;
  placa: string;
  kmAtual: number;
  statusVeiculo: StatusVeiculo;
  /** Capa do veículo ou nulo (sem foto, ou falha do armazenamento: nesse caso vem erroFotoCapa). */
  fotoCapa: FotoCapaVeiculo | null;
  /** Texto pronto do servidor quando o veículo tem foto e o armazenamento falhou. */
  erroFotoCapa: string | null;
  dataRetiradaPrevista: string;
  dataRetiradaPrevistaStr: string;
  dataDevolucaoPrevista: string;
  dataDevolucaoPrevistaStr: string;
  diariasPrevistas: number;
  valorDiaria: number;
  valorDiariaStr: string;
  valorEstimado: number | null;
  /** Vazio quando a reserva não tem valor estimado gravado: a linha "Total previsto" não aparece. */
  valorEstimadoStr: string;
  localRetiradaNome: string;
  localRetiradaEndereco: string;
  idLocacaoExistente: number | null;
  /**
   * Caução na retirada (locacao.contract.json v4, caucao_na_retirada_v4.get_retirada). Verdadeiro quando o servidor exige
   * caução (Pagamentos:ValorCaucao maior que 0). Falso: a tela 16 fica exatamente como sem caução.
   */
  caucaoExigida: boolean;
  /** Valor configurado no servidor; 0 quando a caução não é exigida. Nunca vai no corpo do POST. */
  valorCaucao: number;
  /** Valor pronto do servidor (ex.: "R$ 1.200,00"); com 0, "R$ 0,00". A tela só mostra com a caução exigida. */
  valorCaucaoStr: string;
  /** A abertura com caução pode ser tentada pela tela (hoje: caução exigida e provedor de simulação liberado). */
  caucaoDisponivel: boolean;
  /** Provedor de simulação (FAKE) com a trava liberada: a tela mostra o painel de simulação do cartão. */
  caucaoModoSimulacao: boolean;
}

/**
 * Campos do cartão mandados no POST /api/locacoes só com a caução exigida (LocacaoVO flat). Simulação: token gerado na
 * tela ("FAKE-" + aleatório, "FAKE-RECUSADO" ou "FAKE-ANALISE"), bandeira "visa", emissor e dispositivo nulos.
 */
export interface DadosCaucaoAbertura {
  caucaoTokenCartao: string;
  caucaoMetodoPagamentoId: string;
  caucaoEmissorId: number | null;
  caucaoIdentificadorDispositivo: string | null;
}

/** Abertura da locação: sem caução, só a reserva (como antes); com caução, também os campos do cartão. */
export interface AbrirLocacao { idReserva: number; caucao?: DadosCaucaoAbertura; }
/** Subconjunto usado de ReturnLocacaoVO. */
export interface LocacaoAberta { idLocacao: number; idReserva: number; kmSaida: number; status: StatusLocacao; }
