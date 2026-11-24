/**
 * Painel operacional de hoje (GET /api/painel-operacional/hoje; contrato painel-operacional v2). A página de entrada da
 * retirada usa só `retiradas`; `devolucoes` existe na resposta e não é lido nesta tela.
 */
export type OrigemPainelOperacional = 'RESERVA' | 'LOCACAO';

export interface ItemPainelOperacional {
  origem: OrigemPainelOperacional;
  /** Só na origem RESERVA. */
  idReserva: number | null;
  /** Só na origem LOCACAO. */
  idLocacao: number | null;
  /** Código AV- pronto do servidor (reserva de origem; nulo só em locação gravada sem reserva). */
  codigoReserva: string | null;
  dataHora: string;
  status: string;
  idCliente: number;
  nomeCliente: string;
  idVeiculo: number;
  placa: string;
  marca: string;
  modelo: string;
}

export interface RespostaPainelOperacionalHoje {
  /** Data do dia em yyyy-MM-dd. */
  data: string;
  dataStr: string;
  retiradas: ItemPainelOperacional[];
  devolucoes: ItemPainelOperacional[];
}
