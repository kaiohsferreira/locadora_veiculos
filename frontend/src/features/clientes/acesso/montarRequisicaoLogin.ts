/**
 * Monta o corpo do POST /api/auth/login a partir dos valores já validados.
 * `lembrar` vai sempre como booleano; o CPF vai só com dígitos.
 */
import { somenteDigitos } from '@/lib/formatters';
import type { RequisicaoLogin } from '@/types/autenticacao';
import type { ValoresAcesso } from './esquemaAcesso';

export function montarRequisicaoLogin(valores: ValoresAcesso): RequisicaoLogin {
  return {
    perfil: valores.perfil,
    cpf: somenteDigitos(valores.cpf),
    senha: valores.senha,
    lembrar: valores.lembrar === true,
  };
}
