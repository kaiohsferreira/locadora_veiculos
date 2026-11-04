/**
 * Sessão do usuário no navegador (token + dados mínimos do usuário).
 *
 * Guarda em localStorage, sempre com try/catch: em janela anônima ou com o
 * armazenamento bloqueado a sessão continua valendo enquanto a página estiver
 * aberta (cópia em memória). Outros módulos acompanham as mudanças com
 * `inscreverMudancasSessao` (usado pelo contexto de autenticação).
 */
import { PERFIS_USUARIO, type PerfilUsuario, type UsuarioSessao } from '@/types/autenticacao';

const CHAVE_TOKEN = 'autovia:token';
const CHAVE_USUARIO = 'autovia:usuario';

interface SessaoArmazenada {
  token: string;
  usuario: UsuarioSessao;
}

type OuvinteSessao = () => void;

const ouvintes = new Set<OuvinteSessao>();

function lerItem(chave: string): string | null {
  try {
    return window.localStorage.getItem(chave);
  } catch {
    return null;
  }
}

function gravarItem(chave: string, valor: string): void {
  try {
    window.localStorage.setItem(chave, valor);
  } catch {
    // Armazenamento indisponível: a sessão fica só em memória.
  }
}

function removerItem(chave: string): void {
  try {
    window.localStorage.removeItem(chave);
  } catch {
    // Armazenamento indisponível: nada a remover.
  }
}

function ehPerfilUsuario(valor: unknown): valor is PerfilUsuario {
  return typeof valor === 'string' && (PERFIS_USUARIO as readonly string[]).includes(valor);
}

function ehUsuarioSessao(valor: unknown): valor is UsuarioSessao {
  if (typeof valor !== 'object' || valor === null) return false;
  const candidato = valor as Record<string, unknown>;
  return typeof candidato.nome === 'string' && ehPerfilUsuario(candidato.perfil);
}

function lerSessaoArmazenada(): SessaoArmazenada | null {
  const token = lerItem(CHAVE_TOKEN);
  const usuarioJson = lerItem(CHAVE_USUARIO);
  if (!token || !usuarioJson) return null;
  try {
    const usuario: unknown = JSON.parse(usuarioJson);
    return ehUsuarioSessao(usuario) ? { token, usuario } : null;
  } catch {
    return null;
  }
}

let sessaoAtual: SessaoArmazenada | null = lerSessaoArmazenada();

function notificar(): void {
  ouvintes.forEach((ouvinte) => ouvinte());
}

/** Token salvo, ou null quando não há sessão. */
export function obterToken(): string | null {
  return sessaoAtual?.token ?? null;
}

/** Usuário da sessão, ou null quando não há sessão. Devolve sempre a mesma referência enquanto a sessão não muda. */
export function obterUsuarioSessao(): UsuarioSessao | null {
  return sessaoAtual?.usuario ?? null;
}

/** Abre a sessão após o login. */
export function salvarSessao(token: string, usuario: UsuarioSessao): void {
  sessaoAtual = { token, usuario: { nome: usuario.nome, perfil: usuario.perfil } };
  gravarItem(CHAVE_TOKEN, token);
  gravarItem(CHAVE_USUARIO, JSON.stringify(sessaoAtual.usuario));
  notificar();
}

/** Encerra a sessão (logout ou token expirado). */
export function limparSessao(): void {
  if (sessaoAtual === null && lerItem(CHAVE_TOKEN) === null) return;
  sessaoAtual = null;
  removerItem(CHAVE_TOKEN);
  removerItem(CHAVE_USUARIO);
  notificar();
}

/** Registra um ouvinte de mudanças da sessão. Devolve a função que cancela a inscrição. */
export function inscreverMudancasSessao(ouvinte: OuvinteSessao): () => void {
  ouvintes.add(ouvinte);
  return () => {
    ouvintes.delete(ouvinte);
  };
}

// Mantém abas abertas sincronizadas (login ou logout feito em outra aba).
window.addEventListener('storage', (evento: StorageEvent) => {
  if (evento.key === null || evento.key === CHAVE_TOKEN || evento.key === CHAVE_USUARIO) {
    sessaoAtual = lerSessaoArmazenada();
    notificar();
  }
});
