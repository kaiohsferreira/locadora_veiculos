import type { CategoriaCnh, StatusCadastroCliente, StatusLocacao, StatusReserva, StatusVeiculo } from './enums';
export interface ContextoRetirada {
  idReserva: number; codigoReserva: string; statusReserva: StatusReserva;
  idCliente: number; clienteNome: string; /** CPF já mascarado pelo servidor (ex.: 123.***.***-09). */ clienteCpfMascarado: string;
  cnhNumero: string; cnhCategoria: CategoriaCnh; cnhValidadeStr: string;
  statusCadastroLocadora: StatusCadastroCliente | null;
  idVeiculo: number; marca: string; modelo: string; placa: string; kmAtual: number; statusVeiculo: StatusVeiculo;
  dataRetiradaPrevistaStr: string; dataDevolucaoPrevistaStr: string; valorDiariaStr: string; valorEstimadoStr: string;
  idLocacaoExistente: number | null;
}
export interface AbrirLocacao { idReserva: number; }
/** Subconjunto usado de ReturnLocacaoVO. */
export interface LocacaoAberta { idLocacao: number; idReserva: number; kmSaida: number; status: StatusLocacao; }
