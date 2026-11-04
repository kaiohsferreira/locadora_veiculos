import { useCallback, useMemo, useSyncExternalStore, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { IGNORAR_AUTENTICACAO } from '@/lib/ambiente';
import { inscreverMudancasSessao, limparSessao, obterUsuarioSessao, salvarSessao } from '@/lib/sessao';
import type { PerfilUsuario, UsuarioSessao } from '@/types/autenticacao';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './AutenticacaoContexto';

export interface AutenticacaoProviderProps {
  children: ReactNode;
}

/**
 * Contexto de autenticação mínimo. Lê a sessão de `@/lib/sessao` e reage a
 * login, logout, token expirado (401) e mudanças feitas em outra aba.
 */
export function AutenticacaoProvider({ children }: AutenticacaoProviderProps) {
  const usuario = useSyncExternalStore(inscreverMudancasSessao, obterUsuarioSessao);
  const clienteConsultas = useQueryClient();

  const entrar = useCallback((token: string, dados: UsuarioSessao) => {
    salvarSessao(token, dados);
  }, []);

  const sair = useCallback(() => {
    limparSessao();
    clienteConsultas.clear();
  }, [clienteConsultas]);

  const valor = useMemo<AutenticacaoContextoValor>(
    () => ({
      usuario,
      autenticado: usuario !== null,
      autenticacaoIgnorada: IGNORAR_AUTENTICACAO,
      entrar,
      sair,
      possuiPerfil: (perfis: readonly PerfilUsuario[]) => usuario !== null && perfis.includes(usuario.perfil),
    }),
    [usuario, entrar, sair],
  );

  return <AutenticacaoContexto.Provider value={valor}>{children}</AutenticacaoContexto.Provider>;
}
