import { createContext } from 'react';
import type { PerfilUsuario, UsuarioSessao } from '@/types/autenticacao';

export interface AutenticacaoContextoValor {
  /** Usuário da sessão, ou null quando ninguém entrou. */
  usuario: UsuarioSessao | null;
  /** Há sessão aberta (token + usuário). */
  autenticado: boolean;
  /** Desvio de desenvolvimento ativo (VITE_IGNORAR_AUTENTICACAO=true no npm run dev). */
  autenticacaoIgnorada: boolean;
  /** Abre a sessão após o login (o token vai para o cabeçalho Authorization). */
  entrar: (token: string, usuario: UsuarioSessao) => void;
  /** Encerra a sessão e limpa o cache de dados do usuário anterior. */
  sair: () => void;
  /** O usuário da sessão tem um dos perfis informados. */
  possuiPerfil: (perfis: readonly PerfilUsuario[]) => boolean;
}

export const AutenticacaoContexto = createContext<AutenticacaoContextoValor | null>(null);
