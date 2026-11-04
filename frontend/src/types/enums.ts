/**
 * Valores fixos do banco, espelhados como uniões de string.
 * Não acrescente valores que não existam no backend.
 *
 * Para cada domínio:
 * - `XXX`: lista de valores (útil para montar opções de Select);
 * - `Xxx`: tipo união derivado da lista;
 * - `ROTULO_XXX`: rótulo em português de cada valor.
 */

/** Opção com valor do domínio e rótulo em português. */
export interface OpcaoRotulada<T extends string> {
  valor: T;
  rotulo: string;
}

/** Monta as opções (valor + rótulo) de um domínio, na ordem da lista. */
export function criarOpcoes<T extends string>(
  valores: readonly T[],
  rotulos: Readonly<Record<T, string>>,
): OpcaoRotulada<T>[] {
  return valores.map((valor) => ({ valor, rotulo: rotulos[valor] }));
}

// Clientes -------------------------------------------------------------------

export const STATUS_CADASTRO_CLIENTE = ['PENDENTE', 'APROVADO', 'RECUSADO'] as const;
export type StatusCadastroCliente = (typeof STATUS_CADASTRO_CLIENTE)[number];
export const ROTULO_STATUS_CADASTRO_CLIENTE: Readonly<Record<StatusCadastroCliente, string>> = {
  PENDENTE: 'Pendente',
  APROVADO: 'Aprovado',
  RECUSADO: 'Recusado',
};

export const CATEGORIA_CNH = ['A', 'B', 'AB', 'C', 'D', 'E'] as const;
export type CategoriaCnh = (typeof CATEGORIA_CNH)[number];
export const ROTULO_CATEGORIA_CNH: Readonly<Record<CategoriaCnh, string>> = {
  A: 'A',
  B: 'B',
  AB: 'AB',
  C: 'C',
  D: 'D',
  E: 'E',
};

// Funcionários ---------------------------------------------------------------

export const CARGO_FUNCIONARIO = [
  'ATENDENTE',
  'VENDEDOR',
  'GESTOR_FROTA',
  'FINANCEIRO',
  'ADMINISTRATIVO',
] as const;
export type CargoFuncionario = (typeof CARGO_FUNCIONARIO)[number];
export const ROTULO_CARGO_FUNCIONARIO: Readonly<Record<CargoFuncionario, string>> = {
  ATENDENTE: 'Atendente',
  VENDEDOR: 'Vendedor',
  GESTOR_FROTA: 'Gestor de frota',
  FINANCEIRO: 'Financeiro',
  ADMINISTRATIVO: 'Administrativo',
};

// Frota ----------------------------------------------------------------------

export const STATUS_VEICULO = [
  'DISPONIVEL',
  'RESERVADO',
  'LOCADO',
  'MANUTENCAO',
  'VENDIDO',
  'BAIXADO',
] as const;
export type StatusVeiculo = (typeof STATUS_VEICULO)[number];
export const ROTULO_STATUS_VEICULO: Readonly<Record<StatusVeiculo, string>> = {
  DISPONIVEL: 'Disponível',
  RESERVADO: 'Reservado',
  LOCADO: 'Locado',
  MANUTENCAO: 'Em manutenção',
  VENDIDO: 'Vendido',
  BAIXADO: 'Baixado',
};

// Reservas -------------------------------------------------------------------

export const STATUS_RESERVA = ['ATIVA', 'CONFIRMADA', 'CANCELADA', 'CONCLUIDA'] as const;
export type StatusReserva = (typeof STATUS_RESERVA)[number];
export const ROTULO_STATUS_RESERVA: Readonly<Record<StatusReserva, string>> = {
  ATIVA: 'Ativa',
  CONFIRMADA: 'Confirmada',
  CANCELADA: 'Cancelada',
  CONCLUIDA: 'Concluída',
};

// Locações e vistorias -------------------------------------------------------

export const STATUS_LOCACAO = ['EM_ANDAMENTO', 'FINALIZADA', 'ATRASADA', 'CANCELADA'] as const;
export type StatusLocacao = (typeof STATUS_LOCACAO)[number];
export const ROTULO_STATUS_LOCACAO: Readonly<Record<StatusLocacao, string>> = {
  EM_ANDAMENTO: 'Em andamento',
  FINALIZADA: 'Finalizada',
  ATRASADA: 'Atrasada',
  CANCELADA: 'Cancelada',
};

export const TIPO_VISTORIA = ['ENTRADA', 'SAIDA'] as const;
export type TipoVistoria = (typeof TIPO_VISTORIA)[number];
export const ROTULO_TIPO_VISTORIA: Readonly<Record<TipoVistoria, string>> = {
  ENTRADA: 'Entrada',
  SAIDA: 'Saída',
};

export const GRAVIDADE_AVARIA = ['LEVE', 'MEDIA', 'GRAVE'] as const;
export type GravidadeAvaria = (typeof GRAVIDADE_AVARIA)[number];
export const ROTULO_GRAVIDADE_AVARIA: Readonly<Record<GravidadeAvaria, string>> = {
  LEVE: 'Leve',
  MEDIA: 'Média',
  GRAVE: 'Grave',
};

