/**
 * Lógica pura da verificação de cadastro ao sair do campo (CPF, e-mail e CNH já cadastrados).
 * Sem React e sem Axios: normaliza o valor como o backend, decide se vale a pena consultar, descarta
 * resposta de valor antigo e combina o resultado com o ícone de "conferido" da etapa 1.
 *
 * A verificação nunca bloqueia o cadastro por falha (rede, 5xx, 429, 400): só `existe` bloqueia, e o
 * 409 do envio continua sendo a autoridade.
 */
import { somenteDigitos } from '@/lib/formatters';
import {
  MENSAGEM_CADASTRO_CNH_JA_CADASTRADA,
  MENSAGEM_CADASTRO_CPF_JA_CADASTRADO,
  MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO,
} from '@/lib/mensagens';
import type { CampoVerificacaoCadastro } from '@/types/clientes';
import { ESQUEMA_CADASTRO } from './esquemaCadastro';
import type { CamposValidosDadosPessoais } from './etapasCadastro';

/** Campos do formulário que a verificação cobre (CPF e e-mail na etapa 1; CNH na etapa 2). */
export type CampoVerificavel = 'cpf' | 'email' | 'cnhNumero';

/** Resultado de uma verificação: `indisponivel` = falhou (rede, 5xx, 429, 400, cancelada); nunca bloqueia. */
export type ResultadoVerificacao = 'existe' | 'livre' | 'indisponivel';

/** Situação de um campo na tela: sem verificação do valor atual, em andamento ou com resultado. */
export type EstadoVerificacao = 'nao-verificado' | 'verificando' | ResultadoVerificacao;

/** O que a tela lembra de um campo: para qual valor (já normalizado) a verificação vale e em que ponto está. */
export interface RegistroVerificacao {
  valor: string;
  estado: 'verificando' | ResultadoVerificacao;
}

/** Primeiro item da chave de consulta do TanStack Query; a família inteira é limpa ao sair do cadastro. */
export const CHAVE_VERIFICACAO_CADASTRO = 'verificar-cadastro';

/** Tipo do erro de campo que a verificação põe no formulário (a tela o limpa ao editar o campo). */
export const TIPO_ERRO_DUPLICADO = 'duplicado';

/** Espera depois da última tecla do CPF antes de verificar um CPF que ficou válido. */
export const ATRASO_VERIFICACAO_CPF_MS = 300;

/** Quanto tempo um resultado fica na memória: não repete a pergunta do mesmo valor nesse intervalo (5 minutos). */
export const TEMPO_MEMORIA_VERIFICACAO_MS = 5 * 60 * 1000;

/** Opções fixas da consulta de verificação: sem nova tentativa automática; resultado fica 5 minutos em memória. */
export const OPCOES_CONSULTA_VERIFICACAO = {
  retry: false,
  staleTime: TEMPO_MEMORIA_VERIFICACAO_MS,
  gcTime: TEMPO_MEMORIA_VERIFICACAO_MS,
} as const;

/** Lê a resposta 200 do contrato: `{ existe: true }` é `existe`, `{ existe: false }` é `livre`; o resto é `indisponivel`. */
export function interpretarRespostaVerificacao(resposta: unknown): ResultadoVerificacao {
  if (typeof resposta !== 'object' || resposta === null) return 'indisponivel';
  const existe = (resposta as { existe?: unknown }).existe;
  if (existe === true) return 'existe';
  if (existe === false) return 'livre';
  return 'indisponivel';
}

/** Resultado da chamada protegida; `respostaInvalida` = a API respondeu, mas fora do contrato (não deve ficar em cache). */
export interface VerificacaoComFalhaAberta {
  resultado: ResultadoVerificacao;
  respostaInvalida: boolean;
}

/**
 * Falha aberta: qualquer erro de `buscar` (rede, 5xx, 429, 400, cancelamento) vira `indisponivel`, sem lançar.
 * Se a chamada resolver, interpreta a resposta; resposta fora do contrato também vira `indisponivel`.
 */
export async function verificarComFalhaAberta(
  buscar: () => Promise<unknown>,
): Promise<VerificacaoComFalhaAberta> {
  let resposta: unknown;
  try {
    resposta = await buscar();
  } catch {
    return { resultado: 'indisponivel', respostaInvalida: false };
  }
  const resultado = interpretarRespostaVerificacao(resposta);
  return { resultado, respostaInvalida: resultado === 'indisponivel' };
}

