/**
 * Textos da tela Painel financeiro (P7-FE-01, tela 09 em versão mínima, desenho 09_back_office_financeiro_e_cobran_as).
 * Reexportados por `@/lib/mensagens`.
 *
 * Os textos vêm da lista "textos_novos" do contrato painel-financeiro (aprovados pelo Kaio em 20/11/2026). Os que não
 * estão nessa lista estão marcados como "fora do aprovado" e foram listados no retorno da entrega, exceto os aprovados pelo
 * Kaio em 21/11/2026, que trazem a data no próprio comentário. Os números entram já
 * formatados pelo chamador; os valores em reais e as datas vêm do servidor nos campos Str.
 */

/** Substitui {0}, {1}... pelos valores, na ordem. */
function preencher(modelo: string, ...valores: string[]): string {
  return modelo.replace(/\{(\d+)\}/g, (marca, indice: string) => valores[Number(indice)] ?? marca);
}

// Cabeçalho ------------------------------------------------------------------

/** Título do documento (aba do navegador): o mesmo título da página. */
export const MENSAGEM_FINANCEIRO_TITULO_DOCUMENTO = 'Dashboard & Contas a Receber';
/** Rótulo pequeno acima do título (cabeçalho do desenho). */
export const MENSAGEM_FINANCEIRO_ROTULO = 'Gestão Financeira';
export const MENSAGEM_FINANCEIRO_TITULO = 'Dashboard & Contas a Receber';
export const MENSAGEM_FINANCEIRO_SUBTITULO = 'Recebido, a receber, em atraso e ticket médio das cobranças da locadora.';

/** Período usado pelos números e pela tabela (datas já formatadas pelo servidor). */
export function mensagemFinanceiroPeriodo(deStr: string, ateStr: string): string {
  return preencher('Período: {0} a {1}', deStr, ateStr);
}

// Cartões --------------------------------------------------------------------

export const MENSAGEM_FINANCEIRO_CARTAO_RECEBIDO = 'Recebido no Mês';
export const MENSAGEM_FINANCEIRO_CARTAO_A_RECEBER = 'A receber no mês';
export const MENSAGEM_FINANCEIRO_CARTAO_EM_ATRASO = 'Em atraso';
export const MENSAGEM_FINANCEIRO_CARTAO_TICKET = 'Ticket médio';
export const MENSAGEM_FINANCEIRO_LINHA_TICKET = 'por cobrança paga';

/** "1 pagamento recebido" ou "2 pagamentos recebidos". `quantidadeTexto` já vem formatado. */
export function mensagemFinanceiroLinhaRecebido(quantidade: number, quantidadeTexto: string): string {
  return quantidade === 1
    ? `${quantidadeTexto} pagamento recebido`
    : `${quantidadeTexto} pagamentos recebidos`;
}

/** "1 título" ou "142 títulos". */
export function mensagemFinanceiroLinhaAReceber(quantidade: number, quantidadeTexto: string): string {
  return quantidade === 1 ? `${quantidadeTexto} título` : `${quantidadeTexto} títulos`;
}

/** "1 título vencido" ou "24 títulos vencidos". */
export function mensagemFinanceiroLinhaEmAtraso(quantidade: number, quantidadeTexto: string): string {
  return quantidade === 1 ? `${quantidadeTexto} título vencido` : `${quantidadeTexto} títulos vencidos`;
}

// Abas -----------------------------------------------------------------------

export const MENSAGEM_FINANCEIRO_ABA_TODOS = 'Todos os Títulos';
export const MENSAGEM_FINANCEIRO_ABA_EM_ATRASO = 'Em Atraso';
export const MENSAGEM_FINANCEIRO_ABA_VENCE_HOJE = 'A Vencer Hoje';
export const MENSAGEM_FINANCEIRO_ABA_PAGOS = 'Pagos Recentemente';

/** Rótulo da aba com o contador entre parênteses, ex.: "Em Atraso (24)". Sem contador (ainda carregando), só o rótulo. */
export function mensagemFinanceiroAbaComContador(rotulo: string, quantidadeTexto: string | undefined): string {
  return quantidadeTexto === undefined ? rotulo : preencher('{0} ({1})', rotulo, quantidadeTexto);
}

// Tabela ---------------------------------------------------------------------

