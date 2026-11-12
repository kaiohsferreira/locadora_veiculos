import { describe, expect, it } from 'vitest';
import { GOOGLE_VINCULAR_PROVA_EXPIRADA, MENSAGEM_CREDENCIAIS_INVALIDAS } from './mensagensGoogle';

describe('mensagens do login com Google', () => {
  it('MENSAGEM_CREDENCIAIS_INVALIDAS é o texto que a API devolve no 401 de CPF ou senha', () => {
    // Se o texto do backend mudar, o vínculo deixa de distinguir o 401 de senha do 401 da prova.
    expect(MENSAGEM_CREDENCIAIS_INVALIDAS).toBe('CPF ou senha inválidos.');
  });

  it('a prova expirada manda voltar ao login, sem pedir nova tentativa com a mesma prova', () => {
    expect(GOOGLE_VINCULAR_PROVA_EXPIRADA).toBe(
      'A confirmação com o Google expirou. Volte ao login e escolha Continuar com o Google de novo.',
    );
  });
});
