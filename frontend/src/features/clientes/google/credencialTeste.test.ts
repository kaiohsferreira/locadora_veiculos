import { describe, expect, it } from 'vitest';
import { GOOGLE_TESTE_ERRO_EMAIL_INVALIDO, GOOGLE_TESTE_ERRO_EMAIL_OBRIGATORIO } from '@/lib/mensagens';
import { montarCredencialTeste } from './credencialTeste';

describe('montarCredencialTeste', () => {
  it('monta FAKE:<e-mail> com o e-mail sem espaços das bordas', () => {
    expect(montarCredencialTeste('  ana@exemplo.com ')).toEqual({ ok: true, credencial: 'FAKE:ana@exemplo.com' });
  });

  it('recusa e-mail vazio ou só com espaços', () => {
    expect(montarCredencialTeste('   ')).toEqual({ ok: false, mensagem: GOOGLE_TESTE_ERRO_EMAIL_OBRIGATORIO });
  });

  it('recusa e-mail sem formato válido', () => {
    expect(montarCredencialTeste('ana@')).toEqual({ ok: false, mensagem: GOOGLE_TESTE_ERRO_EMAIL_INVALIDO });
    expect(montarCredencialTeste('ana exemplo.com')).toEqual({ ok: false, mensagem: GOOGLE_TESTE_ERRO_EMAIL_INVALIDO });
  });
});