export const MENSAGEM_FINANCEIRO_COLUNA_ORIGEM = 'Origem / Contrato';
export const MENSAGEM_FINANCEIRO_COLUNA_CLIENTE = 'Cliente / Devedor';
export const MENSAGEM_FINANCEIRO_COLUNA_TIPO = 'Tipo do Lançamento';
export const MENSAGEM_FINANCEIRO_COLUNA_VALOR = 'Valor Bruto';
export const MENSAGEM_FINANCEIRO_COLUNA_VENCIMENTO = 'Vencimento';
export const MENSAGEM_FINANCEIRO_COLUNA_STATUS = 'Status Operacional';
export const MENSAGEM_FINANCEIRO_COLUNA_ACAO = 'Ação Direta';
/** Descrição da tabela para leitores de tela. Aprovado pelo Kaio em 21/11/2026; não aparece na tela. */
export const MENSAGEM_FINANCEIRO_LEGENDA_TABELA = 'Títulos financeiros';
export const MENSAGEM_FINANCEIRO_TABELA_VAZIA = 'Nenhum título neste filtro.';
/** Rótulo de carregamento para leitores de tela (padrão do projeto: "Carregando..."). */
export const MENSAGEM_FINANCEIRO_CARREGANDO = 'Carregando...';

export function mensagemFinanceiroCpf(cpfMascarado: string): string {
  return preencher('CPF: {0}', cpfMascarado);
}

export function mensagemFinanceiroPagoEm(dataStr: string): string {
  return preencher('Pago em {0}', dataStr);
}

export const MENSAGEM_FINANCEIRO_VENCE_HOJE = 'Vence hoje';
export const MENSAGEM_FINANCEIRO_VENCE_AMANHA = 'Vence amanhã';
export function mensagemFinanceiroVenceEm(diasTexto: string): string {
  return preencher('Vence em {0} dias', diasTexto);
}
export function mensagemFinanceiroVencidoHa(diasTexto: string): string {
  return preencher('Vencido há {0} dias', diasTexto);
}
export const MENSAGEM_FINANCEIRO_VENCIDO_UM_DIA = 'Vencido há 1 dia';

export const MENSAGEM_FINANCEIRO_STATUS_ATRASADO = 'Em Atraso';
export const MENSAGEM_FINANCEIRO_STATUS_PENDENTE = 'Pendente';
export const MENSAGEM_FINANCEIRO_STATUS_PAGO = 'Compensado / Pago';
export const MENSAGEM_FINANCEIRO_ACAO_DAR_BAIXA = 'Dar baixa';

// Diálogo Dar baixa ----------------------------------------------------------

export const MENSAGEM_FINANCEIRO_DIALOGO_TITULO = 'Dar baixa no pagamento';
export const MENSAGEM_FINANCEIRO_DIALOGO_FORMA = 'Forma de pagamento';
export const MENSAGEM_FINANCEIRO_DIALOGO_CONFIRMAR = 'Confirmar baixa';
/** Mesmo texto usado pelos outros módulos (fora do aprovado como texto novo: é o botão padrão de cancelar). */
export const MENSAGEM_FINANCEIRO_BOTAO_CANCELAR = 'Cancelar';
export const MENSAGEM_FINANCEIRO_CAIXA_PIX = 'Confirmo a baixa mesmo com o PIX gerado.';
/** Aprovado pelo Kaio em 21/11/2026: aparece sob o campo quando a forma não é escolhida. */
export const MENSAGEM_FINANCEIRO_ERRO_FORMA = 'Escolha a forma de pagamento.';
/** Aprovado pelo Kaio em 21/11/2026: aviso de sucesso depois da baixa. */
export const MENSAGEM_FINANCEIRO_SUCESSO_BAIXA = 'Baixa registrada.';
/**
 * Separador entre origem, cliente e valor no resumo da cobrança do diálogo (o mesmo " · " de "placa · modelo" na tela 14,
 * com um espaço de cada lado). Constante própria do financeiro, com o mesmo valor.
 */
export const MENSAGEM_FINANCEIRO_SEPARADOR_RESUMO = ' · ';
/**
 * Texto que o servidor manda no 409 da baixa com PIX gerado (ErroPagamentoBaixaComPixGerado). O ProblemDetails não tem
 * código de erro, então o front reconhece a situação por status 409 e por este texto. Não é mostrado como texto próprio:
 * a tela sempre mostra a mensagem do servidor.
 */
export const MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO =
  'Esta cobrança tem um PIX gerado. Dê baixa pela forma PIX ou confirme a baixa por outra forma.';
