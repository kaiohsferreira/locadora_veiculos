/**
 * Validação do formulário de cadastro do cliente (P2-FE-02, RF-07), com Zod.
 *
 * Espelha o ClienteVOValidator do backend, com os mesmos textos (ConstantsMessage.Cliente.cs)
 * e os mesmos limites (ConstantsValues.Cliente.cs). O backend continua sendo a palavra final.
 *
 * O formulário guarda tudo como texto (é o que os campos devolvem). A conversão para o
 * corpo da API fica em `conversaoCadastro.ts`.
 *
 * "Hoje" é injetado (`agora`), para os testes não dependerem do relógio.
 */
import { z } from 'zod';
import { somenteDigitos } from '@/lib/formatters';
import {
  MENSAGEM_CADASTRO_CEP_INVALIDO,
  MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA,
  MENSAGEM_CADASTRO_CNH_NUMERO_OBRIGATORIO,
  MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA,
  MENSAGEM_CADASTRO_CNH_VENCIDA,
  MENSAGEM_CADASTRO_CPF_INVALIDO,
  MENSAGEM_CADASTRO_CPF_OBRIGATORIO,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO,
  MENSAGEM_CADASTRO_NOME_OBRIGATORIO,
  MENSAGEM_CADASTRO_SENHA_FRACA,
  MENSAGEM_CADASTRO_SENHA_OBRIGATORIA,
  MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
  MENSAGEM_CADASTRO_TELEFONE_INVALIDO,
  MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO,
  MENSAGEM_CADASTRO_UF_INVALIDA,
  mensagemCadastroCampoTamanho,
  mensagemCadastroCnhNumeroInvalido,
  mensagemCadastroMenorDeIdade,
  mensagemCadastroSenhaTamanho,
} from '@/lib/mensagens';
import {
  calcularIdade,
  ehCpfValido,
  ehDataIsoFutura,
  ehDataIsoPassada,
  ehDataIsoValida,
} from '@/lib/validadores';
import { CATEGORIA_CNH, type CategoriaCnh } from '@/types/enums';
import { ehSiglaUf } from './ufs';

/** Limites do backend (ConstantsValues.Cliente.cs). */
export const LIMITES_CADASTRO = {
  nome: 120,
  email: 120,
  logradouro: 120,
  numero: 10,
  complemento: 60,
  bairro: 60,
  cidade: 60,
  telefoneMinimo: 10,
  telefoneMaximo: 11,
  cnhNumero: 11,
  cep: 8,
  idadeMinima: 18,
  senhaMinima: 8,
  senhaMaxima: 64,
} as const;

/** Formato simples de e-mail: algo@dominio.tld, sem espaços. */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Valores do formulário, todos como texto ("" quando vazio). */
export interface FormularioCadastro {
  nome: string;
  cpf: string;
  dataNascimento: string;
  email: string;
  telefone: string;
  cnhNumero: string;
  /** Uma das categorias de CATEGORIA_CNH, ou "" antes de escolher. */
  cnhCategoria: string;
  cnhValidade: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  /** Sigla da UF, ou "" quando o endereço não foi informado. */
  uf: string;
  senha: string;
  /** Só no front: nunca vai para a API. */
  confirmarSenha: string;
}

/** Campos da etapa 1 (dados pessoais), como texto. */
export type DadosPessoaisCadastro = Pick<
  FormularioCadastro,
  'nome' | 'cpf' | 'dataNascimento' | 'email' | 'telefone'
>;

/** Campos da etapa 2 que podem ser guardados em memória entre as telas: CNH e endereço (nunca a senha). */
export type DocumentosCadastro = Pick<
  FormularioCadastro,
  | 'cnhNumero'
  | 'cnhCategoria'
  | 'cnhValidade'
  | 'cep'
  | 'logradouro'
  | 'numero'
  | 'complemento'
  | 'bairro'
  | 'cidade'
  | 'uf'
>;

/** Formulário da etapa 2: documentos mais a senha e a confirmação (a senha só existe nesta tela). */
export type FormularioDocumentos = DocumentosCadastro & Pick<FormularioCadastro, 'senha' | 'confirmarSenha'>;