/** Campo do formulário para o campo do contrato da API. */
export const CAMPO_API_POR_CAMPO_FORMULARIO: Readonly<Record<CampoVerificavel, CampoVerificacaoCadastro>> = {
  cpf: 'CPF',
  email: 'EMAIL',
  cnhNumero: 'CNH',
};

/** Mensagem de duplicidade por campo (mesmo texto do backend). */
export function mensagemDeDuplicidade(campo: CampoVerificavel): string {
  switch (campo) {
    case 'cpf':
      return MENSAGEM_CADASTRO_CPF_JA_CADASTRADO;
    case 'email':
      return MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO;
    case 'cnhNumero':
      return MENSAGEM_CADASTRO_CNH_JA_CADASTRADA;
  }
}

/** Mesma forma gravada pelo backend: CPF e CNH só dígitos; e-mail sem espaços nas pontas e em minúsculas. */
export function normalizarValorVerificacao(campo: CampoVerificavel, valor: string): string {
  if (campo === 'email') return valor.trim().toLowerCase();
  return somenteDigitos(valor);
}

/**
 * Valor normalizado a consultar, ou null quando o valor ainda não passa no esquema do próprio campo
 * (vazio, CPF inválido, CNH curta, e-mail sem formato...): valor malformado nunca vai para a API.
 */
export function valorVerificavel(campo: CampoVerificavel, valor: string): string | null {
  if (!ESQUEMA_CADASTRO.shape[campo].safeParse(valor).success) return null;
  return normalizarValorVerificacao(campo, valor);
}

/**
 * Resultado conhecido do valor atual do campo. Null quando não há registro, quando a verificação ainda
 * está em andamento ou quando o registro é de outro valor (resposta de valor antigo é descartada).
 */
export function resultadoVigente(
  registro: RegistroVerificacao | undefined,
  campo: CampoVerificavel,
  valorAtual: string,
): ResultadoVerificacao | null {
  if (registro === undefined || registro.estado === 'verificando') return null;
  return registro.valor === normalizarValorVerificacao(campo, valorAtual) ? registro.estado : null;
}

/** Situação do campo para o valor atual; registro de outro valor conta como "não verificado". */
export function estadoVigente(
  registro: RegistroVerificacao | undefined,
  campo: CampoVerificavel,
  valorAtual: string,
): EstadoVerificacao {
  if (registro === undefined) return 'nao-verificado';
  if (registro.valor !== normalizarValorVerificacao(campo, valorAtual)) return 'nao-verificado';
  return registro.estado;
}

/**
 * O `continuar` precisa verificar este campo agora? Sim quando não há registro do valor atual (sem
 * registro ou registro de valor antigo) e quando a tentativa anterior do valor atual falhou
 * (`indisponivel`: uma nova tentativa antes de decidir). Não quando já é `livre` ou `existe`, nem
 * quando há uma verificação em andamento (a chamada em andamento é aproveitada).
 */
export function precisaVerificarAgora(
  registro: RegistroVerificacao | undefined,
  campo: CampoVerificavel,
  valorAtual: string,
): boolean {
  const estado = estadoVigente(registro, campo, valorAtual);
  return estado === 'nao-verificado' || estado === 'indisponivel';
}

/** Situação da verificação dos campos da etapa 1 que mostram o ícone de "conferido". */
export interface EstadosVerificacaoDadosPessoais {
  cpf: EstadoVerificacao;
  email: EstadoVerificacao;
}

/**
 * Junta o "valor válido" do esquema com a verificação, para o ícone de "conferido" de CPF e e-mail:
 * o ícone significa "conferido com o servidor": só aparece se o esquema aprova E a verificação do valor
 * atual terminou em `livre` ou `indisponivel`. Sem ícone se o esquema rejeita, se ainda não foi
 * verificado, enquanto verifica ou se já existe. Os outros campos ficam como estão (só pelo esquema).
 */
export function combinarValidosComVerificacao(
  validos: CamposValidosDadosPessoais,
  estados: EstadosVerificacaoDadosPessoais,
): CamposValidosDadosPessoais {
  const confere = (valido: boolean, estado: EstadoVerificacao): boolean =>
    valido && (estado === 'livre' || estado === 'indisponivel');
  return {
    ...validos,
    cpf: confere(validos.cpf, estados.cpf),
    email: confere(validos.email, estados.email),
  };
}

/** Só `existe` bloqueia o avanço (e o envio). Falha, sem resultado ou valor livre seguem. */
export function deveBloquearContinuar(resultado: ResultadoVerificacao | null): boolean {
  return resultado === 'existe';
}
