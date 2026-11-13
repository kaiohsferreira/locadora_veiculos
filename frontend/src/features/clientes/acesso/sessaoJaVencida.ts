/**
 * Decide se a sessão recém-recebida do login já nasce vencida. Função pura: recebe o
 * relógio como parâmetro. Quando o vencimento já passou, o login não pode parecer
 * sucesso: a sessão não é aberta e o erro vai para a tela.
 */
import type { UsuarioSessao } from '@/types/autenticacao';

/**
 * Verdadeiro quando `usuario.expiraEm` já chegou a `agora` (ou passou). Sem vencimento,
 * ou com data que não se lê, a sessão não vence por tempo (mesma regra de `@/lib/sessao`).
 */
export function sessaoJaVencida(usuario: UsuarioSessao, agora: Date): boolean {
  if (usuario.expiraEm === undefined) return false;
  const vencimento = Date.parse(usuario.expiraEm);
  if (Number.isNaN(vencimento)) return false;
  return vencimento <= agora.getTime();
}
