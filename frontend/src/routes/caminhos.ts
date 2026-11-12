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
  recuperarSenha: '/recuperar-senha',
  redefinirSenha: '/redefinir-senha',
  vincularGoogle: '/entrar/vincular-google',
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

/** Tem caractere de controle (abaixo de 0x20 ou 0x7F) ou espaço. Não faz parte de um caminho válido. */
function contemControleOuEspaco(texto: string): boolean {
  for (const caractere of texto) {
    const codigo = caractere.charCodeAt(0);
    if (codigo <= 0x20 || codigo === 0x7f) return true;
  }
  return false;
}

/** Origem usada na checagem. Fora do navegador (testes sem window), usa uma base fixa só para comparar. */
function obterOrigemAplicacao(): string {
  return typeof window !== 'undefined' ? window.location.origin : 'http://localhost';
}

/**
 * Caminho de retorno seguro: começa com "/", não é "//" nem tem "\", não tem controle nem
 * espaço, resolve para a mesma origem da aplicação e não é a própria tela de login.
 */
function ehCaminhoInternoSeguro(caminho: string): boolean {
  if (!caminho.startsWith('/') || caminho.startsWith('//') || caminho.includes('\\')) return false;
  if (contemControleOuEspaco(caminho)) return false;
  const origem = obterOrigemAplicacao();
  try {
    const destino = new URL(caminho, origem);
    if (destino.origin !== origem) return false;
    return destino.pathname !== CAMINHOS.entrar;
  } catch {
    return false;
  }
}

/**
 * Caminho de retorno após o login, lido do `location.state`.
 * Só aceita caminho interno da própria origem; qualquer outra coisa (inclusive a tela de
 * login, para não voltar a ela depois de entrar) cai no padrão.
 */
export function obterCaminhoRetorno(estado: unknown, padrao: string = CAMINHOS.inicio): string {
  if (typeof estado !== 'object' || estado === null) return padrao;
  const de = (estado as Partial<Record<keyof EstadoRedirecionamentoLogin, unknown>>).de;
  if (typeof de !== 'string') return padrao;
  return ehCaminhoInternoSeguro(de) ? de : padrao;
}
