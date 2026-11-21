/**
 * Textos do desenho 09 que foram cortados na versão mínima (contrato painel-financeiro, tabela de blocos). Não podem aparecer
 * na tela. Usado pelo teste da página inteira, num só lugar.
 */
export const TERMOS_CORTADOS_PAINEL_FINANCEIRO: readonly string[] = [
  'Controladoria',
  'Conciliar Bancos',
  'Nova Cobrança Avulsa',
  'Últimos 30 dias',
  '+14.2%',
  'vs mês anterior',
  'Locações: R$',
  'Vendas: R$',
  '/ Locação',
  'Previsão Estável',
  'Dentro do fluxo',
  'com liquidação até fim do mês',
  'Em Atraso Crítico',
  'Disparar Régua',
  'após régua automática',
  'Régua de Cobrança',
  'Venda Seminovos',
  'Média ponderada',
  'Evolução da Receita',
  'Faturado 2º Semestre',
  'Taxa de Liquidação',
  'Contratos em Notificação',
  'Títulos e Cobranças Pendentes',
  'Filtrar Origem',
  'Exportar XLS',
  'Buscar por cliente',
  'Aguardando PIX',
  'Frota PJ',
  'Pessoa Física',
  'Copiar PIX',
  'Notificar cliente',
  'Ver Comprovante',
  'Liquidado hoje',
  'Negativação',
  'Serasa',
  'Serpro',
];

/** Minúsculas e sem acento, para a comparação não depender da caixa nem dos acentos. */
function normalizarParaComparacao(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Termos cortados que aparecem no HTML renderizado. Ignora o atributo `class` (ex.: `rounded-cartao` não é texto) e o `src`.
 * Lista vazia = nenhum corte apareceu.
 */
export function termosCortadosPresentesPainel(html: string): string[] {
  const visivel = html.replace(/ class="[^"]*"/g, '').replace(/ src="[^"]*"/g, '');
  const alvo = normalizarParaComparacao(visivel);
  return TERMOS_CORTADOS_PAINEL_FINANCEIRO.filter((termo) => alvo.includes(normalizarParaComparacao(termo)));
}
