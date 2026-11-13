/**
 * Nome e e-mail do Google para pré-preencher o cadastro (N3, G10), guardados só em memória do módulo.
 *
 * Não vão para `location.state`, URL, storage nem cache do TanStack Query: a tela de cadastro lê
 * uma vez e limpa. Recarregar a página apaga o dado (o cadastro segue com preenchimento manual).
 */

/** Dados do Google usados como valores iniciais do cadastro. A pessoa pode editar tudo. */
export interface PreenchimentoGoogle {
  /** Nome que veio do Google, ou null. */
  nome: string | null;
  /** E-mail verificado do Google. */
  email: string;
}

let guardado: PreenchimentoGoogle | null = null;

/** Guarda o pré-preenchimento, substituindo o anterior. Copia os dados. */
export function guardarPreenchimentoGoogle(dados: PreenchimentoGoogle): void {
  guardado = { ...dados };
}

/** Pré-preenchimento guardado (uma cópia), ou null quando não há nenhum. Não consome. */
export function obterPreenchimentoGoogle(): PreenchimentoGoogle | null {
  return guardado === null ? null : { ...guardado };
}

/** Descarta o pré-preenchimento. Chamada sem efeito quando não há nenhum. */
export function limparPreenchimentoGoogle(): void {
  guardado = null;
}
