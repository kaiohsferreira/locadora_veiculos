/**
 * Lógica pura da verificação de CNPJ e e-mail já cadastrados ao sair do campo (P10-FE-01, RF-53).
 * Sem React e sem Axios: normaliza o valor como o backend, decide se vale a pena consultar, descarta
 * resposta de valor antigo e combina o resultado com o ícone de "conferido" do CNPJ.
 *
 * A verificação nunca bloqueia o cadastro por falha (rede, 5xx, 429, 400): só `existe` bloqueia, e o
 * 409 do envio continua sendo a autoridade.
 */
import { somenteDigitos } from '@/lib/formatters';
import { MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO, MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO } from '@/lib/mensagens';
import type { CampoVerificacaoEmpresa, RespostaVerificarEmpresa } from '@/types/empresas';
import { ESQUEMA_EMPRESA } from './esquemaLocadora';

/** Campos do formulário que a verificação cobre (CNPJ e e-mail comercial). */
export type CampoVerificavelLocadora = 'cnpj' | 'email';

/** Resultado de uma verificação: `indisponivel` = falhou (rede, 5xx, 429, 400, cancelada); nunca bloqueia. */
export type ResultadoVerificacaoLocadora = 'existe' | 'livre' | 'indisponivel';

/** Situação de um campo na tela: sem verificação do valor atual, em andamento ou com resultado. */
export type EstadoVerificacaoLocadora = 'nao-verificado' | 'verificando' | ResultadoVerificacaoLocadora;

/** O que a tela lembra de um campo: para qual valor (já normalizado) a verificação vale e em que ponto está. */
export interface RegistroVerificacaoLocadora {
  valor: string;
  estado: 'verificando' | ResultadoVerificacaoLocadora;
}

/** Primeiro item da chave de consulta do TanStack Query; a família inteira é limpa ao sair do cadastro. */
export const CHAVE_VERIFICACAO_LOCADORA = 'verificar-empresa';

/** Tipo do erro de campo que a verificação põe no formulário (a tela o limpa ao editar o campo). */
export const TIPO_ERRO_DUPLICADO_LOCADORA = 'duplicado';

/** Quanto tempo um resultado fica na memória: não repete a pergunta do mesmo valor nesse intervalo (5 minutos). */
export const TEMPO_MEMORIA_VERIFICACAO_LOCADORA_MS = 5 * 60 * 1000;

/** Opções fixas da consulta de verificação: sem nova tentativa automática; resultado fica 5 minutos em memória. */
export const OPCOES_CONSULTA_VERIFICACAO_LOCADORA = {
  retry: false,
  staleTime: TEMPO_MEMORIA_VERIFICACAO_LOCADORA_MS,
  gcTime: TEMPO_MEMORIA_VERIFICACAO_LOCADORA_MS,
} as const;

/** Campo do formulário para o campo do contrato da API. */
export const CAMPO_API_POR_CAMPO_LOCADORA: Readonly<Record<CampoVerificavelLocadora, CampoVerificacaoEmpresa>> = {
  cnpj: 'CNPJ',
  email: 'EMAIL',
};

/** Mensagem de duplicidade por campo (o texto do desenho da tela 36). */
export function mensagemDeDuplicidadeLocadora(campo: CampoVerificavelLocadora): string {
  return campo === 'cnpj' ? MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO : MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO;
}

/** Mesma forma gravada pelo backend: CNPJ só dígitos; e-mail sem espaços nas pontas e em minúsculas. */
export function normalizarValorVerificacaoLocadora(campo: CampoVerificavelLocadora, valor: string): string {
  if (campo === 'email') return valor.trim().toLowerCase();
  return somenteDigitos(valor);
}

/**
 * Valor normalizado a consultar, ou null quando o valor ainda não passa no esquema do próprio campo
 * (vazio, CNPJ com dígito errado, e-mail sem formato...): valor malformado nunca vai para a API.
 */
export function valorVerificavelLocadora(campo: CampoVerificavelLocadora, valor: string): string | null {
  if (!ESQUEMA_EMPRESA.shape[campo].safeParse(valor).success) return null;
  return normalizarValorVerificacaoLocadora(campo, valor);
}

/**
 * Lê a resposta 200 do contrato: `{ existe: true }` é `existe`, `{ existe: false }` é `livre`;
 * o resto (resposta fora do contrato) é `indisponivel`.
 */
