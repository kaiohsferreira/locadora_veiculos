/**
 * Textos do desenho 16 que foram cortados (locacao.contract.json, tela_16.termos_cortados). Não podem aparecer na tela,
 * em nenhum estado. Usado pelo teste da página, num só lugar.
 */
export const TERMOS_CORTADOS_RETIRADA: readonly string[] = [
  'Posto de Atendimento', 'Balcão 04', 'Terminal CGH', 'Cliente Gold', '1.3 Turbo Flex', 'Pátio Congonhas', 'Box Totem',
  'Retirada e Devolução', 'c/ taxas', 'Garantia', 'Caução', 'Pré-autorizado', 'Mastercard', 'Proteção', 'Franquia Zero',
  'Opcionais', 'Wi-Fi', 'Cadeira de Bebê', 'Titularidade do cartão', 'Cartão físico', 'Consulta de restrições', 'Serpro',
  'Detran', 'Certidão negativa', 'Biometria', 'Score de 98%', 'Match biométrico', 'Pré-autorização', 'NSU', 'AutoVia Pay',
  'supervisor', 'override', 'Salvar Rascunho', 'Suspender Atendimento', 'Validado no balcão', 'Categoria B regularizada',
  'Endereço em São Paulo', 'Aceite digital registrado', '(Ativa)', 'Busca rápida de placa', 'Pátio Operacional',
  'Agência Congonhas', 'Status do Pátio', 'Operando Normal', 'Carlos Mendes',
];

/** Minúsculas e sem acento, para a comparação não depender da caixa nem dos acentos. */
function normalizarParaComparacao(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Termos cortados que aparecem no HTML renderizado. Ignora o atributo `class` e o `src` (não são texto visível).
 * Lista vazia = nenhum corte apareceu.
 */
export function termosCortadosPresentesRetirada(html: string): string[] {
  const visivel = html.replace(/ class="[^"]*"/g, '').replace(/ src="[^"]*"/g, '');
  const alvo = normalizarParaComparacao(visivel);
  return TERMOS_CORTADOS_RETIRADA.filter((termo) => alvo.includes(normalizarParaComparacao(termo)));
}
