import type { LocacaoCliente, RespostaLocacoes } from '@/types/locacoesCliente';

/**
 * Dados de exemplo da tela Minhas locações, para os testes. Nenhum teste chama a API: estes objetos entram pelos
 * dublês dos hooks. A devolução de exemplo fica no futuro distante, para o cronômetro estar "no prazo".
 */

const BASE: LocacaoCliente = {
  idLocacao: 12,
  codigoReserva: 'AV-00042',
  status: 'EM_ANDAMENTO',
  atrasada: false,
  dataRetirada: '2026-11-14T10:00:00',
  dataRetiradaStr: '14/11/2026 10:00',
  dataDevolucaoPrevista: '2099-12-31T10:00:00',
  dataDevolucaoPrevistaStr: '31/12/2099 10:00',
  devolucaoPrevistaEfetiva: '2099-12-31T10:00:00',
  devolucaoPrevistaEfetivaStr: '31/12/2099 10:00',
  dataDevolucaoEfetiva: null,
  dataDevolucaoEfetivaStr: null,
  diarias: 4,
  valorDiaria: 195,
  valorDiariaStr: 'R$ 195,00',
  valorTotal: null,
  valorTotalStr: null,
  kmSaida: 24850,
  categoriaNome: 'SUV Compacto',
  marca: 'Jeep',
  modelo: 'Renegade Longitude',
  anoModelo: 2024,
  cor: 'Cinza',
  placa: 'BRA2E19',
  fotoCapa: null,
  erroFotoCapa: null,
  locadoraNomeFantasia: 'Locadora Centro',
  locadoraLogradouro: 'Avenida Central',
  locadoraNumero: '100',
  locadoraBairro: 'Centro',
  locadoraCidade: 'Belo Horizonte',
  locadoraUf: 'MG',
  toleranciaAtrasoMinutos: 59,
  combustivelRetirada: 0.5,
  combustivelRetiradaStr: '0,50',
};

/** Locação em andamento, no prazo, com tolerância e tanque pela metade na retirada. */
export const LOCACAO_ATIVA: LocacaoCliente = { ...BASE };

/** Locação com status ATRASADA: o servidor já marcou como atrasada. */
export const LOCACAO_ATRASADA: LocacaoCliente = { ...BASE, idLocacao: 13, status: 'ATRASADA', atrasada: true };

/** Locação finalizada, do histórico, com valor total. */
export const LOCACAO_FINALIZADA: LocacaoCliente = {
  ...BASE,
  idLocacao: 9,
  codigoReserva: null,
  status: 'FINALIZADA',
  atrasada: false,
  dataDevolucaoEfetiva: '2026-11-18T09:40:00',
  dataDevolucaoEfetivaStr: '18/11/2026 09:40',
  devolucaoPrevistaEfetiva: '2026-11-18T10:00:00',
  devolucaoPrevistaEfetivaStr: '18/11/2026 10:00',
  dataDevolucaoPrevista: '2026-11-18T10:00:00',
  dataDevolucaoPrevistaStr: '18/11/2026 10:00',
  valorTotal: 780,
  valorTotalStr: 'R$ 780,00',
  diarias: 4,
  fotoCapa: null,
};

/** Locação cancelada, do histórico: sem valor total e sem devolução efetiva. */
export const LOCACAO_CANCELADA: LocacaoCliente = {
  ...BASE,
  idLocacao: 8,
  status: 'CANCELADA',
  atrasada: false,
  dataDevolucaoEfetiva: null,
  dataDevolucaoEfetivaStr: null,
  valorTotal: null,
  valorTotalStr: null,
};

export function respostaComItens(itens: readonly LocacaoCliente[], total: number): RespostaLocacoes {
  return { itens: [...itens], total, pagina: 1, tamanhoPagina: 5 };
}
