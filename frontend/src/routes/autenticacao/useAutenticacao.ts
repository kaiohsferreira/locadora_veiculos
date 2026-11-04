import { useContext } from 'react';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './AutenticacaoContexto';

/** Dados e ações da sessão do usuário. */
export function useAutenticacao(): AutenticacaoContextoValor {
  const contexto = useContext(AutenticacaoContexto);
  if (!contexto) {
    throw new Error('useAutenticacao precisa ser usado dentro de <AutenticacaoProvider>.');
  }
  return contexto;
}
