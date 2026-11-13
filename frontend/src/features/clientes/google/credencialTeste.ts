/**
 * Credencial do modo de teste (provedor FAKE, só em desenvolvimento, G4). Função pura.
 *
 * O servidor aceita a string `FAKE:<e-mail>` e cria a identidade simulada. Nada disso vai a
 * script externo: é só texto enviado no corpo do login.
 */
import {
  GOOGLE_TESTE_ERRO_EMAIL_INVALIDO,
  GOOGLE_TESTE_ERRO_EMAIL_OBRIGATORIO,
} from '@/lib/mensagens';

const PREFIXO_CREDENCIAL_TESTE = 'FAKE:';

/** Mesma forma simples de e-mail do cadastro: algo@dominio.tld, sem espaços. */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export type ResultadoEmailTeste =
  | { ok: true; credencial: string }
  | { ok: false; mensagem: string };

/** Monta a credencial `FAKE:<e-mail>` a partir do e-mail digitado, ou devolve a mensagem do erro. */
export function montarCredencialTeste(email: string): ResultadoEmailTeste {
  const limpo = email.trim();
  if (limpo === '') return { ok: false, mensagem: GOOGLE_TESTE_ERRO_EMAIL_OBRIGATORIO };
  if (!REGEX_EMAIL.test(limpo)) return { ok: false, mensagem: GOOGLE_TESTE_ERRO_EMAIL_INVALIDO };
  return { ok: true, credencial: `${PREFIXO_CREDENCIAL_TESTE}${limpo}` };
}
