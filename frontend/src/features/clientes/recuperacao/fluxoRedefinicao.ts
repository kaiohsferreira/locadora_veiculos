/**
 * Lógica pura de /redefinir-senha (N1, RF-49): sem React e sem API. A página decide o que mostrar
 * a partir daqui.
 */
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { ErroApi } from '@/types/api';
import type { RedefinirSenhaVO } from '@/types/autenticacao';
import type { ValoresRedefinicao } from './esquemaRedefinicao';

/** Campos da redefinição que a 400 pode apontar. O campo `token` leva ao estado de link inválido. */
export const CAMPOS_REDEFINICAO = ['novaSenha', 'confirmacaoSenha'] as const;

export type CampoRedefinicao = (typeof CAMPOS_REDEFINICAO)[number];

/**
 * Decisão depois de um erro do envio:
 * - `link-invalido`: o token não serve mais (409, ou 400 apontando o token). A página mostra o estado do link inválido;
 * - `campos`: mensagens nos campos de senha;
 * - `alerta`: alerta geral; o formulário e o token continuam para nova tentativa.
 */
export type DecisaoErroRedefinicao =
  | { tipo: 'link-invalido' }
  | { tipo: 'campos'; campos: Partial<Record<CampoRedefinicao, string>> }
  | { tipo: 'alerta'; mensagem: string };

/** Corpo do POST /api/auth/redefinicao-senha. O token vem do fragmento do link. */
export function montarRequisicaoRedefinicao(token: string, valores: ValoresRedefinicao): RedefinirSenhaVO {
  return {
    token,
    novaSenha: valores.novaSenha,
    confirmacaoSenha: valores.confirmacaoSenha,
  };
}

/**
 * Decide o estado da tela depois de um erro:
 * - 409: link inválido (expirado, usado, adulterado, conta inativa: o mesmo texto para todos);
 * - 400 com `token`: link inválido; 400 com campo de senha: mensagem no campo;
 * - qualquer outro erro (429, falha de rede, 400 sem campo conhecido): alerta geral.
 */
export function decidirErroRedefinicao(erro: unknown): DecisaoErroRedefinicao {
  if (erro instanceof ErroApi) {
    if (erro.status === 409) return { tipo: 'link-invalido' };

    if (erro.status === 400) {
      if (obterErroDeCampo(erro, 'token') !== undefined) return { tipo: 'link-invalido' };

      const campos: Partial<Record<CampoRedefinicao, string>> = {};
      const aplicou = aplicarErrosDeCampo(erro, CAMPOS_REDEFINICAO, (campo, mensagem) => {
        campos[campo] = mensagem;
      });
      if (aplicou) return { tipo: 'campos', campos };
    }
  }

  return { tipo: 'alerta', mensagem: obterMensagemErro(erro) };
}
