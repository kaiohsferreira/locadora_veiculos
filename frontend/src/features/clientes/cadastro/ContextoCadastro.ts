import { createContext } from 'react';
import type { ErroCampoCadastro, EtapaCadastro } from './etapasCadastro';
import type { DadosPessoaisCadastro, DocumentosCadastro } from './esquemaCadastro';

/**
 * Dados do cadastro entre as duas telas, só em memória (nada em localStorage nem sessionStorage).
 * A senha nunca entra aqui: ela existe apenas no formulário da etapa 2.
 */
export interface CadastroContextoValor {
  /** Dados da etapa 1, ou null enquanto a etapa 1 não foi concluída (a etapa 2 volta para a 1). */
  dadosPessoais: DadosPessoaisCadastro | null;
  /** CNH e endereço já digitados na etapa 2 (sem senha), para não se perderem ao ir e voltar. */
  documentos: DocumentosCadastro | null;
  /** O nome e o e-mail da etapa 1 vieram do login com Google (a faixa de aviso continua ao voltar). */
  veioDoGoogle: boolean;
  /** Erros da API por campo, esperando a tela do campo abrir (ex.: 409 de CPF volta para a etapa 1). */
  errosServidor: readonly ErroCampoCadastro[];
  salvarDadosPessoais: (dados: DadosPessoaisCadastro, veioDoGoogle: boolean) => void;
  salvarDocumentos: (documentos: DocumentosCadastro) => void;
  definirErrosServidor: (erros: readonly ErroCampoCadastro[]) => void;
  /** Tira da espera os erros da etapa, depois de a tela tê-los aplicado aos campos. */
  removerErrosServidor: (etapa: EtapaCadastro) => void;
}

export const CadastroContexto = createContext<CadastroContextoValor | null>(null);
