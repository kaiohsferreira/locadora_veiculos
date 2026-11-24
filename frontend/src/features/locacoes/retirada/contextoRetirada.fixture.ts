import type { ContextoRetirada } from '@/types/retirada';
/** Dados exclusivos dos testes. Datas sem hora (a reserva só guarda a data) e código no formato real. */
export const contextoRetiradaTeste: ContextoRetirada = {
  idReserva: 8, codigoReserva: 'AV-00008', statusReserva: 'ATIVA', idCliente: 9,
  clienteNome: 'Cliente de teste', clienteCpfMascarado: '123.***.***-09', cnhNumero: '*******1234',
  cnhCategoria: 'B', cnhValidade: '2030-11-18', cnhValidadeStr: '18/11/2030', cnhVencida: false,
  cnhEnviadaEm: '2026-11-10T10:30:00', cnhEnviadaEmStr: '10/11/2026 10:30',
  comprovanteResidenciaEnviadoEm: '2026-11-12T09:15:00', comprovanteResidenciaEnviadoEmStr: '12/11/2026 09:15',
  statusCadastroLocadora: 'APROVADO',
  idVeiculo: 3, categoriaNome: 'SUV Médio', anoModelo: 2024, marca: 'Jeep', modelo: 'Renegade Longitude',
  placa: 'BRA2E19', kmAtual: 12000, statusVeiculo: 'RESERVADO', fotoCapa: null, erroFotoCapa: null,
  dataRetiradaPrevista: '2026-11-18', dataRetiradaPrevistaStr: '18/11/2026',
  dataDevolucaoPrevista: '2026-11-22', dataDevolucaoPrevistaStr: '22/11/2026', diariasPrevistas: 4,
  valorDiaria: 120, valorDiariaStr: 'R$ 120,00', valorEstimado: 480, valorEstimadoStr: 'R$ 480,00',
  localRetiradaNome: 'AutoVia Congonhas', localRetiradaEndereco: 'Rua Exemplo, 100 - Centro, São Paulo - SP',
  idLocacaoExistente: null,
};
