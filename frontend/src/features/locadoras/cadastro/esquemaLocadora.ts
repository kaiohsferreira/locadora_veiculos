/**
 * Validação da etapa 1 do cadastro da locadora (dados da empresa, P10-FE-01, RF-53), com Zod.
 *
 * Espelha o CadastroLocadoraVOValidator do backend, com os mesmos limites (contrato empresa v1) e os
 * mesmos textos (ConstantsMessage.Locadora). O backend continua sendo a palavra final.
 * O formulário guarda tudo como texto (é o que os campos devolvem). A conversão para o corpo da API
 * fica em `conversaoLocadora.ts`. A senha não é desta etapa.
 */
import { z } from 'zod';
import { somenteDigitos } from '@/lib/formatters';
import {
  MENSAGEM_CADASTRO_CEP_INVALIDO,
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_TELEFONE_INVALIDO,
  MENSAGEM_LOCADORA_BAIRRO_OBRIGATORIO,
  MENSAGEM_LOCADORA_CEP_OBRIGATORIO,
  MENSAGEM_LOCADORA_CIDADE_OBRIGATORIA,
  MENSAGEM_LOCADORA_CNPJ_INVALIDO,
  MENSAGEM_LOCADORA_CNPJ_OBRIGATORIO,
  MENSAGEM_LOCADORA_EMAIL_OBRIGATORIO,
  MENSAGEM_LOCADORA_LOGRADOURO_OBRIGATORIO,
  MENSAGEM_LOCADORA_NOME_FANTASIA_OBRIGATORIO,
  MENSAGEM_LOCADORA_NUMERO_OBRIGATORIO,
  MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA,
  MENSAGEM_LOCADORA_TELEFONE_OBRIGATORIO,
  MENSAGEM_LOCADORA_UF_INVALIDA,
  MENSAGEM_LOCADORA_UF_OBRIGATORIA,
  mensagemCadastroCampoTamanho,
} from '@/lib/mensagens';
import { ehCnpjValido } from '@/lib/validadores';
import { ehSiglaUf } from '@/features/clientes/cadastro/ufs';

/** Limites do backend (ConstantsValues.Locadora.cs, contrato empresa v1). */
export const LIMITES_EMPRESA = {
  razaoSocial: 150,
  nomeFantasia: 100,
  email: 120,
  telefoneMinimo: 10,
  telefoneMaximo: 11,
  cep: 8,
  cnpj: 14,
  logradouro: 120,
  numero: 10,
  complemento: 60,
  bairro: 60,
  cidade: 60,
} as const;

/** Formato simples de e-mail: algo@dominio.tld, sem espaços (o backend valida de novo). */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Valores do formulário da etapa 1, todos como texto ("" quando vazio). */
export interface FormularioEmpresa {
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  telefone: string;
  email: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  /** Sigla da UF, ou "" antes de escolher. */
  uf: string;
}

/** Todos os campos da etapa 1, na ordem da tela. */
export const CAMPOS_EMPRESA = [
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
] as const satisfies readonly (keyof FormularioEmpresa)[];

/** Valores iniciais da etapa 1. */
export const DADOS_EMPRESA_INICIAL: FormularioEmpresa = {
  razaoSocial: '',
  nomeFantasia: '',
  cnpj: '',
  telefone: '',
  email: '',
  cep: '',
  logradouro: '',
  numero: '',
  complemento: '',
  bairro: '',
  cidade: '',
  uf: '',
};

/** Texto preenchido (com algo além de espaços). */
function temTexto(valor: string): boolean {
  return valor.trim().length > 0;
}

/** Texto opcional ou obrigatório: não pode passar do máximo, sem contar as bordas. */
function caberNoLimite(valor: string, maximo: number): boolean {
  return valor.trim().length <= maximo;
}

/**
 * Esquema da etapa 1. Cada campo para na primeira regra que falha (o resolver mostra só o primeiro erro
 * de cada campo), na mesma ordem do backend.
 */
export function criarEsquemaEmpresa() {
  return z.object({
    razaoSocial: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.razaoSocial), {
        error: mensagemCadastroCampoTamanho('razão social', LIMITES_EMPRESA.razaoSocial),
      }),

    nomeFantasia: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_NOME_FANTASIA_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.nomeFantasia), {
        error: mensagemCadastroCampoTamanho('nome fantasia', LIMITES_EMPRESA.nomeFantasia),
      }),

    cnpj: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_CNPJ_OBRIGATORIO })
      .refine(ehCnpjValido, { error: MENSAGEM_LOCADORA_CNPJ_INVALIDO }),

    telefone: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_TELEFONE_OBRIGATORIO })
      .refine(
        (valor) => {
          const quantidade = somenteDigitos(valor).length;
          return quantidade >= LIMITES_EMPRESA.telefoneMinimo && quantidade <= LIMITES_EMPRESA.telefoneMaximo;
        },
        { error: MENSAGEM_CADASTRO_TELEFONE_INVALIDO },
      ),

    email: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_EMAIL_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.email), {
        error: mensagemCadastroCampoTamanho('e-mail', LIMITES_EMPRESA.email),
      })
      .refine((valor) => REGEX_EMAIL.test(valor.trim()), { error: MENSAGEM_CADASTRO_EMAIL_INVALIDO }),

    cep: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_CEP_OBRIGATORIO })
      .refine((valor) => somenteDigitos(valor).length === LIMITES_EMPRESA.cep, {
        error: MENSAGEM_CADASTRO_CEP_INVALIDO,
      }),

    logradouro: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_LOGRADOURO_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.logradouro), {
        error: mensagemCadastroCampoTamanho('endereço', LIMITES_EMPRESA.logradouro),
      }),

    numero: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_NUMERO_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.numero), {
        error: mensagemCadastroCampoTamanho('número', LIMITES_EMPRESA.numero),
      }),

    // Complemento é opcional: vazio passa; preenchido não pode passar do máximo.
    complemento: z.string().refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.complemento), {
      error: mensagemCadastroCampoTamanho('complemento', LIMITES_EMPRESA.complemento),
    }),

    bairro: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_BAIRRO_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.bairro), {
        error: mensagemCadastroCampoTamanho('bairro', LIMITES_EMPRESA.bairro),
      }),

    cidade: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_CIDADE_OBRIGATORIA })
      .refine((valor) => caberNoLimite(valor, LIMITES_EMPRESA.cidade), {
        error: mensagemCadastroCampoTamanho('cidade', LIMITES_EMPRESA.cidade),
      }),

    uf: z
      .string()
      .refine(temTexto, { error: MENSAGEM_LOCADORA_UF_OBRIGATORIA })
      .refine(ehSiglaUf, { error: MENSAGEM_LOCADORA_UF_INVALIDA }),
  });
}

/** Esquema da etapa 1 com as regras reais (usado pela tela e pela verificação ao sair do campo). */
export const ESQUEMA_EMPRESA = criarEsquemaEmpresa();

/** O CNPJ tem o formato e os dígitos verificadores certos (sem consultar a Receita). */
export function calcularCnpjValido(valor: string): boolean {
  return ESQUEMA_EMPRESA.shape.cnpj.safeParse(valor).success;
}
