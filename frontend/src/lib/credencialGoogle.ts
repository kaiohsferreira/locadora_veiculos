/**
 * Credencial do Google guardada só em memória do módulo (N3, RN3.8 e A5).
 *
 * Nunca vai para history.state, URL, localStorage, sessionStorage, cache do TanStack Query
 * nem log. Recarregar a página apaga a credencial, e a tela de vínculo volta para /entrar.
 */

/** Dados guardados entre o login com Google e o vínculo com a conta AutoVia. */
export interface CredencialGuardada {
  /** JWT do Google. Não é registrado em lugar nenhum. */
  credencial: string;
  /** Valor da caixa "Lembrar de mim" no momento do login com Google (RN2.7). */
  lembrar: boolean;
  /** Nome que veio do Google, ou null. */
  nome: string | null;
  /** E-mail verificado do Google. */
  email: string | null;
}

let guardada: CredencialGuardada | null = null;

/** Guarda a credencial, substituindo a anterior, se houver. Copia os dados para não sofrerem mutação de fora. */
export function guardarCredencialGoogle(dados: CredencialGuardada): void {
  guardada = { ...dados };
}

/** Credencial guardada (uma cópia), ou null quando não há nenhuma. */
export function obterCredencialGoogle(): CredencialGuardada | null {
  return guardada === null ? null : { ...guardada };
}

/** Descarta a credencial. Chamada sem efeito quando não há nenhuma. */
export function limparCredencialGoogle(): void {
  guardada = null;
}