export const TIPO_COBRANCA_EXTRA = [
  'MULTA_TRANSITO',
  'KM_EXCEDENTE',
  'ATRASO',
  'AVARIA',
  'COMBUSTIVEL',
  'LIMPEZA',
] as const;
export type TipoCobrancaExtra = (typeof TIPO_COBRANCA_EXTRA)[number];
export const ROTULO_TIPO_COBRANCA_EXTRA: Readonly<Record<TipoCobrancaExtra, string>> = {
  MULTA_TRANSITO: 'Multa de trânsito',
  KM_EXCEDENTE: 'Quilometragem excedente',
  ATRASO: 'Atraso',
  AVARIA: 'Avaria',
  COMBUSTIVEL: 'Combustível',
  LIMPEZA: 'Limpeza',
};

// Vendas ---------------------------------------------------------------------

export const FORMA_PAGAMENTO_VENDA = ['A_VISTA', 'FINANCIAMENTO', 'CONSORCIO'] as const;
export type FormaPagamentoVenda = (typeof FORMA_PAGAMENTO_VENDA)[number];
export const ROTULO_FORMA_PAGAMENTO_VENDA: Readonly<Record<FormaPagamentoVenda, string>> = {
  A_VISTA: 'À vista',
  FINANCIAMENTO: 'Financiamento',
  CONSORCIO: 'Consórcio',
};

export const STATUS_DOCUMENTACAO_VENDA = ['PENDENTE', 'EM_TRANSFERENCIA', 'CONCLUIDA'] as const;
export type StatusDocumentacaoVenda = (typeof STATUS_DOCUMENTACAO_VENDA)[number];
export const ROTULO_STATUS_DOCUMENTACAO_VENDA: Readonly<Record<StatusDocumentacaoVenda, string>> = {
  PENDENTE: 'Pendente',
  EM_TRANSFERENCIA: 'Em transferência',
  CONCLUIDA: 'Concluída',
};

// Financeiro -----------------------------------------------------------------

export const TIPO_PAGAMENTO = ['CAUCAO', 'DIARIA', 'COBRANCA_EXTRA', 'VENDA'] as const;
export type TipoPagamento = (typeof TIPO_PAGAMENTO)[number];
export const ROTULO_TIPO_PAGAMENTO: Readonly<Record<TipoPagamento, string>> = {
  CAUCAO: 'Caução',
  DIARIA: 'Diária',
  COBRANCA_EXTRA: 'Cobrança extra',
  VENDA: 'Venda',
};

export const STATUS_PAGAMENTO = ['PENDENTE', 'PAGO', 'ATRASADO', 'CANCELADO'] as const;
export type StatusPagamento = (typeof STATUS_PAGAMENTO)[number];
export const ROTULO_STATUS_PAGAMENTO: Readonly<Record<StatusPagamento, string>> = {
  PENDENTE: 'Pendente',
  PAGO: 'Pago',
  ATRASADO: 'Atrasado',
  CANCELADO: 'Cancelado',
};

// Manutenção -----------------------------------------------------------------

export const TIPO_MANUTENCAO = ['PREVENTIVA', 'CORRETIVA'] as const;
export type TipoManutencao = (typeof TIPO_MANUTENCAO)[number];
export const ROTULO_TIPO_MANUTENCAO: Readonly<Record<TipoManutencao, string>> = {
  PREVENTIVA: 'Preventiva',
  CORRETIVA: 'Corretiva',
};

// Seguros --------------------------------------------------------------------

export const TIPO_SINISTRO = [
  'COLISAO',
  'ROUBO',
  'FURTO',
  'INCENDIO',
  'FENOMENO_NATURAL',
  'TERCEIROS',
] as const;
export type TipoSinistro = (typeof TIPO_SINISTRO)[number];
export const ROTULO_TIPO_SINISTRO: Readonly<Record<TipoSinistro, string>> = {
  COLISAO: 'Colisão',
  ROUBO: 'Roubo',
  FURTO: 'Furto',
  INCENDIO: 'Incêndio',
  FENOMENO_NATURAL: 'Fenômeno natural',
  TERCEIROS: 'Terceiros',
};

export const STATUS_SINISTRO = ['ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO'] as const;
export type StatusSinistro = (typeof STATUS_SINISTRO)[number];
export const ROTULO_STATUS_SINISTRO: Readonly<Record<StatusSinistro, string>> = {
  ABERTO: 'Aberto',
  EM_ANALISE: 'Em análise',
  APROVADO: 'Aprovado',
  NEGADO: 'Negado',
  ENCERRADO: 'Encerrado',
};

// Status (todos os domínios) -------------------------------------------------

/** Qualquer valor de status do sistema. É o que o componente Etiqueta recebe. */
export type StatusDominio =
  | StatusCadastroCliente
  | StatusVeiculo
  | StatusReserva
  | StatusLocacao
  | StatusDocumentacaoVenda
  | StatusPagamento
  | StatusSinistro;

/**
 * Rótulo de qualquer status. Valores repetidos entre domínios (ex.: PENDENTE,
 * CONCLUIDA, APROVADO) têm o mesmo rótulo em todos eles.
 */
export const ROTULO_STATUS: Readonly<Record<StatusDominio, string>> = {
  ...ROTULO_STATUS_CADASTRO_CLIENTE,
  ...ROTULO_STATUS_VEICULO,
  ...ROTULO_STATUS_RESERVA,
  ...ROTULO_STATUS_LOCACAO,
  ...ROTULO_STATUS_DOCUMENTACAO_VENDA,
  ...ROTULO_STATUS_PAGAMENTO,
  ...ROTULO_STATUS_SINISTRO,
};
