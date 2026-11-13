import { useCallback, useMemo, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { CadastroContexto, type CadastroContextoValor } from './ContextoCadastro';
import { etapaDoCampo, type ErroCampoCadastro, type EtapaCadastro } from './etapasCadastro';
import type { DadosPessoaisCadastro, DocumentosCadastro } from './esquemaCadastro';

/** Estado em memória do cadastro. Some quando a pessoa sai das duas telas (o provedor desmonta). */
interface EstadoCadastro {
  dadosPessoais: DadosPessoaisCadastro | null;
  documentos: DocumentosCadastro | null;
  veioDoGoogle: boolean;
  errosServidor: readonly ErroCampoCadastro[];
}

const ESTADO_INICIAL: EstadoCadastro = {
  dadosPessoais: null,
  documentos: null,
  veioDoGoogle: false,
  errosServidor: [],
};

/**
 * Rota-pai das duas telas do cadastro: guarda, só em memória, os dados da etapa 1, a CNH e o endereço
 * da etapa 2 e os erros da API que apontam para o campo de outra tela. Não guarda a senha.
 */
export function ProvedorCadastro() {
  const [estado, setEstado] = useState<EstadoCadastro>(ESTADO_INICIAL);

  const salvarDadosPessoais = useCallback((dados: DadosPessoaisCadastro, veioDoGoogle: boolean) => {
    setEstado((atual) => ({ ...atual, dadosPessoais: dados, veioDoGoogle }));
  }, []);

  const salvarDocumentos = useCallback((documentos: DocumentosCadastro) => {
    setEstado((atual) => ({ ...atual, documentos }));
  }, []);

  const definirErrosServidor = useCallback((erros: readonly ErroCampoCadastro[]) => {
    setEstado((atual) => ({ ...atual, errosServidor: erros }));
  }, []);

  const removerErrosServidor = useCallback((etapa: EtapaCadastro) => {
    setEstado((atual) => {
      const restantes = atual.errosServidor.filter((erro) => etapaDoCampo(erro.campo) !== etapa);
      return restantes.length === atual.errosServidor.length ? atual : { ...atual, errosServidor: restantes };
    });
  }, []);

  const valor = useMemo<CadastroContextoValor>(
    () => ({
      ...estado,
      salvarDadosPessoais,
      salvarDocumentos,
      definirErrosServidor,
      removerErrosServidor,
    }),
    [estado, salvarDadosPessoais, salvarDocumentos, definirErrosServidor, removerErrosServidor],
  );

  return (
    <CadastroContexto.Provider value={valor}>
      <Outlet />
    </CadastroContexto.Provider>
  );
}
