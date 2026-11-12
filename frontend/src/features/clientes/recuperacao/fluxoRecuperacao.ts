/**
 * Lógica pura de /recuperar-senha (N1, RF-49): sem React e sem API. A página e o hook só usam o resultado.
 */
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { somenteDigitos } from '@/lib/formatters';
import { ErroApi } from '@/types/api';
import { PERFIS_API, type PerfilApi, type SolicitarRecuperacaoSenhaVO } from '@/types/autenticacao';
import type { ValoresSolicitacao } from './esquemaSolicitacao';

/** Campos que a 400 da solicitação pode apontar (ProblemDetails.errors). */
export const CAMPOS_SOLICITACAO = ['perfil', 'cpf'] as const;

export type CampoSolicitacao = (typeof CAMPOS_SOLICITACAO)[number];

/** Decisão depois de um erro do envio: mensagem em um campo, ou alerta geral. */
export type DecisaoErroSolicitacao =
  | { tipo: 'campos'; campos: Partial<Record<CampoSolicitacao, string>> }
  | { tipo: 'alerta'; mensagem: string };

function ehPerfilApi(valor: unknown): valor is PerfilApi {
  return typeof valor === 'string' && (PERFIS_API as readonly string[]).includes(valor);
}

/**
 * Perfil marcado ao abrir a tela, vindo de `location.state` (enviado pelo link "Esqueci minha senha").
 * Valor ausente ou inválido é ignorado e vira Cliente.
 */
export function perfilInicialRecuperacao(estado: unknown): PerfilApi {
  if (typeof estado !== 'object' || estado === null || !('perfil' in estado)) return 'CLIENTE';
  return ehPerfilApi(estado.perfil) ? estado.perfil : 'CLIENTE';
}

/** Corpo do POST /api/auth/recuperacao-senha. O CPF vai só com dígitos. */
export function montarRequisicaoSolicitacao(valores: ValoresSolicitacao): SolicitarRecuperacaoSenhaVO {
  return {
    perfil: valores.perfil,
    cpf: somenteDigitos(valores.cpf),
  };
}

/**
 * Erro do envio da solicitação. 400 com campo conhecido vai para o campo; o restante (429, falha de
 * rede, 400 sem campo) vira alerta geral com `obterMensagemErro`.
 */
export function decidirErroSolicitacao(erro: unknown): DecisaoErroSolicitacao {
  if (erro instanceof ErroApi && erro.status === 400) {
    const campos: Partial<Record<CampoSolicitacao, string>> = {};
    const aplicou = aplicarErrosDeCampo(erro, CAMPOS_SOLICITACAO, (campo, mensagem) => {
      campos[campo] = mensagem;
    });
    if (aplicou) return { tipo: 'campos', campos };
  }
  return { tipo: 'alerta', mensagem: obterMensagemErro(erro) };
}
