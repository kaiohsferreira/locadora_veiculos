import type { ContextoRetirada } from '@/types/retirada';
/** Dados exclusivos dos testes. */
export const contextoRetiradaTeste: ContextoRetirada = {
  idReserva: 8, codigoReserva: 'RES-8', statusReserva: 'ATIVA', idCliente: 9,
  clienteNome: 'Cliente de teste', clienteCpfStr: '***.***.***-09', cnhNumero: '*******1234',
  cnhCategoria: 'B', cnhValidadeStr: '18/11/2030', statusCadastroLocadora: 'APROVADO',
  idVeiculo: 3, marca: 'Marca', modelo: 'Modelo', placa: 'ABC1D23', kmAtual: 12000,
  statusVeiculo: 'RESERVADO', dataRetiradaPrevistaStr: '18/11/2026 09:00',
  dataDevolucaoPrevistaStr: '20/11/2026 09:00', valorDiariaStr: 'R$ 120,00',
  valorEstimadoStr: 'R$ 240,00', idLocacaoExistente: null,
};
