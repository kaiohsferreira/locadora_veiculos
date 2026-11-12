/**
 * Pré-preenchimento do cadastro vindo do login com Google (N3, G10). Função pura.
 *
 * Quando o login com Google devolve CADASTRO_NECESSARIO, o nome e o e-mail ficam na memória do
 * módulo `@/lib/preenchimentoGoogle` (nunca em `location.state`). Aqui só se valida esse valor:
 * só strings, cortadas no limite do cadastro, e o resto é ignorado. Os valores viram iniciais do
 * formulário, que o usuário pode editar. A conta Google sozinha nunca cria cliente: o POST do
 * cadastro continua exigindo tudo.
 */

/** Mesmo limite de nome e e-mail do cadastro (LIMITES_CADASTRO em esquemaCadastro.ts). */
export const LIMITE_PREENCHIMENTO_CADASTRO = 120;

export interface PreenchimentoCadastro {
  nome: string;
  email: string;
}

/** Texto do campo, cortado no limite. Origem que não é objeto, campo ausente ou não-texto viram vazio. */
function lerTexto(origem: unknown, campo: string): string {
  if (typeof origem !== 'object' || origem === null || !(campo in origem)) return '';
  const valor: unknown = (origem as Record<string, unknown>)[campo];
  return typeof valor === 'string' ? valor.slice(0, LIMITE_PREENCHIMENTO_CADASTRO) : '';
}

/**
 * Nome e e-mail vindos da memória do login com Google. Valor ausente, nulo ou de outro tipo vira
 * vazio; campos extras são ignorados.
 */
export function lerPreenchimentoCadastro(valor: unknown): PreenchimentoCadastro {
  return {
    nome: lerTexto(valor, 'nome'),
    email: lerTexto(valor, 'email'),
  };
}
