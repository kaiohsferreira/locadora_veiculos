import type { Pagina } from '@/types/api';
import type { StatusReserva } from '@/types/enums';

/**
 * Situações da lista do cliente (parâmetro `situacao` de GET /api/reservas, contrato reserva).
 * ATIVAS = status ATIVA e CONFIRMADA; HISTORICO = CANCELADA e CONCLUIDA.
 */
export const SITUACOES_RESERVA = ['ATIVAS', 'HISTORICO'] as const;
export type SituacaoReserva = (typeof SITUACOES_RESERVA)[number];

/**
 * Uma reserva do próprio cliente (ReturnReservaVO, contrato reserva). Data e valor vêm com o campo Str ao lado,
 * que é o que a tela exibe. A tela não lê `diarias` do contrato: calcula a partir das datas (calculosReserva).
 */
export interface ReservaCliente {
  idReserva: number;
  /** Código de exibição "AV-" + id com 5 dígitos (ex.: AV-00042). Gerado no back, só para exibição. */
  codigo: string;
  idVeiculo: number;
  /** Momento da criação, ISO 8601 sem fuso (ex.: 2026-11-16T14:22:10). */
  dataReserva: string;
  dataReservaStr: string;
  /** Datas no formato AAAA-MM-DD. */
  dataRetirada: string;
  dataRetiradaStr: string;
  dataDevolucao: string;
  dataDevolucaoStr: string;
  status: StatusReserva;
  /** Valor estimado em reais; null em reservas antigas. */
  valorEstimado: number | null;
  /** Valor em reais formatado pelo back; vazio quando o valor é nulo. */
  valorEstimadoStr: string;
  categoriaNome: string;
  marca: string;
  modelo: string;
  anoModelo: number;
  cor: string | null;
  placa: string;
  locadoraNomeFantasia: string;
  locadoraLogradouro: string;
  locadoraNumero: string;
  locadoraBairro: string;
  locadoraCidade: string;
  locadoraUf: string;
}

/** Parâmetros da listagem. `busca` só sai quando há termo depois de aparar. */
export interface FiltroReservas {
  pagina: number;
  tamanhoPagina: number;
  situacao: SituacaoReserva;
  busca?: string;
}

/** Resposta paginada de GET /api/reservas. */
export type RespostaReservas = Pagina<ReservaCliente>;