/** Valores iniciais da etapa 1. */
export const DADOS_PESSOAIS_INICIAL: DadosPessoaisCadastro = {
  nome: '',
  cpf: '',
  dataNascimento: '',
  email: '',
  telefone: '',
};

/** Valores iniciais dos documentos (CNH e endereço), sem senha. */
export const DOCUMENTOS_INICIAL: DocumentosCadastro = {
  cnhNumero: '',
  cnhCategoria: '',
  cnhValidade: '',
  cep: '',
  logradouro: '',
  numero: '',
  complemento: '',
  bairro: '',
  cidade: '',
  uf: '',
};

/** A categoria existe em CATEGORIA_CNH (exige a grafia exata). */
export function ehCategoriaCnh(valor: string): valor is CategoriaCnh {
  return (CATEGORIA_CNH as readonly string[]).includes(valor);
}

/** Texto preenchido (com algo além de espaços). */
function temTexto(valor: string): boolean {
  return valor.trim().length > 0;
}

/** Texto opcional: vazio passa; preenchido não pode passar do máximo, sem contar as bordas. */
function caberNoLimite(valor: string, maximo: number): boolean {
  return valor.trim().length <= maximo;
}

/**
 * Campos da etapa 1 (dados pessoais). Cada campo para na primeira regra que falha (o resolver
 * mostra só o primeiro erro de cada campo), na mesma ordem do backend.
 */
function criarCamposDadosPessoais(agora: () => Date) {
  return {
    nome: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_NOME_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.nome), {
        error: mensagemCadastroCampoTamanho('nome', LIMITES_CADASTRO.nome),
      }),

    cpf: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_CPF_OBRIGATORIO })
      .refine(ehCpfValido, { error: MENSAGEM_CADASTRO_CPF_INVALIDO }),

    dataNascimento: z
      .string()
      .refine((valor) => valor !== '' && ehDataIsoValida(valor), {
        error: MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
      })
      .refine((valor) => !ehDataIsoFutura(valor, agora()), {
        error: MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
      })
      .refine((valor) => (calcularIdade(valor, agora()) ?? 0) >= LIMITES_CADASTRO.idadeMinima, {
        error: mensagemCadastroMenorDeIdade(LIMITES_CADASTRO.idadeMinima),
      }),

    email: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.email), {
        error: mensagemCadastroCampoTamanho('e-mail', LIMITES_CADASTRO.email),
      })
      .refine((valor) => REGEX_EMAIL.test(valor.trim()), { error: MENSAGEM_CADASTRO_EMAIL_INVALIDO }),

    telefone: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO })
      .refine((valor) => {
        const quantidade = somenteDigitos(valor).length;
        return (
          quantidade >= LIMITES_CADASTRO.telefoneMinimo &&
          quantidade <= LIMITES_CADASTRO.telefoneMaximo
        );
      }, { error: MENSAGEM_CADASTRO_TELEFONE_INVALIDO }),
  };
}

/** Campos da etapa 2 que não são senha: habilitação (CNH) e endereço opcional. */
function criarCamposDocumentos(agora: () => Date) {
  return {
    cnhNumero: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_CNH_NUMERO_OBRIGATORIO })
      .refine((valor) => somenteDigitos(valor).length === LIMITES_CADASTRO.cnhNumero, {
        error: mensagemCadastroCnhNumeroInvalido(LIMITES_CADASTRO.cnhNumero),
      }),

    cnhCategoria: z
      .string()
      .refine(ehCategoriaCnh, { error: MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA }),

    cnhValidade: z
      .string()
      .refine((valor) => valor !== '' && ehDataIsoValida(valor), {
        error: MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA,
      })
      .refine((valor) => !ehDataIsoPassada(valor, agora()), { error: MENSAGEM_CADASTRO_CNH_VENCIDA }),

    // Endereço (opcional): cada campo só é checado quando preenchido.
    cep: z
      .string()
      .refine((valor) => !temTexto(valor) || somenteDigitos(valor).length === LIMITES_CADASTRO.cep, {
        error: MENSAGEM_CADASTRO_CEP_INVALIDO,
      }),

    logradouro: z
      .string()
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.logradouro), {
        error: mensagemCadastroCampoTamanho('logradouro', LIMITES_CADASTRO.logradouro),
      }),

    numero: z
      .string()
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.numero), {
        error: mensagemCadastroCampoTamanho('número', LIMITES_CADASTRO.numero),
      }),

    complemento: z
      .string()
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.complemento), {
        error: mensagemCadastroCampoTamanho('complemento', LIMITES_CADASTRO.complemento),
      }),

    bairro: z
      .string()
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.bairro), {
        error: mensagemCadastroCampoTamanho('bairro', LIMITES_CADASTRO.bairro),
      }),

    cidade: z
      .string()
      .refine((valor) => caberNoLimite(valor, LIMITES_CADASTRO.cidade), {
        error: mensagemCadastroCampoTamanho('cidade', LIMITES_CADASTRO.cidade),
      }),

    uf: z
      .string()
      .refine((valor) => !temTexto(valor) || ehSiglaUf(valor), { error: MENSAGEM_CADASTRO_UF_INVALIDA }),
  };
}

