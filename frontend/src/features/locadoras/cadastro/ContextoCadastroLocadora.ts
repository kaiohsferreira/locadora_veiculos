import { createContext, useContext } from 'react';
import type { RespostaEmpresa } from '@/types/empresas';
import type { FormularioEmpresa } from './esquemaLocadora';
import type { DadosResponsavelLocadora } from './esquemaResponsavel';
import type { ConflitosEmpresaLocadora, ErroCampoLocadora, EtapaErroLocadora } from './mapeamentoErroLocadora';

/** Conta criada pelo envio da etapa 2: o que a etapa 3 mostra. Nunca traz a senha. */
export interface ResultadoCadastroLocadora {
  /** Resposta 201 do POST /api/empresas (sem senha nem hash). */
  empresa: RespostaEmpresa;
  /** E-mail de acesso digitado: reserva da tela 38, usada só se a resposta não trouxer `responsavelEmail`. */
  emailAcesso: string;
  /** Falso quando a conta foi criada mas o login automático falhou (a etapa 3 oferece "Fazer login"). */
  loginAutomatico: boolean;
}

/**
 * Dados do cadastro da locadora entre as etapas, só em memória (nada em localStorage nem sessionStorage).
 * A senha nunca entra aqui: ela existe apenas no formulário da etapa 2 e na chamada de envio.
 */
export interface ContextoCadastroLocadoraValor {
  /** Dados da etapa 1, ou null enquanto a etapa 1 não foi concluída (as etapas 2 e 3 voltam para a 1). */
  dadosEmpresa: FormularioEmpresa | null;
  /** Guarda os dados da etapa 1 ao concluí-la. */
  salvarDadosEmpresa: (dados: FormularioEmpresa) => void;
  /** Dados do responsável sem a senha (para o "Voltar" da etapa 2 não perder o que foi digitado). */
  dadosResponsavel: DadosResponsavelLocadora | null;
  salvarDadosResponsavel: (dados: DadosResponsavelLocadora) => void;
  /** Erros do envio que ainda esperam a tela da etapa certa (aplicados e removidos pela própria etapa). */
  errosServidor: readonly ErroCampoLocadora[];
  definirErrosServidor: (erros: readonly ErroCampoLocadora[]) => void;
  removerErrosServidor: (etapa: EtapaErroLocadora) => void;
  /** CNPJ e e-mail que o servidor recusou (valores normalizados): a etapa 1 não avança com eles. */
  conflitos: ConflitosEmpresaLocadora;
  registrarConflitos: (conflitos: ConflitosEmpresaLocadora) => void;
  /** Conta criada, ou null enquanto o cadastro não foi concluído. Some ao recarregar a página. */
  resultado: ResultadoCadastroLocadora | null;
  salvarResultado: (resultado: ResultadoCadastroLocadora) => void;
}

export const ContextoCadastroLocadora = createContext<ContextoCadastroLocadoraValor | null>(null);

/** Dados em memória do cadastro da locadora. Só funciona dentro de <ProvedorCadastroLocadora>. */
export function useCadastroLocadora(): ContextoCadastroLocadoraValor {
  const contexto = useContext(ContextoCadastroLocadora);
  if (!contexto) {
    throw new Error('useCadastroLocadora precisa ser usado dentro de <ProvedorCadastroLocadora>.');
  }
  return contexto;
}
