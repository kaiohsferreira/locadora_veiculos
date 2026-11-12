import { useCallback, useEffect, useMemo, useRef, useSyncExternalStore, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { IGNORAR_AUTENTICACAO } from '@/lib/ambiente';
import { inscreverMudancasSessao, limparSessao, obterUsuarioSessao, salvarSessao } from '@/lib/sessao';
import type { PerfilUsuario, UsuarioSessao } from '@/types/autenticacao';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './AutenticacaoContexto';

export interface AutenticacaoProviderProps {
  children: ReactNode;
}

/** Identidade de quem está na sessão (perfil, id e nome). Só muda quando o usuário muda. */
function chaveIdentidade(usuario: UsuarioSessao | null): string | null {
  if (usuario === null) return null;
  return `${usuario.perfil}|${usuario.idUsuario ?? ''}|${usuario.nome}`;
}

/**
 * Contexto de autenticação mínimo. Lê a sessão de `@/lib/sessao` e reage a
 * login, logout, token expirado (401) e mudanças feitas em outra aba.
 * O cache de consultas é limpo sempre que a identidade muda: aqui (entrar/sair)
 * ou em outra aba (evento storage). A mudança desta aba não é limpa de novo.
 */
export function AutenticacaoProvider({ children }: AutenticacaoProviderProps) {
  const usuario = useSyncExternalStore(inscreverMudancasSessao, obterUsuarioSessao);
  const clienteConsultas = useQueryClient();
  /** Marcada por entrar/sair desta aba: a próxima mudança de sessão já foi tratada aqui. */
  const mudancaNestaAba = useRef(false);
  const identidadeAnterior = useRef<string | null>(chaveIdentidade(usuario));

  useEffect(() => {
    const atual = chaveIdentidade(usuario);
    const anterior = identidadeAnterior.current;
    const veioDestaAba = mudancaNestaAba.current;
    identidadeAnterior.current = atual;
    mudancaNestaAba.current = false;
    if (atual === anterior || veioDestaAba) return;
    // Outra aba trocou o usuário: consultas desta aba pertencem ao usuário anterior.
    clienteConsultas.clear();
  }, [usuario, clienteConsultas]);

  const entrar = useCallback(
    (token: string, dados: UsuarioSessao) => {
      mudancaNestaAba.current = true;
      // Limpa o cache antes de abrir a sessão: não mostra dado de um usuário anterior.
      clienteConsultas.clear();
      salvarSessao(token, dados);
    },
    [clienteConsultas],
  );

  const sair = useCallback(() => {
    // Só marca quando há sessão para encerrar: sem mudança, a marca sobraria para a próxima.
    if (obterUsuarioSessao() !== null) mudancaNestaAba.current = true;
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
