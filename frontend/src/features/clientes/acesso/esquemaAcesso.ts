/**
 * Validação do formulário de acesso (P2-FE-01). Roda antes de chamar a API.
 * No login o backend NÃO confere o dígito verificador do CPF, então esta validação também não confere.
 */
import { z } from 'zod';
import { somenteDigitos } from '@/lib/formatters';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_PERFIL_OBRIGATORIO,
  ACESSO_ERRO_SENHA_OBRIGATORIA,
  ACESSO_ERRO_SENHA_TAMANHO,
  ACESSO_TAMANHO_MAXIMO_SENHA,
} from '@/lib/mensagens';
import { PERFIS_API } from '@/types/autenticacao';

/** Tamanho do CPF com dígitos (sem a máscara). */
const TAMANHO_CPF = 11;

export const esquemaAcesso = z.object({
  perfil: z.enum(PERFIS_API, { error: ACESSO_ERRO_PERFIL_OBRIGATORIO }),
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

/** Valores do formulário, como o usuário preencheu (CPF com ou sem máscara). */
export type ValoresAcesso = z.infer<typeof esquemaAcesso>;
