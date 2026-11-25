import type { FotoCapaVeiculo } from './fotosVeiculo';

/**
 * Veículo da frota (contrato veiculo.contract.json v3: VeiculoVO de entrada e ReturnVeiculoVO de saída).
 * Nomes em português como no contrato. O pedido não tem status nem empresa: o status nasce DISPONIVEL no servidor,
 * e a empresa vem do contexto de quem está logado.
 */

/** Status do veículo (texto no banco). Só leitura no front: o cadastro e a edição não o enviam. */
export const STATUS_VEICULO = [
  'DISPONIVEL',
  'RESERVADO',
  'LOCADO',
  'MANUTENCAO',
  'VENDIDO',
  'BAIXADO',
] as const;
export type StatusVeiculo = (typeof STATUS_VEICULO)[number];

/**
 * Corpo do POST e do PUT (VeiculoVO). Os 15 campos do contrato (veiculo v4, com fornecedor e nota fiscal de compra),
 * sem status e sem empresa. Datas no formato "AAAA-MM-DD"; valores em reais como número com ponto decimal.
 */
export interface VeiculoRequisicao {
  idCategoria: number;
  /** Mercosul (ABC1D23) ou antiga (ABC1234), sem hífen e em maiúsculas. */
  placa: string;
  chassi: string;
  /** Texto, com os zeros à esquerda. */
  renavam: string;
  marca: string;
  modelo: string;
  anoFabricacao: number;
  anoModelo: number;
  /** Texto vazio quando não há cor (o servidor grava nulo). */
  cor: string;
  kmAtual: number;
  valorDiaria: number;
  /** Nulo quando o veículo não tem valor de venda. */
  valorVenda: number | null;
  /** Nulo quando a data não foi informada. */
  dataAquisicao: string | null;
  /** Texto vazio quando não há fornecedor. Dado interno da locadora: não aparece em retorno público. */
  fornecedor: string;
  /** Texto vazio quando não há nota fiscal de compra. Dado interno da locadora. */
  numeroNotaFiscalCompra: string;
}

/**
 * Veículo devolvido pela API (ReturnVeiculoVO): GET por id, retorno do POST (201) e base da edição.
 * Os textos formatados (`valorDiariaStr`, `valorVendaStr`, `dataAquisicaoStr`) vêm do servidor e não são usados na edição.
 */
export interface VeiculoRetorno {
  idVeiculo: number;
  idCategoria: number;
  categoriaNome: string;
  placa: string;
  chassi: string;
  renavam: string;
  marca: string;
  modelo: string;
  anoFabricacao: number;
  anoModelo: number;
  cor: string | null;
  kmAtual: number;
  valorDiaria: number;
  valorDiariaStr: string;
  valorVenda: number | null;
  valorVendaStr: string;
  status: StatusVeiculo;
  dataAquisicao: string | null;
  dataAquisicaoStr: string;
  /** Dado interno (contrato v4): nulo quando não há. Não vem nos retornos públicos. */
  fornecedor: string | null;
  numeroNotaFiscalCompra: string | null;
  locadoraNomeFantasia: string;
  locadoraCidade: string;
  locadoraUf: string;
}

/** Filtros da consulta pública de disponibilidade (P3-BE-01 / RF-11). */
export interface FiltroVeiculosDisponiveis {
  cidade?: string;
  uf?: string;
  dataRetirada: string;
  dataDevolucao: string;
  idCategoria?: number;
  valorDiariaMin?: number;
  valorDiariaMax?: number;
  pagina: number;
  tamanhoPagina: number;
}

/** Veículo elegível para locação no período solicitado, conforme ReturnVeiculoDisponivelVO. */
export interface VeiculoDisponivel {
  idVeiculo: number;
  idCategoria: number;
  categoriaNome: string;
  marca: string;
  modelo: string;
  anoModelo: number;
  cor: string | null;
  valorDiaria: number;
  valorDiariaStr: string;
  locadoraNomeFantasia: string;
  locadoraCidade: string;
  locadoraUf: string;
  fotoCapa?: FotoCapaVeiculo | null;
  erroFotoCapa?: string | null;
}
