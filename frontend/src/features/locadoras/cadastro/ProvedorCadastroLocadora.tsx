import { useCallback, useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Outlet } from 'react-router-dom';
import {
  ContextoCadastroLocadora,
  type ContextoCadastroLocadoraValor,
  type ResultadoCadastroLocadora,
} from './ContextoCadastroLocadora';
import type { FormularioEmpresa } from './esquemaLocadora';
import type { DadosResponsavelLocadora } from './esquemaResponsavel';
import type { ConflitosEmpresaLocadora, ErroCampoLocadora, EtapaErroLocadora } from './mapeamentoErroLocadora';
import { CHAVE_VERIFICACAO_LOCADORA } from './verificacaoLocadora';

/**
 * Rota-pai das etapas do cadastro da locadora: guarda, só em memória, os dados das etapas (sem a senha), os
 * erros do envio que esperam a tela certa, os conflitos de CNPJ e e-mail e o resultado da conta criada.
 * Ao desmontar (saída do cadastro), apaga da memória as consultas da verificação de CNPJ e e-mail.
 */
export function ProvedorCadastroLocadora() {
  const [dadosEmpresa, setDadosEmpresa] = useState<FormularioEmpresa | null>(null);
  const [dadosResponsavel, setDadosResponsavel] = useState<DadosResponsavelLocadora | null>(null);
  const [errosServidor, setErrosServidor] = useState<readonly ErroCampoLocadora[]>([]);
  const [conflitos, setConflitos] = useState<ConflitosEmpresaLocadora>({});
  const [resultado, setResultado] = useState<ResultadoCadastroLocadora | null>(null);
  const queryClient = useQueryClient();

  useEffect(() => {
    return () => {
      queryClient.removeQueries({ queryKey: [CHAVE_VERIFICACAO_LOCADORA] });
    };
  }, [queryClient]);

  const salvarDadosEmpresa = useCallback((dados: FormularioEmpresa) => {
    setDadosEmpresa(dados);
  }, []);

  const salvarDadosResponsavel = useCallback((dados: DadosResponsavelLocadora) => {
    setDadosResponsavel(dados);
  }, []);

  const definirErrosServidor = useCallback((erros: readonly ErroCampoLocadora[]) => {
    setErrosServidor(erros);
  }, []);

  const removerErrosServidor = useCallback((etapa: EtapaErroLocadora) => {
    setErrosServidor((atuais) => {
      const restantes = atuais.filter((erro) => erro.etapa !== etapa);
      return restantes.length === atuais.length ? atuais : restantes;
    });
  }, []);

  const registrarConflitos = useCallback((novos: ConflitosEmpresaLocadora) => {
    setConflitos((atuais) => ({ ...atuais, ...novos }));
  }, []);

  const salvarResultado = useCallback((novo: ResultadoCadastroLocadora) => {
    setResultado(novo);
  }, []);

  const valor = useMemo<ContextoCadastroLocadoraValor>(
    () => ({
      dadosEmpresa,
      salvarDadosEmpresa,
      dadosResponsavel,
      salvarDadosResponsavel,
      errosServidor,
      definirErrosServidor,
      removerErrosServidor,
      conflitos,
      registrarConflitos,
      resultado,
      salvarResultado,
    }),
    [
      dadosEmpresa,
      salvarDadosEmpresa,
      dadosResponsavel,
      salvarDadosResponsavel,
      errosServidor,
      definirErrosServidor,
      removerErrosServidor,
      conflitos,
      registrarConflitos,
      resultado,
      salvarResultado,
    ],
  );

  return (
    <ContextoCadastroLocadora.Provider value={valor}>
      <Outlet />
    </ContextoCadastroLocadora.Provider>
  );
}
