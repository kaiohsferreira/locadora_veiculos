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
}

export interface AbrirLocacao { idReserva: number; }
/** Subconjunto usado de ReturnLocacaoVO. */
export interface LocacaoAberta { idLocacao: number; idReserva: number; kmSaida: number; status: StatusLocacao; }
