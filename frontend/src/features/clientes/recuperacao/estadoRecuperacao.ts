/**
 * Estado de navegação que passa pela tela /recuperar-senha (N1, RF-49), em `location.state`.
 * Função pura: sem React, sem navegação.
 *
 * Leva o perfil marcado e o caminho de retorno do login (`de`), para que "Voltar ao login" devolva
 * a pessoa ao mesmo lugar, com o mesmo perfil. Só carrega os campos conhecidos: nada de CPF, senha
 * ou token passa por aqui.
 */
import { PERFIS_API, type PerfilApi } from '@/types/autenticacao';

/** Campos do estado de recuperação. Ambos opcionais: quem não tem o dado não envia o campo. */
export interface EstadoRecuperacao {
  /** Perfil marcado na tela de acesso (Cliente ou Equipe). */
  perfil?: PerfilApi;
  /** Caminho interno para onde voltar depois do login (já validado por `obterCaminhoRetorno`). */
  de?: string;
}

function ehPerfilApi(valor: unknown): valor is PerfilApi {
  return typeof valor === 'string' && (PERFIS_API as readonly string[]).includes(valor);
}

/**
 * Monta o estado enviado a /recuperar-senha ou devolvido a /entrar. `de` vazio não entra no estado.
 * Quem chama passa o caminho já validado (ex.: `obterCaminhoRetorno(estado, '')`).
 */
export function montarEstadoRecuperacao(perfil: PerfilApi, de: string): EstadoRecuperacao {
  return de === '' ? { perfil } : { perfil, de };
}

/** Lê só os campos conhecidos do `location.state`. Valor de outro tipo ou desconhecido fica de fora. */
export function lerEstadoRecuperacao(estado: unknown): EstadoRecuperacao {
  if (typeof estado !== 'object' || estado === null) return {};
  const campos = estado as Partial<Record<keyof EstadoRecuperacao, unknown>>;
  const resultado: EstadoRecuperacao = {};
  if (ehPerfilApi(campos.perfil)) resultado.perfil = campos.perfil;
  if (typeof campos.de === 'string' && campos.de !== '') resultado.de = campos.de;
  return resultado;
}
