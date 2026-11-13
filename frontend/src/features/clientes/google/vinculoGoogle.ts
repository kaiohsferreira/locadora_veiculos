/**
 * Regras puras do vínculo da conta Google (N3, G8): corpo do POST /api/auth/google/vinculo
 * e decisão para cada erro do envio. Sem React, sem API.
 */
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { somenteDigitos } from '@/lib/formatters';
import { GOOGLE_VINCULAR_PROVA_EXPIRADA, MENSAGEM_CREDENCIAIS_INVALIDAS } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { GoogleVinculoVO } from '@/types/autenticacao';
import type { ValoresVinculo } from './esquemaVinculo';

/** Campos que o 400 do vínculo pode apontar (ProblemDetails.errors). */
export const CAMPOS_VINCULO = ['cpf', 'senha'] as const;

export type CampoVinculo = (typeof CAMPOS_VINCULO)[number];

/** Corpo do vínculo: CPF só com dígitos e `lembrar` sempre booleano. */
export function montarRequisicaoVinculo(
  valores: ValoresVinculo,
  credencial: string,
  lembrar: boolean,
): GoogleVinculoVO {
  return {
    credencial,
    cpf: somenteDigitos(valores.cpf),
    senha: valores.senha,
    lembrar: lembrar === true,
  };
}

/** Decisão depois de um erro do envio do vínculo. */
export type DecisaoErroVinculo =
  /** 400: mensagem junto dos campos de CPF e senha apontados pela API. */
  | { tipo: 'campos'; campos: Partial<Record<CampoVinculo, string>> }
  /**
   * 401 de CPF ou senha: a mensagem vai junto do campo de senha, que é limpo e recebe o foco.
   * A credencial continua valendo: a pessoa pode tentar de novo.
   */
  | { tipo: 'senha-recusada'; mensagem: string }
  /** Alerta geral. `voltarAoLogin` oferece o botão "Voltar ao login" (a prova do Google não serve mais). */
  | { tipo: 'alerta'; mensagem: string; voltarAoLogin: boolean };

/** Campo do 400 que aponta a credencial do Google (não o CPF nem a senha). */
const CAMPO_CREDENCIAL = 'credencial';

/**
 * Decide o que fazer com o erro do vínculo:
 * - 400 com cpf ou senha → campos;
 * - 400 que aponta a credencial, 401 de prova do Google (inválida ou expirada) → alerta "Volte ao login"
 *   com o texto do front (reenviar a mesma prova não adianta);
 * - 401 com a mensagem de CPF ou senha → senha-recusada;
 * - 409 (conta já vinculada, cliente já vinculado, conta ambígua) → alerta com "Voltar ao login";
 * - 429, 502 e falha de rede → alerta com a mensagem da API (ou de conexão), sem voltar.
 */
export function decidirErroVinculo(erro: unknown): DecisaoErroVinculo {
  const status = erro instanceof ErroApi ? erro.status : 0;

  if (status === 400) {
    const campos: Partial<Record<CampoVinculo, string>> = {};
    const aplicou = aplicarErrosDeCampo(erro, CAMPOS_VINCULO, (campo, mensagem) => {
      campos[campo] = mensagem;
    });
    if (aplicou) return { tipo: 'campos', campos };

    if (obterErroDeCampo(erro, CAMPO_CREDENCIAL) !== undefined) {
      return { tipo: 'alerta', mensagem: GOOGLE_VINCULAR_PROVA_EXPIRADA, voltarAoLogin: true };
    }
  }

  const mensagem = obterMensagemErro(erro);

  if (status === 401) {
    if (mensagem === MENSAGEM_CREDENCIAIS_INVALIDAS) return { tipo: 'senha-recusada', mensagem };
    return { tipo: 'alerta', mensagem: GOOGLE_VINCULAR_PROVA_EXPIRADA, voltarAoLogin: true };
  }

  if (status === 409) return { tipo: 'alerta', mensagem, voltarAoLogin: true };

  return { tipo: 'alerta', mensagem, voltarAoLogin: false };
}
