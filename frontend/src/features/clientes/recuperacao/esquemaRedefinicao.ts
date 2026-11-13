/**
 * Validação do formulário de /redefinir-senha (N1, RF-49), com Zod. Espelha o RedefinirSenhaVOValidator
 * do backend: 8 a 64 caracteres, ao menos uma letra e um número, e confirmação igual.
 *
 * Os textos são os do backend (ConstantsMessage.RecuperacaoSenha.cs), em `mensagensRecuperacao`.
 * Os limites vêm do cadastro (LIMITES_CADASTRO), que têm o mesmo valor. O backend continua sendo a palavra final.
 */
import { z } from 'zod';
import { LIMITES_CADASTRO } from '@/features/clientes/cadastro/esquemaCadastro';
import {
  mensagemRedefinicaoSenhaTamanho,
  REDEFINICAO_CONFIRMACAO_DIFERENTE,
  REDEFINICAO_CONFIRMACAO_OBRIGATORIA,
  REDEFINICAO_SENHA_FRACA,
  REDEFINICAO_SENHA_OBRIGATORIA,
} from '@/lib/mensagens';

/** Valores do formulário de redefinição. As senhas não passam por trim (espaços fazem parte da senha). */
export const esquemaRedefinicao = z
  .object({
    novaSenha: z
      .string({ error: REDEFINICAO_SENHA_OBRIGATORIA })
      .refine((valor) => valor.length > 0, { error: REDEFINICAO_SENHA_OBRIGATORIA, abort: true })
      .refine(
        (valor) => valor.length >= LIMITES_CADASTRO.senhaMinima && valor.length <= LIMITES_CADASTRO.senhaMaxima,
        {
          error: mensagemRedefinicaoSenhaTamanho(LIMITES_CADASTRO.senhaMinima, LIMITES_CADASTRO.senhaMaxima),
        },
      )
      // Mesma regra do cadastro: ao menos uma letra (qualquer alfabeto) e um dígito.
      .refine((valor) => /\p{L}/u.test(valor) && /\p{Nd}/u.test(valor), {
        error: REDEFINICAO_SENHA_FRACA,
      }),

    confirmacaoSenha: z
      .string({ error: REDEFINICAO_CONFIRMACAO_OBRIGATORIA })
      .refine((valor) => valor.length > 0, { error: REDEFINICAO_CONFIRMACAO_OBRIGATORIA, abort: true }),
  })
  // Roda junto com os erros dos campos (o esquema não aborta por campo): um erro de senha não esconde a confirmação.
  .refine((dados) => dados.confirmacaoSenha === dados.novaSenha, {
    error: REDEFINICAO_CONFIRMACAO_DIFERENTE,
    path: ['confirmacaoSenha'],
  });

/** Valores do formulário de redefinição (já validados). */
export type ValoresRedefinicao = z.infer<typeof esquemaRedefinicao>;
