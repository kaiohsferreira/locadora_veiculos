import { describe, expect, it } from 'vitest';
import type { UsuarioSessao } from '@/types/autenticacao';
import { sessaoJaVencida } from './sessaoJaVencida';

const AGORA = new Date('2026-11-12T13:00:00.000Z');

function usuarioComExpiracao(expiraEm: string | undefined): UsuarioSessao {
  return { nome: 'Ana', perfil: 'cliente', expiraEm };
}

describe('sessaoJaVencida', () => {
  it('é verdadeira quando o vencimento já passou', () => {
    expect(sessaoJaVencida(usuarioComExpiracao('2026-11-12T12:00:00.000Z'), AGORA)).toBe(true);
  });

  it('é verdadeira quando o vencimento é exatamente agora', () => {
    expect(sessaoJaVencida(usuarioComExpiracao('2026-11-12T13:00:00.000Z'), AGORA)).toBe(true);
  });

  it('é falsa quando o vencimento ainda não chegou', () => {
    expect(sessaoJaVencida(usuarioComExpiracao('2026-11-12T13:00:01.000Z'), AGORA)).toBe(false);
  });

  it('é falsa sem vencimento (a sessão não vence por tempo)', () => {
    expect(sessaoJaVencida(usuarioComExpiracao(undefined), AGORA)).toBe(false);
  });

  it('é falsa quando a data não se lê (mesma regra da sessão)', () => {
    expect(sessaoJaVencida(usuarioComExpiracao('não é data'), AGORA)).toBe(false);
  });

  it('usa o relógio recebido, não o do sistema', () => {
    const vencimento = usuarioComExpiracao('2026-11-12T13:00:00.000Z');
    expect(sessaoJaVencida(vencimento, new Date('2026-11-12T12:59:59.000Z'))).toBe(false);
    expect(sessaoJaVencida(vencimento, new Date('2026-11-12T13:00:00.000Z'))).toBe(true);
  });
});
