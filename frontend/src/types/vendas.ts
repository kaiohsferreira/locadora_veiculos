/**
 * Tipos da venda de seminovos: detalhe público do veículo, simulação do parcelamento e proposta de compra
 * (contrato proposta-compra v1.2, P6-BE-02). Os nomes e formatos seguem exatamente o contrato.
 */
import type { Pagina } from './api';
import type { FotoCapaVeiculo } from './fotosVeiculo';

/** Situação da proposta (coluna proposta_compra.status). Nesta entrega toda proposta nasce ENVIADA. */
export type StatusPropostaCompra = 'ENVIADA' | 'ACEITA' | 'RECUSADA' | 'CANCELADA';

/** Seminovo à venda (GET /api/veiculos/a-venda/{id}): o mesmo formato de um item do catálogo. Sem placa, chassi nem renavam. */
export interface VeiculoAVenda {
  idVeiculo: number;
  marca: string;
  modelo: string;
  anoFabricacao: number;
  anoModelo: number;
  cor: string | null;
  kmAtual: number;
  categoriaNome: string;
  valorVenda: number;
  /** Valor em reais formatado pelo servidor ("R$ 104.500,00"). A tela mostra este texto. */
  valorVendaStr: string;
  locadoraNomeFantasia: string;
  locadoraCidade: string;
  locadoraUf: string;
  /** Capa do veículo. Nula quando não há foto ou quando o armazenamento falhou. */
  fotoCapa: FotoCapaVeiculo | null;
  /** Texto do servidor quando o armazenamento falhou (com fotoCapa nula). Nulo nas respostas sem erro. */
  erroFotoCapa: string | null;
}

/** Parâmetros da simulação (query de GET /api/veiculos/{id}/simulacao). */
export interface ParametrosSimulacao {
  /** Entrada em reais, maior ou igual a zero e menor que o valor de venda. */
  valorEntrada: number;
  /** Número de parcelas, de 1 a 60. */
  numeroParcelas: number;
}

/** Resultado da simulação. A última parcela pode ser alguns centavos maior que as demais. */
export interface SimulacaoParcelamento {
  idVeiculo: number;
  valorVenda: number;
  valorVendaStr: string;
  valorEntrada: number;
  valorEntradaStr: string;
  valorFinanciado: number;
  valorFinanciadoStr: string;
  numeroParcelas: number;
  valorParcela: number;
  valorParcelaStr: string;
  valorUltimaParcela: number;
  valorUltimaParcelaStr: string;
}

/** Corpo de POST /api/propostas-compra. O cliente vem do token e os valores são recalculados no servidor. */
export interface PropostaCompraRequisicao extends ParametrosSimulacao {
  idVeiculo: number;
}

/** Proposta gravada (201 do POST e 200 do GET /api/propostas-compra/{id}). */
export interface PropostaCompra extends SimulacaoParcelamento {
  idPropostaCompra: number;
  marca: string;
  modelo: string;
  anoModelo: number;
  status: StatusPropostaCompra;
  /** Data e hora do envio, em ISO 8601 (sem fuso: horário do servidor). */
  dataProposta: string;
  /** Data e hora do envio formatadas pelo servidor ("dd/MM/yyyy HH:mm"). A tela mostra este texto. */
  dataPropostaStr: string;
}

/**
 * Filtros do catálogo de seminovos (query de GET /api/veiculos/a-venda). Todos opcionais e combinados com AND; o ano é o
 * do modelo. `pagina` começa em 1 e `tamanhoPagina` é 20 na tela.
 */
export interface FiltroCatalogo {
  pagina: number;
  tamanhoPagina: number;
  marca?: string;
  precoMinimo?: number;
  precoMaximo?: number;
  anoMinimo?: number;
  anoMaximo?: number;
  kmMaximo?: number;
}

/** Página do catálogo: veículos a venda do menor preço para o maior. */
export type RespostaCatalogo = Pagina<VeiculoAVenda>;
