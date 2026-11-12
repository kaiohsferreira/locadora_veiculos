/**
 * Validação da tela /entrar/vincular-google (N3, G7), com Zod. Roda antes de chamar a API.
 * Mesmos textos e limites do login por CPF (esquemaAcesso).
 */
import { z } from 'zod';
import { somenteDigitos } from '@/lib/formatters';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_SENHA_OBRIGATORIA,
  ACESSO_ERRO_SENHA_TAMANHO,
  ACESSO_TAMANHO_MAXIMO_SENHA,
} from '@/lib/mensagens';

/** Tamanho do CPF com dígitos (sem a máscara). */
const TAMANHO_CPF = 11;

export const esquemaVinculo = z.object({
  cpf: z
    .string({ error: ACESSO_ERRO_CPF_OBRIGATORIO })
    .refine((valor) => somenteDigitos(valor).length > 0, {
      error: ACESSO_ERRO_CPF_OBRIGATORIO,
      abort: true,
    })
    .refine((valor) => somenteDigitos(valor).length === TAMANHO_CPF, {
      error: ACESSO_ERRO_CPF_DIGITOS,
      abort: true,
    }),
  senha: z
    .string({ error: ACESSO_ERRO_SENHA_OBRIGATORIA })
    .min(1, { error: ACESSO_ERRO_SENHA_OBRIGATORIA, abort: true })
    .max(ACESSO_TAMANHO_MAXIMO_SENHA, { error: ACESSO_ERRO_SENHA_TAMANHO }),
});

/** Valores do formulário já validados. */
export type ValoresVinculo = z.infer<typeof esquemaVinculo>;
