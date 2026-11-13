/**
 * Validação do formulário de /recuperar-senha (N1, RF-49). Roda antes de chamar a API.
 * Os textos são os mesmos do login (CPF e perfil). Como no login, o dígito verificador não é conferido:
 * o backend também só confere o formato (11 dígitos depois de remover a máscara).
 */
import { z } from 'zod';
import { somenteDigitos } from '@/lib/formatters';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_PERFIL_OBRIGATORIO,
} from '@/lib/mensagens';
import { PERFIS_API } from '@/types/autenticacao';

/** Tamanho do CPF com dígitos (sem a máscara). */
const TAMANHO_CPF = 11;

export const esquemaSolicitacao = z.object({
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
});

/** Valores já validados do formulário de solicitação. */
export type ValoresSolicitacao = z.infer<typeof esquemaSolicitacao>;
