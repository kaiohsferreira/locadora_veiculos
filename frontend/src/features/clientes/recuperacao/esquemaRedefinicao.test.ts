import { describe, expect, it } from 'vitest';
import {
  mensagemRedefinicaoSenhaTamanho,
  REDEFINICAO_CONFIRMACAO_DIFERENTE,
  REDEFINICAO_CONFIRMACAO_OBRIGATORIA,
  REDEFINICAO_SENHA_FRACA,
  REDEFINICAO_SENHA_OBRIGATORIA,
} from '@/lib/mensagens';
import { esquemaRedefinicao } from './esquemaRedefinicao';

const TAMANHO_TEXTO = mensagemRedefinicaoSenhaTamanho(8, 64);

/** Valores válidos; cada teste altera só o que interessa. */
function com(novaSenha: string, confirmacaoSenha: string = novaSenha) {
  return { novaSenha, confirmacaoSenha };
}

/** Todas as mensagens de um campo, na ordem em que o Zod as produziu. */
function mensagensDoCampo(valores: unknown, campo: string): string[] {
  const resultado = esquemaRedefinicao.safeParse(valores);
  if (resultado.success) return [];
  return resultado.error.issues.filter((issue) => issue.path[0] === campo).map((issue) => issue.message);
}

/** Primeira mensagem do campo, ou undefined quando o campo passou. */
function primeiraMensagem(valores: unknown, campo: string): string | undefined {
  return mensagensDoCampo(valores, campo)[0];
}

describe('textos da redefinição (iguais ao ConstantsMessage.RecuperacaoSenha.cs)', () => {
  it('batem com os textos do backend', () => {
    expect(REDEFINICAO_SENHA_OBRIGATORIA).toBe('Informe a nova senha.');
    expect(mensagemRedefinicaoSenhaTamanho(8, 64)).toBe('A nova senha deve ter entre 8 e 64 caracteres.');
    expect(REDEFINICAO_SENHA_FRACA).toBe('A nova senha deve ter ao menos uma letra e um número.');
    expect(REDEFINICAO_CONFIRMACAO_OBRIGATORIA).toBe('Confirme a nova senha.');
    expect(REDEFINICAO_CONFIRMACAO_DIFERENTE).toBe('A confirmação não é igual à nova senha.');
  });
});

describe('esquemaRedefinicao: tamanho da senha', () => {
  it('aceita 8 caracteres (com letra e número)', () => {
    expect(esquemaRedefinicao.safeParse(com('abcdefg1')).success).toBe(true);
  });

  it('aceita 64 caracteres (com letra e número)', () => {
    expect(esquemaRedefinicao.safeParse(com(`${'a'.repeat(63)}1`)).success).toBe(true);
  });

  it('recusa 7 caracteres', () => {
    expect(primeiraMensagem(com('abcdef1'), 'novaSenha')).toBe(TAMANHO_TEXTO);
  });

  it('recusa 65 caracteres', () => {
    expect(primeiraMensagem(com(`${'a'.repeat(64)}1`), 'novaSenha')).toBe(TAMANHO_TEXTO);
  });
});

describe('esquemaRedefinicao: letra e número', () => {
  it('recusa senha sem número', () => {
    expect(primeiraMensagem(com('abcdefghij'), 'novaSenha')).toBe(REDEFINICAO_SENHA_FRACA);
  });

  it('recusa senha sem letra', () => {
    expect(primeiraMensagem(com('12345678'), 'novaSenha')).toBe(REDEFINICAO_SENHA_FRACA);
  });
});

describe('esquemaRedefinicao: obrigatórios e confirmação', () => {
  it('exige a nova senha quando está vazia', () => {
    expect(primeiraMensagem(com('', ''), 'novaSenha')).toBe(REDEFINICAO_SENHA_OBRIGATORIA);
  });

  it('exige a confirmação quando está vazia', () => {
    expect(primeiraMensagem(com('abcdefg1', ''), 'confirmacaoSenha')).toBe(REDEFINICAO_CONFIRMACAO_OBRIGATORIA);
  });

  it('recusa confirmação diferente da nova senha', () => {
    expect(primeiraMensagem(com('abcdefg1', 'abcdefg2'), 'confirmacaoSenha')).toBe(
      REDEFINICAO_CONFIRMACAO_DIFERENTE,
    );
  });

  it('aceita confirmação igual', () => {
    expect(esquemaRedefinicao.safeParse(com('abcdefg1')).success).toBe(true);
  });

  it('checa a confirmação mesmo quando a nova senha tem outro erro', () => {
    // Senha curta e sem número, com confirmação diferente: a confirmação também aparece.
    expect(mensagensDoCampo(com('abc', 'xyz'), 'confirmacaoSenha')).toContain(REDEFINICAO_CONFIRMACAO_DIFERENTE);
    expect(primeiraMensagem(com('abc', 'xyz'), 'novaSenha')).toBe(TAMANHO_TEXTO);
  });

  it('não diz que a confirmação está errada quando ela está vazia (mostra só a obrigatoriedade)', () => {
    expect(primeiraMensagem(com('abcdefg1', ''), 'confirmacaoSenha')).toBe(REDEFINICAO_CONFIRMACAO_OBRIGATORIA);
  });

  it('não corta espaços da senha (espaço faz parte da senha)', () => {
    expect(esquemaRedefinicao.safeParse(com(' abcdefg1 ')).success).toBe(true);
  });
});
