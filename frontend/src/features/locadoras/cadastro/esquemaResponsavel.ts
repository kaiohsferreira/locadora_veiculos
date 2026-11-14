/**
 * Validação da etapa 2 do cadastro da locadora (responsável e acesso, tela 37), com Zod.
 *
 * Espelha o validador do responsável no backend (contrato empresa v1): mesmos limites e os mesmos textos
 * (ConstantsMessage.Locadora.cs). A senha do responsável segue a tela 37: 8 a 64 caracteres, com uma letra
 * MAIÚSCULA e um número. Não é a regra do cliente (que não exige maiúscula), por decisão do dono do produto.
 *
 * A senha existe só neste formulário e na chamada de envio. Nunca entra em estado persistido: ver
 * `extrairDadosResponsavel`, que devolve tudo menos a senha e a confirmação.
 */
import { z } from 'zod';
import {
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_SENHA_OBRIGATORIA,
  MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
  mensagemCadastroCampoTamanho,
  mensagemCadastroSenhaTamanho,
  MENSAGEM_LOCADORA_ACEITE_TERMOS_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_EMAIL_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO,
  MENSAGEM_LOCADORA_SENHA_FRACA,
} from '@/lib/mensagens';
import { ehCpfValido } from '@/lib/validadores';

/** Limites do backend para o responsável (ConstantsValues.Locadora.cs, contrato empresa v1). */
export const LIMITES_RESPONSAVEL = {
  nome: 120,
  email: 120,
  senhaMinima: 8,
  senhaMaxima: 64,
} as const;

/** Formato simples de e-mail: algo@dominio.tld, sem espaços (o backend valida de novo). */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Valores do formulário da etapa 2. A senha e a confirmação só existem aqui. */
export interface FormularioResponsavel {
  nome: string;
  cpf: string;
  email: string;
  senha: string;
  /** Só no front: nunca vai para a API. */
  confirmarSenha: string;
  aceiteTermos: boolean;
}

/** Parte da etapa 2 que pode ficar em memória entre as telas (sem senha). */
export type DadosResponsavelLocadora = Omit<FormularioResponsavel, 'senha' | 'confirmarSenha'>;

/** Valores iniciais da etapa 2 (aceite desmarcado: o aceite dos termos é uma escolha explícita). */
export const DADOS_RESPONSAVEL_INICIAL: DadosResponsavelLocadora = {
  nome: '',
  cpf: '',
  email: '',
  aceiteTermos: false,
};

/** Só os dados que podem ficar em memória (nunca a senha nem a confirmação). */
export function extrairDadosResponsavel(valores: FormularioResponsavel): DadosResponsavelLocadora {
  return {
    nome: valores.nome,
    cpf: valores.cpf,
    email: valores.email,
    aceiteTermos: valores.aceiteTermos,
  };
}

/** Texto preenchido (com algo além de espaços). */
function temTexto(valor: string): boolean {
  return valor.trim().length > 0;
}

/** Texto não pode passar do máximo, sem contar as bordas. */
function caberNoLimite(valor: string, maximo: number): boolean {
  return valor.trim().length <= maximo;
}

/** Regra da senha: ao menos uma letra MAIÚSCULA e um número. */
function temMaiusculaENumero(valor: string): boolean {
  return /\p{Lu}/u.test(valor) && /\p{Nd}/u.test(valor);
}

/** Confirmação igual à senha (regra do objeto). */
function confirmacaoConfereComSenha(dados: { senha: string; confirmarSenha: string }): boolean {
  return dados.confirmarSenha === dados.senha;
}

/**
 * Esquema da etapa 2. Cada campo para na primeira regra que falha (o resolver mostra só o primeiro erro de
 * cada campo). A confirmação é uma regra do objeto, então roda junto com os outros campos.
 */
export function criarEsquemaResponsavel() {
  return z
    .object({
      nome: z
        .string()
        .refine(temTexto, { error: MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO })
        .refine((valor) => caberNoLimite(valor, LIMITES_RESPONSAVEL.nome), {
          error: mensagemCadastroCampoTamanho('nome', LIMITES_RESPONSAVEL.nome),
        }),

      cpf: z
        .string()
        .refine(temTexto, { error: MENSAGEM_LOCADORA_RESPONSAVEL_CPF_OBRIGATORIO })
        .refine(ehCpfValido, { error: MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO }),

      email: z
        .string()
        .refine(temTexto, { error: MENSAGEM_LOCADORA_RESPONSAVEL_EMAIL_OBRIGATORIO })
        .refine((valor) => caberNoLimite(valor, LIMITES_RESPONSAVEL.email), {
          error: mensagemCadastroCampoTamanho('e-mail', LIMITES_RESPONSAVEL.email),
        })
        .refine((valor) => REGEX_EMAIL.test(valor.trim()), { error: MENSAGEM_CADASTRO_EMAIL_INVALIDO }),

      senha: z
        .string()
        .refine(temTexto, { error: MENSAGEM_CADASTRO_SENHA_OBRIGATORIA })
        .refine(
          (valor) => valor.length >= LIMITES_RESPONSAVEL.senhaMinima && valor.length <= LIMITES_RESPONSAVEL.senhaMaxima,
          {
            error: mensagemCadastroSenhaTamanho(LIMITES_RESPONSAVEL.senhaMinima, LIMITES_RESPONSAVEL.senhaMaxima),
          },
        )
        .refine(temMaiusculaENumero, { error: MENSAGEM_LOCADORA_SENHA_FRACA }),

      confirmarSenha: z.string(),

      aceiteTermos: z.boolean().refine((valor) => valor === true, {
        error: MENSAGEM_LOCADORA_ACEITE_TERMOS_OBRIGATORIO,
      }),
    })
    .refine(confirmacaoConfereComSenha, {
      error: MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
      path: ['confirmarSenha'],
    });
}

/** Esquema da etapa 2 com as regras reais (usado pela tela). */
export const ESQUEMA_RESPONSAVEL = criarEsquemaResponsavel();

/** Campos do formulário da etapa 2, na ordem da tela. */
export const CAMPOS_RESPONSAVEL = [
  'nome',
  'cpf',
  'email',
  'senha',
  'confirmarSenha',
  'aceiteTermos',
] as const satisfies readonly (keyof FormularioResponsavel)[];

/** O nome vem do formulário e é um campo da etapa 2 (guarda de tipo). */
export function ehCampoResponsavel(campo: string): campo is keyof FormularioResponsavel {
  return (CAMPOS_RESPONSAVEL as readonly string[]).includes(campo);
}
