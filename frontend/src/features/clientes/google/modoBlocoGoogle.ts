/**
 * Escolhe como o bloco do Google aparece na tela de acesso, a partir da configuração pública
 * (GET /api/auth/google/configuracao). Função pura.
 *
 * RN3.1: com `habilitado` falso, sem configuração ou sem Client ID, o bloco não aparece e nenhuma
 * mensagem é mostrada. `modoTeste` verdadeiro usa o bloco de teste só em desenvolvimento; em
 * produção ele é tratado como Google indisponível (o bloco não aparece).
 */
import type { ReturnGoogleConfiguracaoVO } from '@/types/autenticacao';

export type ModoBlocoGoogle =
  | { tipo: 'oculto' }
  | { tipo: 'teste' }
  | { tipo: 'real'; clientId: string };

/**
 * Modo do bloco do Google para a configuração recebida (ou undefined enquanto não chegou).
 * `emDesenvolvimento` vem de `import.meta.env.DEV`: o bloco de teste nunca entra no build de produção.
 */
export function obterModoBlocoGoogle(
  configuracao: ReturnGoogleConfiguracaoVO | undefined,
  emDesenvolvimento: boolean,
): ModoBlocoGoogle {
  if (configuracao === undefined || !configuracao.habilitado) return { tipo: 'oculto' };
  if (configuracao.modoTeste) return emDesenvolvimento ? { tipo: 'teste' } : { tipo: 'oculto' };
  const clientId = configuracao.clientId ?? '';
  if (clientId.trim() === '') return { tipo: 'oculto' };
  return { tipo: 'real', clientId };
}
