/**
 * Rascunho do cadastro de veículo, só neste navegador (decisão do Kaio, 16/11/2026). Sem React e sem chamada à API.
 *
 * - A chave tem o id do usuário e o id da locadora da sessão: um rascunho nunca aparece para outra pessoa ou outra
 *   locadora no mesmo navegador. Sem essas duas coisas, não há rascunho.
 * - O conteúdo leva a versão do formato. Versão diferente, JSON inválido ou campo que não é texto: o rascunho é ignorado
 *   (nunca lança e nunca preenche o formulário com dado parcial).
 * - Só os valores do formulário; nunca fotos. Ao cadastrar, quem chama apaga o rascunho.
 * - O armazenamento pode não existir (navegador sem armazenamento local, modo privado cheio). Toda leitura e gravação
 *   tem proteção: sem armazenamento, a tela funciona sem rascunho.
 */
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';

/** Versão do formato guardado. Mudar o formato pede outra versão: o antigo é ignorado. */
export const VERSAO_RASCUNHO_VEICULO = 1;

const PREFIXO_CHAVE_RASCUNHO = 'autovia:rascunho-veiculo';

/** Parte do armazenamento local que o rascunho usa (o subconjunto de `Storage`). */
export type ArmazenamentoRascunho = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

/** Identidade da sessão que separa os rascunhos. Ausente = sem rascunho. */
export interface IdentidadeRascunhoVeiculo {
  idUsuario?: number;
  idEmpresa?: number;
}

interface RascunhoGravado {
  versao: number;
  valores: ValoresFormularioVeiculo;
}

function ehObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null && !Array.isArray(valor);
}

/** Chave do rascunho do usuário na locadora da sessão. Nulo quando falta algum dos dois ids. */
export function chaveRascunhoVeiculo(identidade: IdentidadeRascunhoVeiculo): string | null {
  const { idUsuario, idEmpresa } = identidade;
  if (idUsuario === undefined || idEmpresa === undefined) return null;
  return `${PREFIXO_CHAVE_RASCUNHO}:v${VERSAO_RASCUNHO_VEICULO}:usuario-${idUsuario}:locadora-${idEmpresa}`;
}

/** Texto gravado no armazenamento: versão e os valores do formulário (todos texto). */
export function serializarRascunhoVeiculo(valores: ValoresFormularioVeiculo): string {
  const gravado: RascunhoGravado = { versao: VERSAO_RASCUNHO_VEICULO, valores: { ...valores } };
  return JSON.stringify(gravado);
}

/**
 * Lê o texto gravado. Devolve os valores só quando a versão é a atual e cada campo do formulário é texto. Qualquer
 * outra coisa (JSON inválido, versão antiga, campo ausente ou que não é texto) devolve nulo.
 */
export function lerRascunhoVeiculo(texto: string | null): ValoresFormularioVeiculo | null {
  if (texto === null) return null;
  let dado: unknown;
  try {
    dado = JSON.parse(texto);
  } catch {
    return null;
  }
  if (!ehObjeto(dado) || dado.versao !== VERSAO_RASCUNHO_VEICULO || !ehObjeto(dado.valores)) return null;
  const formato = valoresIniciaisCadastro();
  const valores: ValoresFormularioVeiculo = { ...formato };
  for (const campo of Object.keys(formato) as (keyof ValoresFormularioVeiculo)[]) {
    const valor = dado.valores[campo];
    if (typeof valor !== 'string') return null;
    valores[campo] = valor;
  }
  return valores;
}

/** Armazenamento disponível ou nulo (acesso ao `localStorage` pode lançar em navegadores que o bloqueiam). */
export function obterArmazenamentoRascunho(): ArmazenamentoRascunho | null {
  try {
    if (typeof window === 'undefined' || window.localStorage === undefined) return null;
    return window.localStorage;
  } catch {
    return null;
  }
}

/** Grava o rascunho. Devolve falso quando não foi possível (sem chave, sem armazenamento ou cota cheia). */
export function guardarRascunhoVeiculo(
  armazenamento: ArmazenamentoRascunho | null,
  chave: string | null,
  valores: ValoresFormularioVeiculo,
): boolean {
  if (armazenamento === null || chave === null) return false;
  try {
    armazenamento.setItem(chave, serializarRascunhoVeiculo(valores));
    return true;
  } catch {
    return false;
  }
}

/** Lê o rascunho guardado para a chave. Sem armazenamento, sem chave ou com erro de leitura: nulo. */
export function lerRascunhoArmazenado(
  armazenamento: ArmazenamentoRascunho | null,
  chave: string | null,
): ValoresFormularioVeiculo | null {
  if (armazenamento === null || chave === null) return null;
  try {
    return lerRascunhoVeiculo(armazenamento.getItem(chave));
  } catch {
    return null;
  }
}

/** Apaga o rascunho da chave. Sem armazenamento ou com erro, não faz nada (não é falha da tela). */
export function apagarRascunhoVeiculo(armazenamento: ArmazenamentoRascunho | null, chave: string | null): void {
  if (armazenamento === null || chave === null) return;
  try {
    armazenamento.removeItem(chave);
  } catch {
    // Armazenamento indisponível: não há o que apagar.
  }
}
