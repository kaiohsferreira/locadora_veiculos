import type { StatusLocacao } from './enums';

/** Subconjunto de GET /api/locacoes/{id}, usado para identificar a locação. */
export interface LocacaoDaVistoria {
  idLocacao: number;
  idReserva: number;
  idCliente: number;
  idVeiculo: number;
  dataRetiradaStr: string;
  dataDevolucaoPrevistaStr: string;
  status: StatusLocacao;
}

export interface RegistrarVistoriaEntrada {
  kmRegistrado: number;
  nivelCombustivel: number;
  observacoes: string | null;
}

/** Funcionário, tipo e data definidos pelo servidor; combustível legado pode ser nulo. */
export interface VistoriaEntrada {
  idVistoria: number;
  idLocacao: number;
  idFuncionario: number;
  tipo: 'ENTRADA';
  dataHoraStr: string;
  kmRegistrado: number;
  nivelCombustivel: number | null;
  observacoes: string | null;
}

export interface ContextoVistoriaEntrada {
  locacao: LocacaoDaVistoria;
  vistoria: VistoriaEntrada | null;
}
