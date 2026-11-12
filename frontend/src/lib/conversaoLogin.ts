import type { PerfilApi, PerfilUsuario, RespostaLogin, UsuarioSessao } from '@/types/autenticacao';

/** Perfil do backend para o perfil usado na sessão do front. */
const PERFIL_POR_API: Readonly<Record<PerfilApi, PerfilUsuario>> = {
  CLIENTE: 'cliente',
  FUNCIONARIO: 'funcionario',
};

/** Perfil da sessão para o perfil que o backend devolveu no login. Fonte única do mapeamento. */
export function perfilUsuarioDaApi(perfil: PerfilApi): PerfilUsuario {
  return PERFIL_POR_API[perfil];
}

/** Decodifica base64url (com ou sem padding) para o texto do segmento. Usado só para ler o payload. */
function decodificarBase64Url(segmento: string): string {
  const base64 = segmento.replace(/-/g, '+').replace(/_/g, '/');
  const preenchido = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
  return atob(preenchido);
}

/**
 * Vencimento do token em ISO UTC (com `Z`), lido do claim `exp` do próprio JWT (segundos
 * desde a época). Só lê o payload: NÃO verifica a assinatura (quem valida é a API) e nunca
 * registra o token. Devolve null quando o token não tem 3 partes, o payload não é JSON
 * ou `exp` não é um número finito.
 */
export function lerVencimentoDoToken(token: string): string | null {
  const partes = token.split('.');
  if (partes.length !== 3) return null;
  try {
    const payload: unknown = JSON.parse(decodificarBase64Url(partes[1]));
    if (typeof payload !== 'object' || payload === null) return null;
    const exp = (payload as Record<string, unknown>).exp;
    if (typeof exp !== 'number' || !Number.isFinite(exp)) return null;
    return new Date(exp * 1000).toISOString();
  } catch {
    return null;
  }
}

/**
 * Converte a resposta do login (ReturnLoginVO) no usuário guardado na sessão.
 * `expiraEm` vem do claim `exp` do token (UTC): o backend manda a hora local do servidor
 * sem fuso, e o navegador leria no próprio fuso. Sem token legível, usa o `expiraEm` do backend.
 * Campos `null` ou ausentes ficam ausentes no resultado (ex.: cargo só para funcionário).
 */
export function converterRespostaLogin(resposta: RespostaLogin): UsuarioSessao {
  const usuario: UsuarioSessao = {
    nome: resposta.nome,
    perfil: perfilUsuarioDaApi(resposta.perfil),
    idUsuario: resposta.idUsuario,
    expiraEm: lerVencimentoDoToken(resposta.token) ?? resposta.expiraEm,
  };
  if (resposta.cargo) usuario.cargo = resposta.cargo;
  if (resposta.statusCadastro) usuario.statusCadastro = resposta.statusCadastro;
  return usuario;
}
