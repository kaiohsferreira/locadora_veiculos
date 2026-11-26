import type { StatusLocacao } from './enums';

/** Subconjunto de GET /api/locacoes/{id}, usado para identificar a locação. */
export interface LocacaoDaVistoria {
  idLocacao: number;
  idReserva: number;
  idCliente: number;
  idVeiculo: number;
  dataRetiradaStr: string;
  dataDevolucaoPrevistaStr: string;
  /** Quilometragem de saída gravada na abertura (igual ao km do veículo naquele momento). */
  kmSaida: number;
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
  /** Nome abreviado de quem registrou a entrada (vistoria-entrada v3, N10); nulo quando o cadastro não tem nome. Opcional para as telas que montam a vistoria sem ele. */
  operadorStr?: string | null;
}

export interface ContextoVistoriaEntrada {
  locacao: LocacaoDaVistoria;
  vistoria: VistoriaEntrada | null;
}
