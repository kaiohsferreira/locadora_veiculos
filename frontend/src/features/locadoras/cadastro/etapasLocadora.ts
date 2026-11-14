/**
 * Regras pura das etapas do cadastro da locadora (sem React): quem pode abrir cada etapa.
 * Etapa 1: dados da empresa (sempre aberta). Etapa 2: responsável e acesso. Etapa 3: confirmação.
 * Sem os dados da etapa 1 em memória, a etapa 2 e a 3 voltam para a 1, como o cadastro do cliente faz.
 */
import { CAMINHOS } from '@/routes/caminhos';

/** Etapa do cadastro da locadora: 1 = dados da empresa, 2 = responsável e acesso, 3 = confirmação. */
export type EtapaLocadora = 1 | 2 | 3;

/**
 * Caminho para onde levar quem tenta abrir a etapa sem os dados necessários; null quando pode abrir.
 * As etapas 2 e 3 exigem os dados da etapa 1 em memória.
 */
export function caminhoSemDadosEmpresa(etapa: EtapaLocadora, temDadosEmpresa: boolean): string | null {
  if (etapa === 1) return null;
  return temDadosEmpresa ? null : CAMINHOS.cadastroLocadoraEmpresa;
}

/**
 * Caminho para onde levar quem abre a confirmação (etapa 3) sem a conta criada; null quando pode abrir.
 * Sem o resultado do envio (ex.: a página foi recarregada), volta para a etapa certa: a 2 se a etapa 1
 * ainda está em memória, senão a 1.
 */
export function caminhoSemCadastroConcluido(temDadosEmpresa: boolean, cadastroConcluido: boolean): string | null {
  if (cadastroConcluido) return null;
  return temDadosEmpresa ? CAMINHOS.cadastroLocadoraResponsavel : CAMINHOS.cadastroLocadoraEmpresa;
}