export function interpretarRespostaVerificacaoEmpresa(resposta: unknown): ResultadoVerificacaoLocadora {
  if (typeof resposta !== 'object' || resposta === null) return 'indisponivel';
  const existe = (resposta as Partial<RespostaVerificarEmpresa>).existe;
  if (existe === true) return 'existe';
  if (existe === false) return 'livre';
  return 'indisponivel';
}

/** Resultado da chamada protegida; `respostaInvalida` = a API respondeu, mas fora do contrato (não deve ficar em cache). */
export interface VerificacaoComFalhaAbertaLocadora {
  resultado: ResultadoVerificacaoLocadora;
  respostaInvalida: boolean;
}

/**
 * Falha aberta: qualquer erro de `buscar` (rede, 5xx, 429, 400, cancelamento) vira `indisponivel`, sem lançar.
 * Se a chamada resolver, interpreta a resposta; resposta fora do contrato também vira `indisponivel`.
 */
export async function verificarComFalhaAbertaLocadora(
  buscar: () => Promise<unknown>,
): Promise<VerificacaoComFalhaAbertaLocadora> {
  let resposta: unknown;
  try {
    resposta = await buscar();
  } catch {
    return { resultado: 'indisponivel', respostaInvalida: false };
  }
  const resultado = interpretarRespostaVerificacaoEmpresa(resposta);
  return { resultado, respostaInvalida: resultado === 'indisponivel' };
}

/**
 * Decide se é preciso consultar o valor agora. Não consulta de novo o mesmo valor (nem quando já há
 * verificação em andamento, que é aproveitada). Consulta quando não há registro do valor, quando o
 * valor mudou, e, com `tentarNovamenteIndisponivel` (o avanço da etapa), quando a tentativa anterior
 * falhou.
 */
export function precisaConsultarLocadora(
  registro: RegistroVerificacaoLocadora | undefined,
  valorNormalizado: string,
  tentarNovamenteIndisponivel = false,
): boolean {
  if (registro === undefined || registro.valor !== valorNormalizado) return true;
  if (registro.estado === 'verificando') return false;
  return tentarNovamenteIndisponivel && registro.estado === 'indisponivel';
}

/** Situação do campo para o valor atual; registro de outro valor conta como "não verificado". */
export function estadoVigenteLocadora(
  registro: RegistroVerificacaoLocadora | undefined,
  campo: CampoVerificavelLocadora,
  valorAtual: string,
): EstadoVerificacaoLocadora {
  if (registro === undefined) return 'nao-verificado';
  if (registro.valor !== normalizarValorVerificacaoLocadora(campo, valorAtual)) return 'nao-verificado';
  return registro.estado;
}

/** O nome do campo do formulário é um dos campos que a verificação cobre (guarda de tipo). */
export function ehCampoVerificavelLocadora(campo: string): campo is CampoVerificavelLocadora {
  return campo === 'cnpj' || campo === 'email';
}

/**
 * Ícone de "conferido" do CNPJ: só aparece se o formato e os dígitos conferem E a verificação do valor
 * atual terminou em `livre` ou `indisponivel` (falha não tira o ícone de quem já passou no formato).
 * Sem ícone se o formato não confere, se ainda não foi verificado, enquanto verifica ou se já existe.
 */
export function cnpjConferidoPelaVerificacao(cnpjValido: boolean, estado: EstadoVerificacaoLocadora): boolean {
  return cnpjValido && (estado === 'livre' || estado === 'indisponivel');
}

/** Só `existe` bloqueia o avanço (e o envio). Falha, sem resultado ou valor livre seguem. */
export function deveBloquearContinuarLocadora(resultado: ResultadoVerificacaoLocadora | null): boolean {
  return resultado === 'existe';
}

/** Campos que a tela precisa focar ao bloquear o avanço, na ordem da tela (CNPJ antes do e-mail). */
export function camposBloqueadosAoAvancar(
  resultadoCnpj: ResultadoVerificacaoLocadora | null,
  resultadoEmail: ResultadoVerificacaoLocadora | null,
): CampoVerificavelLocadora[] {
  const bloqueados: CampoVerificavelLocadora[] = [];
  if (deveBloquearContinuarLocadora(resultadoCnpj)) bloqueados.push('cnpj');
  if (deveBloquearContinuarLocadora(resultadoEmail)) bloqueados.push('email');
  return bloqueados;
}
