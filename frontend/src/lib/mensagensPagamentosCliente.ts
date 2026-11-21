/**
 * Textos da tela 30 (Pagamentos e Faturas do cliente, P7-FE-02). Os textos do desenho estão sem mudança, exceto os
 * marcados como "ajustado" (decisão do Kaio, contrato pagamento, grupo C) e os marcados como "novo" (listados para o
 * Kaio antes do commit).
 */
import { formatarNumero } from '@/lib/formatters';
import type { PeriodoPagamentosCliente } from '@/types/pagamentos';

// Cabeçalho ------------------------------------------------------------------

/** Título da página (desenho) e da aba do navegador. */
export const MENSAGEM_PAGAMENTOS_TITULO_PAGINA = 'Pagamentos e Faturas';
/** Subtítulo (ajustado: decisão 5 do grupo C). */
export const MENSAGEM_PAGAMENTOS_DESCRICAO = 'Consulte o extrato detalhado das suas cobranças.';
/** Migalha de pão (desenho). */
export const MENSAGEM_PAGAMENTOS_MIGALHA_INICIO = 'Portal do Cliente';
/** Migalha de pão, item atual (desenho). */
export const MENSAGEM_PAGAMENTOS_MIGALHA_ATUAL = 'Financeiro & Pagamentos';
/** Nome da trilha de navegação para leitores de tela (novo). */
export const MENSAGEM_PAGAMENTOS_NAVEGACAO_CAMINHO = 'Caminho da página';

// Gaveta de PIX (P7-FE-02, contrato pagamento-pix; textos aprovados em 20/11/2026) --------

/** Título da gaveta (desenho, html da gaveta). */
export const MENSAGEM_PIX_TITULO = 'Pagamento Seguro AutoVia';
/** Rótulo do valor no bloco de resumo (desenho). */
export const MENSAGEM_PIX_VALOR_FATURA = 'Valor da Fatura';
/** Vencimento curto no selo do bloco de resumo: "Venc. 22/11". */
export function mensagemPixVencimentoCurto(diaEMes: string): string {
  return `Venc. ${diaEMes}`;
}
/** Estado gerando: enquanto o servidor responde. */
export const MENSAGEM_PIX_GERANDO = 'Gerando seu PIX...';
/** Texto abaixo do QR (desenho). */
export const MENSAGEM_PIX_ESCANEIE = 'Escaneie com o app do seu Banco';
/** Texto antes da contagem regressiva (desenho). */
export const MENSAGEM_PIX_CONTAGEM_ANTES = 'O código expira em';
/** Alternativa textual do QR Code (acessibilidade). */
export const MENSAGEM_PIX_QR_ALT = 'QR Code para pagamento via PIX';
/** Nome acessível do campo somente leitura do copia e cola. Aprovado pelo Kaio em 21/11/2026. */
export const MENSAGEM_PIX_CAMPO_CODIGO = 'Código PIX copia e cola';
/** Botão do copia e cola (desenho). */
export const MENSAGEM_PIX_COPIAR = 'Copiar';
/** Feedback do Copiar, por 2 segundos. */
export const MENSAGEM_PIX_CODIGO_COPIADO = 'Código copiado';
/** Estado expirado: quando a contagem chega a zero. */
export const MENSAGEM_PIX_EXPIROU = 'O código PIX expirou.';
/** Botão do estado expirado: chama a mesma rota, que gera outro PIX. */
export const MENSAGEM_PIX_GERAR_NOVO = 'Gerar novo código';
/** Botão do estado erro. */
export const MENSAGEM_PIX_TENTAR_NOVAMENTE = 'Tentar novamente';
/** Rótulo do botão de fechar a gaveta (mesmo texto da gaveta de funcionário). Aprovado pelo Kaio em 21/11/2026. */
export const MENSAGEM_PIX_FECHAR = 'Fechar';

// Faixa de pendência ------------------------------------------------------------

/** Faixa laranja: "Você possui 1 cobrança pendente." ou "Você possui N cobranças pendentes." (ajustado, decisão 5). */
export function mensagemFaixaPendencias(quantidade: number): string {
  if (quantidade === 1) return 'Você possui 1 cobrança pendente.';
  return `Você possui ${formatarNumero(quantidade)} cobranças pendentes.`;
}

/** Botão da faixa, com o total pendente como veio do servidor. */
export function mensagemBotaoPagarFatura(totalStr: string): string {
  return `Pagar Fatura ${totalStr}`;
}

// Cartões ----------------------------------------------------------------------

export const MENSAGEM_PAGAMENTOS_CARTAO_LIQUIDADO = 'Total Liquidado no Ano';
export const MENSAGEM_PAGAMENTOS_CARTAO_PENDENTES = 'Cobranças Pendentes';

/** Linha de baixo do cartão de liquidado: "N cobranças quitadas" com singular (decisão 9). */
export function mensagemCobrancasQuitadas(quantidade: number): string {
  if (quantidade === 1) return '1 cobrança quitada';
  return `${formatarNumero(quantidade)} cobranças quitadas`;
}

/** Exercício do cartão de liquidado: o ano corrente (decisão 9). */
export function mensagemExercicio(ano: number): string {
  return `Exercício ${ano}`;
}

