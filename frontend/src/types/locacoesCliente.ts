import type { Pagina } from '@/types/api';
import type { StatusLocacao } from '@/types/enums';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

/**
 * Situações da lista do cliente (parâmetro `situacao` de GET /api/locacoes/minhas, contrato locacao-cliente).
 * EM_ANDAMENTO = status EM_ANDAMENTO e ATRASADA; HISTORICO = FINALIZADA e CANCELADA.
 */
export const SITUACOES_LOCACAO = ['EM_ANDAMENTO', 'HISTORICO'] as const;
export type SituacaoLocacao = (typeof SITUACOES_LOCACAO)[number];

/**
 * Uma locação do próprio cliente (ReturnMinhaLocacaoVO, contrato locacao-cliente v3). Datas e valores vêm com o campo
 * Str ao lado, que é o que a tela exibe. A devolução prevista efetiva vem pronta do servidor: a tela não a recalcula.
 */
export interface LocacaoCliente {
  idLocacao: number;
  /** Código de exibição da reserva de origem (AV-00042); nulo quando a locação não tem reserva. */
  codigoReserva: string | null;
  status: StatusLocacao;
  /** Verdadeiro quando a locação está ATRASADA ou a devolução prevista efetiva já passou (calculado no servidor). */
  atrasada: boolean;
  dataRetirada: string;
  dataRetiradaStr: string;
  dataDevolucaoPrevista: string;
  dataDevolucaoPrevistaStr: string;
  /** Data da devolução prevista combinada na hora da retirada (regra B1). É a data que a contagem regressiva usa. */
  devolucaoPrevistaEfetiva: string;
  devolucaoPrevistaEfetivaStr: string;
  /** Nulo enquanto a locação está aberta. */
  dataDevolucaoEfetiva: string | null;
  dataDevolucaoEfetivaStr: string | null;
  /** Diárias pela retirada real (mesma conta da apuração). */
  diarias: number;
  valorDiaria: number;
  valorDiariaStr: string;
  /** Nulo enquanto a locação está em andamento; preenchido no encerramento. */
  valorTotal: number | null;
  valorTotalStr: string | null;
  kmSaida: number;
  categoriaNome: string;
  marca: string;
  modelo: string;
  anoModelo: number;
  cor: string | null;
  placa: string;
  /** Foto de capa do veículo. Nula quando não há foto ou quando o armazenamento falhou. Respostas sem o campo: tratar como nula. */
  fotoCapa?: FotoCapaVeiculo | null;
  /** Texto do servidor quando o armazenamento falhou para este item (com fotoCapa nula). */
  erroFotoCapa?: string | null;
  locadoraNomeFantasia: string;
  locadoraLogradouro: string;
  locadoraNumero: string;
  locadoraBairro: string;
  locadoraCidade: string;
  locadoraUf: string;
  /** Tolerância de atraso da categoria, em minutos; nulo quando a categoria não tem política. */
  toleranciaAtrasoMinutos: number | null;
  /** Nível de combustível na retirada, de 0 a 1 com 2 casas; nulo sem vistoria de entrada. */
  combustivelRetirada: number | null;
  combustivelRetiradaStr: string | null;
}

/** Parâmetros da listagem. */
export interface FiltroLocacoes {
  pagina: number;
  tamanhoPagina: number;
  situacao: SituacaoLocacao;
}

/** Resposta paginada de GET /api/locacoes/minhas. */
export type RespostaLocacoes = Pagina<LocacaoCliente>;
