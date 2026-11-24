/**
 * Textos do desenho 16 que foram cortados (locacao.contract.json, tela_16.termos_cortados e termos_cortados_v4), em duas
 * listas. Usadas pelos testes das páginas, num só lugar.
 */

/**
 * Proibidos em TODOS os estados, com ou sem caução. Continuam aqui, entre outros, "Pré-autorizado", "Mastercard", "NSU",
 * "AutoVia Pay" e "Cartão físico": a tela nunca mostra bandeira, final do cartão, NSU nem caução aprovada.
 */
export const TERMOS_CORTADOS_RETIRADA: readonly string[] = [
  'Posto de Atendimento', 'Balcão 04', 'Terminal CGH', 'Cliente Gold', '1.3 Turbo Flex', 'Pátio Congonhas', 'Box Totem',
  'Retirada e Devolução', 'c/ taxas', 'Pré-autorizado', 'Mastercard', 'Proteção', 'Franquia Zero',
  'Opcionais', 'Wi-Fi', 'Cadeira de Bebê', 'Cartão físico', 'Consulta de restrições', 'Serpro',
  'Detran', 'Certidão negativa', 'Biometria', 'Score de 98%', 'Match biométrico', 'NSU', 'AutoVia Pay',
  'supervisor', 'override', 'Salvar Rascunho', 'Suspender Atendimento', 'Validado no balcão', 'Categoria B regularizada',
  'Endereço em São Paulo', 'Aceite digital registrado', '(Ativa)', 'Busca rápida de placa', 'Pátio Operacional',
  'Agência Congonhas', 'Status do Pátio', 'Operando Normal', 'Carlos Mendes',
];

/** Termos da caução: proibidos só quando a caução NÃO é exigida (e nos estados sem dados). */
export const TERMOS_DA_CAUCAO_RETIRADA: readonly string[] = ['Garantia', 'Caução', 'Pré-autorização', 'Titularidade do cartão'];

/** Minúsculas e sem acento, para a comparação não depender da caixa nem dos acentos. */
function normalizarParaComparacao(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * O termo aparece começando uma palavra (antes dele não há letra nem número). Assim "Garantia" acha "garantias", mas
 * "NSU" não acha o meio de "consultados", que é texto aprovado da tela.
 */
function apareceNoInicioDePalavra(alvo: string, termo: string): boolean {
  let posicao = alvo.indexOf(termo);
  while (posicao !== -1) {
    const anterior = posicao === 0 ? '' : alvo.charAt(posicao - 1);
    if (!/[a-z0-9]/.test(anterior)) return true;
    posicao = alvo.indexOf(termo, posicao + 1);
  }
  return false;
}

/** Termos da lista que aparecem no HTML renderizado. Ignora o atributo `class` e o `src` (não são texto visível). */
function termosPresentes(html: string, termos: readonly string[]): string[] {
  const visivel = html.replace(/ class="[^"]*"/g, '').replace(/ src="[^"]*"/g, '');
  const alvo = normalizarParaComparacao(visivel);
  return termos.filter((termo) => apareceNoInicioDePalavra(alvo, normalizarParaComparacao(termo)));
}

/** Termos cortados de sempre que aparecem no HTML. Lista vazia = nenhum corte apareceu. */
export function termosCortadosPresentesRetirada(html: string): string[] {
  return termosPresentes(html, TERMOS_CORTADOS_RETIRADA);
}

/** Termos da caução que aparecem no HTML. Lista vazia = nada da caução apareceu. */
export function termosDaCaucaoPresentesRetirada(html: string): string[] {
  return termosPresentes(html, TERMOS_DA_CAUCAO_RETIRADA);
}
