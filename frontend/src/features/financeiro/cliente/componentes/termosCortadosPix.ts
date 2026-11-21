/**
 * Textos que não podem aparecer na gaveta de PIX da tela 30: cortados pela decisão P4 (contrato pagamento-pix). Usado
 * pelos testes de marcação da gaveta inteira (cabeçalho, resumo e todos os estados), em um só lugar.
 */
export const TERMOS_CORTADOS_PIX: readonly string[] = [
  'PIX Instantâneo',
  'Instantâneo',
  'Aprovação imediata',
  'Mastercard',
  '9842',
  'Cartão',
  'Parcelamento',
  'Confirmar Pagamento',
  'Meio de Pagamento',
  'PCI-DSS',
  'Recibo enviado',
  'Até 6x',
];

/** Minúsculas e sem acento, para a comparação não depender de como o texto foi digitado (ex.: "Instantaneo" ou "Instantâneo"). */
function normalizarParaComparacao(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Termos cortados que aparecem no HTML renderizado (sem acento e sem diferença de caixa). Ignora o atributo `class` (ex.:
 * `rounded-cartao` não é o texto "Cartão") e o `src` (a imagem base64 do QR). Lista vazia = nenhum corte.
 */
export function termosCortadosPresentes(html: string): string[] {
  const visivel = html.replace(/ class="[^"]*"/g, '').replace(/ src="[^"]*"/g, '');
  const alvo = normalizarParaComparacao(visivel);
  return TERMOS_CORTADOS_PIX.filter((termo) => alvo.includes(normalizarParaComparacao(termo)));
}