/** Senha e confirmação (a confirmação só existe no front). */
function criarCamposSenha() {
  return {
    senha: z
      .string()
      .refine(temTexto, { error: MENSAGEM_CADASTRO_SENHA_OBRIGATORIA })
      .refine(
        (valor) => valor.length >= LIMITES_CADASTRO.senhaMinima && valor.length <= LIMITES_CADASTRO.senhaMaxima,
        {
          error: mensagemCadastroSenhaTamanho(LIMITES_CADASTRO.senhaMinima, LIMITES_CADASTRO.senhaMaxima),
        },
      )
      .refine((valor) => /\p{L}/u.test(valor) && /\p{Nd}/u.test(valor), {
        error: MENSAGEM_CADASTRO_SENHA_FRACA,
      }),

    confirmarSenha: z.string(),
  };
}

/** Regra do objeto: a confirmação tem de ser igual à senha. */
function confirmacaoConfereComSenha(dados: { senha: string; confirmarSenha: string }): boolean {
  return dados.confirmarSenha === dados.senha;
}

/**
 * Esquema do formulário completo (os 17 campos). Cada campo para na primeira regra que falha,
 * na mesma ordem do backend.
 *
 * A confirmação de senha é uma regra do objeto. Ela roda junto com os erros dos outros
 * campos (teste "documenta: a confirmação é checada mesmo com outro campo inválido"),
 * porque todos os campos são texto e nenhum erro de tipo aborta a validação.
 *
 * O cadastro é preenchido em duas telas (dados pessoais; documentos e senha), mas o envio final
 * sempre passa por este esquema completo.
 */
export function criarEsquemaCadastro(agora: () => Date = () => new Date()) {
  return z
    .object({
      ...criarCamposDadosPessoais(agora),
      ...criarCamposDocumentos(agora),
      ...criarCamposSenha(),
    })
    .refine(confirmacaoConfereComSenha, {
      error: MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
      path: ['confirmarSenha'],
    });
}

/** Esquema com o relógio real (usado pela tela). */
export const ESQUEMA_CADASTRO = criarEsquemaCadastro();

/** Esquema só da etapa 1 (dados pessoais): as mesmas regras do esquema completo para esses cinco campos. */
export function criarEsquemaDadosPessoais(agora: () => Date = () => new Date()) {
  return z.object(criarCamposDadosPessoais(agora));
}

/** Esquema da etapa 1 com o relógio real (usado pela tela). */
export const ESQUEMA_DADOS_PESSOAIS = criarEsquemaDadosPessoais();

/** Esquema da etapa 2 (habilitação, endereço opcional e senha), com as mesmas regras do esquema completo. */
export function criarEsquemaDocumentos(agora: () => Date = () => new Date()) {
  return z
    .object({
      ...criarCamposDocumentos(agora),
      ...criarCamposSenha(),
    })
    .refine(confirmacaoConfereComSenha, {
      error: MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
      path: ['confirmarSenha'],
    });
}

/** Esquema da etapa 2 com o relógio real (usado pela tela). */
export const ESQUEMA_DOCUMENTOS = criarEsquemaDocumentos();
