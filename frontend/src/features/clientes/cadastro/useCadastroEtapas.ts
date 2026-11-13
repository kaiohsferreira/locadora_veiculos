import { useContext } from 'react';
import { CadastroContexto, type CadastroContextoValor } from './ContextoCadastro';

/** Dados em memória do cadastro em duas etapas. Só funciona dentro de <ProvedorCadastro>. */
export function useCadastroEtapas(): CadastroContextoValor {
  const contexto = useContext(CadastroContexto);
  if (!contexto) {
    throw new Error('useCadastroEtapas precisa ser usado dentro de <ProvedorCadastro>.');
  }
  return contexto;
}
