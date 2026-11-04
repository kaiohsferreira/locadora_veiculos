/**
 * Caminhos (padrões de rota) da aplicação: fonte única para o roteador e para os links.
 *
 * Para montar um link com parâmetro, use `generatePath` do React Router, que
 * confere os nomes dos parâmetros pelo próprio padrão:
 *   generatePath(CAMINHOS.detalheVeiculo, { veiculoId: String(veiculo.id) })
 */
export const CAMINHOS = {
  // Área pública e do cliente ------------------------------------------------
  inicio: '/',
  entrar: '/entrar',
  cadastro: '/cadastro',
  cadastroDadosPessoais: '/cadastro/dados-pessoais',
  cadastroDocumentos: '/cadastro/documentos',
  resultadosBusca: '/veiculos',
  detalheVeiculo: '/veiculos/:veiculoId',
  checkoutReserva: '/reservas/checkout/:veiculoId',
  confirmacaoReserva: '/reservas/:reservaId/confirmacao',
  seminovos: '/seminovos',
  propostaCompra: '/seminovos/:veiculoId/proposta',
  minhasReservas: '/minhas-reservas',
  minhasLocacoes: '/minhas-locacoes',
  meusPagamentos: '/meus-pagamentos',

  // Back office ----------------------------------------------------------------
  backOffice: '/back-office',
  frota: '/back-office/frota',
  frotaVeiculos: '/back-office/frota/veiculos',
  frotaNovoVeiculo: '/back-office/frota/veiculos/novo',
  clientes: '/back-office/clientes',
  verificacaoCadastros: '/back-office/clientes/verificacao',
  reservas: '/back-office/reservas',
  locacoes: '/back-office/locacoes',
  retiradaVeiculo: '/back-office/locacoes/retirada/:reservaId',
  vistoriaLocacao: '/back-office/locacoes/:locacaoId/vistoria',
  contratoLocacao: '/back-office/locacoes/:locacaoId/contrato',
  devolucaoLocacao: '/back-office/locacoes/:locacaoId/devolucao',
  vendas: '/back-office/vendas',
  financeiro: '/back-office/financeiro',
  manutencao: '/back-office/manutencao',
  custoResultadoFrota: '/back-office/manutencao/custos-e-resultado',
  seguros: '/back-office/seguros',
  detalheSinistro: '/back-office/seguros/sinistros/:sinistroId',
} as const;

/** Estado enviado para a tela de login, para voltar à página que o usuário tentou abrir. */
export interface EstadoRedirecionamentoLogin {
  de: string;
}

/**
 * Caminho de retorno após o login, lido do `location.state`.
 * Só aceita caminhos internos ("/..."): bloqueia "//site", "/\site" e URLs
 * absolutas, evitando redirecionamento para fora da aplicação.
 */
export function obterCaminhoRetorno(estado: unknown, padrao: string = CAMINHOS.inicio): string {
  if (typeof estado !== 'object' || estado === null) return padrao;
  const de = (estado as Partial<Record<keyof EstadoRedirecionamentoLogin, unknown>>).de;
  if (typeof de !== 'string') return padrao;
  const interno = de.startsWith('/') && !de.startsWith('//') && !de.includes('\\');
  return interno ? de : padrao;
}