/** Linha de baixo do cartão de pendentes, ajustada ao número (contrato pagamento). */
export function mensagemFaturasAguardandoRegularizacao(quantidade: number): string {
  if (quantidade === 1) return '1 fatura aguardando regularização';
  return `${formatarNumero(quantidade)} faturas aguardando regularização`;
}

/** Linha de baixo do cartão de pendentes com zero pendente (novo). */
export const MENSAGEM_PAGAMENTOS_SEM_PENDENTES = 'Nenhuma fatura aguardando regularização';

/** Botão do cartão de pendentes (desenho). */
export const MENSAGEM_PAGAMENTOS_REGULARIZAR = 'Regularizar →';

// Abas e filtros ----------------------------------------------------------------

export const MENSAGEM_PAGAMENTOS_NAVEGACAO_ABAS = 'Situação das cobranças';
export const MENSAGEM_PAGAMENTOS_ABA_TODOS = 'Todos';

/** Aba Pendentes com contagem; sem contagem enquanto o resumo não chega. */
export function mensagemAbaPendentes(quantidade: number | null): string {
  return quantidade === null ? 'Pendentes' : `Pendentes (${formatarNumero(quantidade)})`;
}

/** Aba Pagos com contagem; sem contagem enquanto o resumo não chega. */
export function mensagemAbaPagos(quantidade: number | null): string {
  return quantidade === null ? 'Pagos' : `Pagos (${formatarNumero(quantidade)})`;
}

export const MENSAGEM_PAGAMENTOS_PERIODO_ROTULO = 'Período das cobranças';

/** Rótulo de cada período do seletor (desenho; "Ano Atual" leva o ano). */
export function mensagemPeriodo(periodo: PeriodoPagamentosCliente, ano: number): string {
  switch (periodo) {
    case 'DOZE_MESES':
      return 'Últimos 12 meses';
    case 'ANO_ATUAL':
      return `Ano Atual (${ano})`;
    case 'SEIS_MESES':
      return 'Últimos 6 meses';
    case 'TUDO':
      return 'Histórico Completo';
  }
}

// Tabela -------------------------------------------------------------------------

export const MENSAGEM_PAGAMENTOS_LEGENDA_TABELA = 'Cobranças do cliente';
export const MENSAGEM_PAGAMENTOS_CABECALHO_DESCRICAO = 'Descrição & Serviço';
export const MENSAGEM_PAGAMENTOS_CABECALHO_LOCACAO = 'Locação Relacionada';
export const MENSAGEM_PAGAMENTOS_CABECALHO_VENCIMENTO = 'Vencimento';
export const MENSAGEM_PAGAMENTOS_CABECALHO_VALOR = 'Valor Total';
export const MENSAGEM_PAGAMENTOS_CABECALHO_STATUS = 'Status';
export const MENSAGEM_PAGAMENTOS_CABECALHO_ACOES = 'Ações Rápidas';

/** Número da cobrança, embaixo do tipo (ajustado: decisão 12). */
export function mensagemCobrancaNumero(idPagamento: number): string {
  return `Cobrança nº ${idPagamento}`;
}

/** Origem da locação (ajustado: decisão 12). */
export function mensagemLocacaoNumero(idLocacao: number): string {
  return `Locação nº ${idLocacao}`;
}

/** Origem da venda (ajustado: decisão 12). */
export function mensagemVendaNumero(idVenda: number): string {
  return `Venda nº ${idVenda}`;
}

/** Subtexto do vencimento de cobrança pendente. */
export function mensagemVencimento(diasParaVencer: number): string {
  if (diasParaVencer === 0) return 'Vence hoje';
  if (diasParaVencer === 1) return 'Vence amanhã';
  return `Vence em ${formatarNumero(diasParaVencer)} dias`;
}

/** Subtexto do vencimento de cobrança atrasada (dias é positivo). */
export function mensagemVencidaHa(dias: number): string {
  if (dias === 1) return 'Vencida há 1 dia';
  return `Vencida há ${formatarNumero(dias)} dias`;
}

/** Subtexto do vencimento de cobrança paga, com a forma usada. */
export function mensagemPagoVia(rotuloForma: string): string {
  return `Pago via ${rotuloForma}`;
}

/** Botão de pagar da linha (ligado: abre a gaveta do PIX; ver BotaoPagar). */
export const MENSAGEM_PAGAMENTOS_PAGAR_AGORA = 'Pagar agora';

/** Etiqueta do status pago (desenho). */
export const MENSAGEM_PAGAMENTOS_COMPENSADO = 'Compensado';

// Estados ------------------------------------------------------------------------

export const MENSAGEM_PAGAMENTOS_VAZIO_TITULO = 'Você ainda não tem cobranças.';
export const MENSAGEM_PAGAMENTOS_VAZIO_FILTRO_TITULO = 'Nenhuma cobrança neste filtro.';
export const MENSAGEM_PAGAMENTOS_RODAPE_ITENS = 'cobranças';
export const MENSAGEM_PAGAMENTOS_ERRO_RESUMO = 'Não foi possível carregar o resumo das cobranças.';
