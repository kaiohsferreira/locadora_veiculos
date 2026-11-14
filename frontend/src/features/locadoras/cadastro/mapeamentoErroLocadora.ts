/**
 * Erros do envio do cadastro da locadora (POST /api/empresas) viram instruções para a tela: em qual etapa e
 * em qual campo mostrar, ou um aviso geral. Sem React e sem Axios.
 *
 * - 400: cada chave de `errors` (nome do CadastroLocadoraVO) vai para o campo do formulário, na etapa certa.
 * - 409: o texto diz qual conflito foi (CNPJ e e-mail da empresa são da etapa 1; CPF do responsável é da 2).
 *   O contrato não traz o campo em `errors` para o 409, então a leitura é pelo texto (ver relatório).
 * - 404 (cadastro público desligado), 429 (limite), falha de rede e demais: aviso geral com texto próprio.
 */
import { somenteDigitos } from '@/lib/formatters';
import {
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_LOCADORA_CADASTRO_INDISPONIVEL,
  MENSAGEM_LOCADORA_CNPJ_INVALIDO,
  MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO,
  MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO,
  MENSAGEM_LOCADORA_FALHA_ENVIO,
  MENSAGEM_LOCADORA_LIMITE_TENTATIVAS,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { RequisicaoCadastroLocadora } from '@/types/empresas';

/** Etapas que recebem erro de campo (a 3 não tem formulário). */
export type EtapaErroLocadora = 1 | 2;

/** Erro de um campo, já na etapa e com o nome do campo do formulário (não o da API). */
export interface ErroCampoLocadora {
  etapa: EtapaErroLocadora;
  campo: string;
  mensagem: string;
}

/** Campos da etapa 1 (dados da empresa), na grafia do CadastroLocadoraVO. */
const CAMPOS_EMPRESA_API = [
  'razaoSocial',
  'nomeFantasia',
  'cnpj',
  'telefone',
  'email',
  'cep',
  'logradouro',
  'numero',
  'complemento',
  'bairro',
  'cidade',
  'uf',
] as const;

/** Campos da etapa 2 (responsável e acesso): chave da API e campo do formulário. */
const CAMPOS_RESPONSAVEL_API: Readonly<Record<string, string>> = {
  responsavelNome: 'nome',
  responsavelCpf: 'cpf',
  responsavelEmail: 'email',
  senha: 'senha',
  aceiteTermos: 'aceiteTermos',
};

/** Chave da API (sem diferença de maiúsculas) para o campo da etapa. Null quando não é de nenhuma etapa. */
function destinoDaChaveApi(chave: string): { etapa: EtapaErroLocadora; campo: string } | null {
  const procurada = chave.toLowerCase();
  const empresa = CAMPOS_EMPRESA_API.find((campo) => campo.toLowerCase() === procurada);
  if (empresa !== undefined) return { etapa: 1, campo: empresa };
  const responsavel = Object.keys(CAMPOS_RESPONSAVEL_API).find((campo) => campo.toLowerCase() === procurada);
  if (responsavel !== undefined) return { etapa: 2, campo: CAMPOS_RESPONSAVEL_API[responsavel] };
  return null;
}

/** Resultado da leitura de um erro do envio. */
export interface ResultadoErroCadastroLocadora {
  /** Erros de campo a mostrar (vazio quando o erro é geral). */
  erros: ErroCampoLocadora[];
  /** Aviso geral para o alerta da tela (null quando há erros de campo). */
  mensagemGeral: string | null;
  /** Verdadeiro quando o erro de campo é de duplicidade (409): o valor já está em uso na plataforma. */
  emUso: boolean;
}

function resultadoComCampos(erros: ErroCampoLocadora[], emUso: boolean): ResultadoErroCadastroLocadora {
  return { erros, mensagemGeral: null, emUso };
}

function resultadoGeral(mensagem: string): ResultadoErroCadastroLocadora {
  return { erros: [], mensagemGeral: mensagem, emUso: false };
}

/** 409: pelo texto do conflito (CNPJ, CPF do responsável ou e-mail da empresa). */
function interpretarConflito(mensagem: string): ResultadoErroCadastroLocadora {
  if (mensagem.includes('CNPJ')) {
    return resultadoComCampos([{ etapa: 1, campo: 'cnpj', mensagem: MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO }], true);
  }
  if (mensagem.includes('CPF')) {
    return resultadoComCampos(
      [{ etapa: 2, campo: 'cpf', mensagem: MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO }],
      true,
    );
  }
  if (/e-?mail/i.test(mensagem)) {
    return resultadoComCampos([{ etapa: 1, campo: 'email', mensagem: MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO }], true);
  }
  return resultadoGeral(mensagem);
}

/**
 * 400: cada chave conhecida vira um erro de campo; chaves desconhecidas são ignoradas. Sem `errors`, o CNPJ recusado
 * no serviço (ex.: o da empresa operadora) volta para a etapa 1, no campo CNPJ, como o 409 de CNPJ.
 */
function interpretarValidacao(erro: ErroApi): ResultadoErroCadastroLocadora {
  const erros: ErroCampoLocadora[] = [];
  for (const [chave, mensagens] of Object.entries(erro.erros ?? {})) {
    const destino = destinoDaChaveApi(chave);
    if (destino === null || mensagens.length === 0) continue;
    erros.push({ etapa: destino.etapa, campo: destino.campo, mensagem: mensagens[0] });
  }
  if (erros.length > 0) return resultadoComCampos(erros, false);
  if (erro.mensagem.includes('CNPJ')) {
    return resultadoComCampos([{ etapa: 1, campo: 'cnpj', mensagem: MENSAGEM_LOCADORA_CNPJ_INVALIDO }], false);
  }
  return resultadoGeral(erro.mensagem);
}

/**
 * Lê o erro do envio e diz o que mostrar. Qualquer coisa que não seja `ErroApi` vira a mensagem padrão.
 * Nunca lança e nunca devolve a mensagem técnica do backend para o usuário.
 */
export function interpretarErroCadastroLocadora(erro: unknown): ResultadoErroCadastroLocadora {
  if (!(erro instanceof ErroApi)) return resultadoGeral(MENSAGEM_ERRO_API_PADRAO);
  switch (erro.status) {
    case 0:
      return resultadoGeral(MENSAGEM_LOCADORA_FALHA_ENVIO);
    case 400:
      return interpretarValidacao(erro);
    case 404:
      return resultadoGeral(MENSAGEM_LOCADORA_CADASTRO_INDISPONIVEL);
    case 409:
      return interpretarConflito(erro.mensagem);
    case 429:
      return resultadoGeral(MENSAGEM_LOCADORA_LIMITE_TENTATIVAS);
    default:
      return resultadoGeral(erro.mensagem);
  }
}

/** Valores em conflito que a etapa 1 guarda para não avançar com o mesmo CNPJ ou e-mail já recusado. */
export interface ConflitosEmpresaLocadora {
  cnpj?: string;
  email?: string;
}

/**
 * Conflitos a guardar depois de um 409 de CNPJ ou e-mail, já normalizados como o backend grava (CNPJ só
 * dígitos; e-mail em minúsculas). Só o valor do envio que foi recusado entra aqui.
 */
export function conflitosDoEnvio(
  resultado: ResultadoErroCadastroLocadora,
  requisicao: RequisicaoCadastroLocadora,
): ConflitosEmpresaLocadora {
  const conflitos: ConflitosEmpresaLocadora = {};
  if (!resultado.emUso) return conflitos;
  if (resultado.erros.some((erro) => erro.etapa === 1 && erro.campo === 'cnpj')) {
    conflitos.cnpj = somenteDigitos(requisicao.cnpj);
  }
  if (resultado.erros.some((erro) => erro.etapa === 1 && erro.campo === 'email')) {
    conflitos.email = requisicao.email.trim().toLowerCase();
  }
  return conflitos;
}

/**
 * Campos da etapa 1 cujo valor digitado ainda é o que o servidor recusou. A etapa 1 não avança com eles,
 * mesmo que a verificação anterior tenha dito "livre" (o 409 do envio é a autoridade).
 */
export function camposEmConflitoLocadora(
  valores: { cnpj: string; email: string },
  conflitos: ConflitosEmpresaLocadora,
): ('cnpj' | 'email')[] {
  const emConflito: ('cnpj' | 'email')[] = [];
  if (conflitos.cnpj !== undefined && somenteDigitos(valores.cnpj) === conflitos.cnpj) emConflito.push('cnpj');
  if (conflitos.email !== undefined && valores.email.trim().toLowerCase() === conflitos.email) {
    emConflito.push('email');
  }
  return emConflito;
}

/**
 * Erros pendentes (vindos do envio) que a tela de uma etapa deve aplicar agora. A etapa 1 aplica os seus.
 * A etapa 2 só aplica os seus quando não resta erro da etapa 1: a pessoa resolve a etapa 1 primeiro.
 */
export function errosDaEtapaLocadora(
  etapa: EtapaErroLocadora,
  pendentes: readonly ErroCampoLocadora[],
): ErroCampoLocadora[] {
  const daEtapa1 = pendentes.filter((erro) => erro.etapa === 1);
  if (etapa === 1) return daEtapa1;
  return daEtapa1.length > 0 ? [] : pendentes.filter((erro) => erro.etapa === 2);
}
