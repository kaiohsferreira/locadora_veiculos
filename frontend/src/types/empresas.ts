/**
 * Tipos do cadastro da locadora (contrato empresa.contract.json v1, P10-BE-01, RF-53 a RF-55).
 * CNPJ, CPF, CEP e telefone vão só com dígitos; e-mail em minúsculas; UF em maiúsculas.
 * Este arquivo cobre as duas chamadas do contrato: o cadastro (POST /api/empresas, usado na P10-FE-02)
 * e a verificação de CNPJ e e-mail (POST /api/empresas/verificar-cadastro, usada na P10-FE-01).
 */

/** Dados da empresa (a parte da etapa 1 do CadastroLocadoraVO). Opcionais vazios vão como null. */
export interface DadosEmpresaCadastro {
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  telefone: string;
  email: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string | null;
  bairro: string;
  cidade: string;
  uf: string;
}

/**
 * Corpo do POST /api/empresas (CadastroLocadoraVO). Junta os dados da empresa com o responsável e a senha
 * (etapa 2). O backend não aceita telefone do responsável nem campo de ativo, operadora ou id.
 */
export interface RequisicaoCadastroLocadora extends DadosEmpresaCadastro {
  responsavelNome: string;
  responsavelCpf: string;
  responsavelEmail: string;
  senha: string;
  aceiteTermos: boolean;
}

/** Resposta 201 do POST /api/empresas (ReturnEmpresaVO). Não traz senha, hash nem CPF completo. */
export interface RespostaEmpresa {
  idEmpresa: number;
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  cnpjStr: string;
  telefone: string;
  email: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string | null;
  bairro: string;
  cidade: string;
  uf: string;
  ativo: boolean;
  idFuncionarioResponsavel: number;
  responsavelNome: string;
  matriculaResponsavel: string;
  dataCadastroStr: string;
  /** E-mail de acesso do responsável, como gravado (minúsculas). O contrato sempre o devolve; a tela usa o digitado só se faltar. */
  responsavelEmail?: string;
}

/** Campo que a verificação consulta (enum texto `CampoVerificacaoEmpresa` do backend). */
export type CampoVerificacaoEmpresa = 'CNPJ' | 'EMAIL';

/** Corpo do POST /api/empresas/verificar-cadastro. O valor vai SEMPRE no corpo, nunca na URL. */
export interface RequisicaoVerificarEmpresa {
  campo: CampoVerificacaoEmpresa;
  valor: string;
}

/** Resposta 200 (ReturnVerificacaoCadastroEmpresaVO): só diz se já existe empresa com o valor. */
export interface RespostaVerificarEmpresa {
  existe: boolean;
}

/** Cidade e UF com pelo menos uma locadora ativa, conforme o contrato público de disponibilidade. */
export interface CidadeLocadora {
  cidade: string;
  uf: string;
}
