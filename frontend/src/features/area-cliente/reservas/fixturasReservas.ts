import { MENSAGEM_RESERVAS_ABA_HISTORICO, MENSAGEM_RESERVAS_HISTORICO_TITULO } from '@/lib/mensagensReservas';
import type { ReservaCliente } from '@/types/reservas';

/** Dados de exemplo para os testes da tela Minhas reservas. Não são chamadas à API. */
export const RESERVA_ATIVA: ReservaCliente = {
  idReserva: 42,
  codigo: 'AV-00042',
  idVeiculo: 7,
  dataReserva: '2026-11-02T14:22:10',
  dataReservaStr: '02/11/2026 14:22',
  dataRetirada: '2026-11-14',
  dataRetiradaStr: '14/11/2026',
  dataDevolucao: '2026-11-18',
  dataDevolucaoStr: '18/11/2026',
  status: 'ATIVA',
  valorEstimado: 1146.16,
  valorEstimadoStr: 'R$ 1.146,16',
  categoriaNome: 'SUV Compacto Especial',
  marca: 'Jeep',
  modelo: 'Renegade Longitude',
  anoModelo: 2025,
  cor: 'Azul',
  placa: 'BRA2E19',
  locadoraNomeFantasia: 'Locadora Centro',
  locadoraLogradouro: 'Avenida Central',
  locadoraNumero: '100',
  locadoraBairro: 'Centro',
  locadoraCidade: 'São Paulo',
  locadoraUf: 'SP',
};

export const RESERVA_CONFIRMADA: ReservaCliente = {
  ...RESERVA_ATIVA,
  idReserva: 43,
  codigo: 'AV-00043',
  status: 'CONFIRMADA',
};

export const RESERVA_HISTORICO: ReservaCliente = {
  ...RESERVA_ATIVA,
  idReserva: 17,
  codigo: 'AV-00017',
  status: 'CANCELADA',
  dataRetirada: '2026-10-18',
  dataRetiradaStr: '18/10/2026',
  dataDevolucao: '2026-10-21',
  dataDevolucaoStr: '21/10/2026',
  valorEstimado: null,
  valorEstimadoStr: '',
};

/**
 * Termos do desenho 29 que o contrato não sustenta (ver o plano da tela, seção 1). Nenhuma tela deste módulo pode
 * mostrar nenhum deles: o teste negativo procura cada um no HTML renderizado. Por decisão do Kaio de 18/11/2026, a aba
 * "Histórico de Locações" e o título "Histórico de Locações Recentes" do desenho 29 são permitidos (ver
 * `semTextosAprovadosDoDesenho`); a palavra "Locações" continua proibida fora desses dois textos.
 */
export const TERMOS_SEM_LASTRO: readonly string[] = [
  'Gold',
  'Totem',
  'Voucher',
  'Mastercard',
  'NF-e',
  'Tanque',
  'Franquia',
  'Biometria',
  'Proteção Total',
  'Assistência',
  'pts',
  'Tolerância',
  'Km Livre',
  'Faturas',
  'Fatura',
  'Nota fiscal',
  'Alugar novamente',
  'Alugar',
  'Membro desde',
  'CPF',
  'Carteira',
  'Check-in',
  'QR Code',
  'Ar Digital',
  'Lugares',
  'Malas',
  'Pontos',
  'Pagamento',
  'Pix',
  'liquidado',
  'Liquidado',
  'Alterar ou Estender',
  'Pronta para Retirada',
  'Nível',
  'Notas Fiscais',
  'Segurança',
  'Cobertura',
  'Quilometragem',
  'Pátio',
  'Cartão',
  'Suporte',
  'Atendimento',
  'Locações',
];

/**
 * Retira do HTML, antes da conferência de TERMOS_SEM_LASTRO, só os dois textos aprovados do desenho 29. O mais longo
 * sai primeiro, porque "Histórico de Locações" está contido em "Histórico de Locações Recentes".
 */
export function semTextosAprovadosDoDesenho(html: string): string {
  return html.split(MENSAGEM_RESERVAS_HISTORICO_TITULO).join('').split(MENSAGEM_RESERVAS_ABA_HISTORICO).join('');
}
