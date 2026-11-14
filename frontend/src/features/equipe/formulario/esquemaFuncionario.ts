/**
 * Validação do formulário de funcionário (adicionar e editar), com Zod. Espelha os limites do contrato
 * (FuncionarioVO e FuncionarioEdicaoVO); o backend valida de novo e é a palavra final.
 *
 * A regra da senha inicial (8 a 64 caracteres, uma letra maiúscula e um número) é a mesma da tela de cadastro da
 * locadora, mas foi escrita aqui de propósito: a equipe não importa nada de `features/locadoras`.
 *
 * Na edição não existem CPF nem senha: o esquema deixa esses campos sem regra, e a conversão não os envia.
 */
import { z } from 'zod';
import { dataBrParaIso } from '@/lib/datasBr';
import { ehCpfValido, ehDataIsoFutura } from '@/lib/validadores';
import {
  MENSAGEM_EQUIPE_ADMISSAO_FORMATO,
  MENSAGEM_EQUIPE_ADMISSAO_FUTURA,
  MENSAGEM_EQUIPE_ADMISSAO_OBRIGATORIA,
  MENSAGEM_EQUIPE_CARGO_OBRIGATORIO,
  MENSAGEM_EQUIPE_CONFIRMACAO_SENHA,
  MENSAGEM_EQUIPE_CPF_INVALIDO,
  MENSAGEM_EQUIPE_CPF_OBRIGATORIO,
  MENSAGEM_EQUIPE_EMAIL_INVALIDO,
  MENSAGEM_EQUIPE_EMAIL_OBRIGATORIO,
  MENSAGEM_EQUIPE_MATRICULA_OBRIGATORIA,
  MENSAGEM_EQUIPE_NOME_OBRIGATORIO,
  MENSAGEM_EQUIPE_SENHA_FRACA,
  MENSAGEM_EQUIPE_SENHA_OBRIGATORIA,
  MENSAGEM_EQUIPE_SENHA_TAMANHO,
  mensagemEquipeCampoTamanho,
} from '@/lib/mensagens';
import { CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';

/** Limites do contrato (ConstantsValues.Equipe). */
export const LIMITES_FUNCIONARIO = {
  nome: 120,
  matricula: 20,
  email: 120,
  senhaMinima: 8,
  senhaMaxima: 64,
} as const;

/** Formato simples de e-mail: algo@dominio.tld, sem espaços. */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Modo do formulário: adicionar (com CPF e senha) ou editar (sem CPF e sem senha). */
export type ModoFormularioFuncionario = 'cadastro' | 'edicao';

/**
 * Valores do formulário. `dataAdmissao` é o texto digitado (DD/MM/AAAA); `cpf` só existe no cadastro, e
 * `senhaInicial` e `confirmarSenha` também (na edição ficam vazios e não são enviados).
 */
export interface FormularioFuncionario {
  nome: string;
  cpf: string;
  matricula: string;
  email: string;
  dataAdmissao: string;
  cargo: CargoFuncionario;
  senhaInicial: string;
  /** Só no front: nunca vai para a API. */
  confirmarSenha: string;
}

/** Texto preenchido, com algo além de espaços. */
function temTexto(valor: string): boolean {
  return valor.trim().length > 0;
}

/** Texto aparado cabe no limite. */
function caberNoLimite(valor: string, maximo: number): boolean {
  return valor.trim().length <= maximo;
}

/** Senha com ao menos uma letra maiúscula e um número. */
function temMaiusculaENumero(valor: string): boolean {
  return /\p{Lu}/u.test(valor) && /\p{Nd}/u.test(valor);
}

/** Data DD/MM/AAAA válida que não é futura. Texto incompleto não é julgado aqui (outra regra cuida). */
function naoEFutura(valor: string): boolean {
  const iso = dataBrParaIso(valor);
  return iso === null || !ehDataIsoFutura(iso);
}

/** Regra da data de admissão: obrigatória, DD/MM/AAAA real e não futura. */
const regraDataAdmissao = z
  .string()
  .refine(temTexto, { error: MENSAGEM_EQUIPE_ADMISSAO_OBRIGATORIA })
  .refine((valor) => !temTexto(valor) || dataBrParaIso(valor) !== null, { error: MENSAGEM_EQUIPE_ADMISSAO_FORMATO })
  .refine(naoEFutura, { error: MENSAGEM_EQUIPE_ADMISSAO_FUTURA });

/** Regra do e-mail: obrigatório, até 120 caracteres e formato simples. */
const regraEmail = z
  .string()
  .refine(temTexto, { error: MENSAGEM_EQUIPE_EMAIL_OBRIGATORIO })
  .refine((valor) => caberNoLimite(valor, LIMITES_FUNCIONARIO.email), {
    error: mensagemEquipeCampoTamanho('e-mail', LIMITES_FUNCIONARIO.email),
  })
  .refine((valor) => REGEX_EMAIL.test(valor.trim()), { error: MENSAGEM_EQUIPE_EMAIL_INVALIDO });

/** Regra da senha inicial: 8 a 64 caracteres, com maiúscula e número. Só existe no cadastro. */
const regraSenhaInicial = z
  .string()
  .refine(temTexto, { error: MENSAGEM_EQUIPE_SENHA_OBRIGATORIA })
  .refine(
    (valor) => valor.length >= LIMITES_FUNCIONARIO.senhaMinima && valor.length <= LIMITES_FUNCIONARIO.senhaMaxima,
    { error: MENSAGEM_EQUIPE_SENHA_TAMANHO },
  )
  .refine(temMaiusculaENumero, { error: MENSAGEM_EQUIPE_SENHA_FRACA });

/**
 * Esquema do formulário no modo pedido. No cadastro, CPF e senha são obrigatórios e a confirmação deve ser igual
 * à senha. Na edição, esses campos não têm regra. Cada campo para na primeira regra que falha.
 */
export function criarEsquemaFuncionario(modo: ModoFormularioFuncionario) {
  const cadastro = modo === 'cadastro';

  const esquema = z.object({
    nome: z
      .string()
      .refine(temTexto, { error: MENSAGEM_EQUIPE_NOME_OBRIGATORIO })
      .refine((valor) => caberNoLimite(valor, LIMITES_FUNCIONARIO.nome), {
        error: mensagemEquipeCampoTamanho('nome', LIMITES_FUNCIONARIO.nome),
      }),

    cpf: cadastro
      ? z
          .string()
          .refine(temTexto, { error: MENSAGEM_EQUIPE_CPF_OBRIGATORIO })
          .refine(ehCpfValido, { error: MENSAGEM_EQUIPE_CPF_INVALIDO })
      : z.string(),

    matricula: z
      .string()
      .refine(temTexto, { error: MENSAGEM_EQUIPE_MATRICULA_OBRIGATORIA })
      .refine((valor) => caberNoLimite(valor, LIMITES_FUNCIONARIO.matricula), {
        error: mensagemEquipeCampoTamanho('matrícula', LIMITES_FUNCIONARIO.matricula),
      }),

    email: regraEmail,

    dataAdmissao: regraDataAdmissao,

    cargo: z.enum(CARGO_FUNCIONARIO, { error: MENSAGEM_EQUIPE_CARGO_OBRIGATORIO }),

    senhaInicial: cadastro ? regraSenhaInicial : z.string(),

    confirmarSenha: z.string(),
  });

  if (!cadastro) return esquema;

  return esquema.refine((dados) => dados.confirmarSenha === dados.senhaInicial, {
    error: MENSAGEM_EQUIPE_CONFIRMACAO_SENHA,
    path: ['confirmarSenha'],
  });
}

/** Esquema do cadastro (com CPF e senha). */
export const ESQUEMA_CADASTRO_FUNCIONARIO = criarEsquemaFuncionario('cadastro');

/** Esquema da edição (sem CPF e sem senha). */
export const ESQUEMA_EDICAO_FUNCIONARIO = criarEsquemaFuncionario('edicao');

/** Campos do formulário que podem receber erro do servidor (400 e 409), na ordem da tela. */
export const CAMPOS_FORMULARIO_FUNCIONARIO = [
  'nome',
  'cpf',
  'matricula',
  'email',
  'dataAdmissao',
  'cargo',
  'senhaInicial',
] as const;

/** Um dos campos do formulário que recebem erro do servidor. */
export type CampoFormularioFuncionario = (typeof CAMPOS_FORMULARIO_FUNCIONARIO)[number];

/** Guarda de tipo: o nome vindo do servidor é um campo conhecido do formulário. */
export function ehCampoFormularioFuncionario(campo: string): campo is CampoFormularioFuncionario {
  return (CAMPOS_FORMULARIO_FUNCIONARIO as readonly string[]).includes(campo);
}
