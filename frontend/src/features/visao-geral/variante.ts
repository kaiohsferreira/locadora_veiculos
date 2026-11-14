/** Escolhe a tela: 40 (primeiro acesso) quando a locadora ainda não tem veículo; 39 nos demais casos. */
import type { RespostaVisaoGeral } from '@/types/painel';

export type VarianteVisaoGeral = 'visao-geral' | 'primeiro-acesso';

export function escolherVariante(resposta: Pick<RespostaVisaoGeral, 'primeiroAcesso'>): VarianteVisaoGeral {
  return resposta.primeiroAcesso ? 'primeiro-acesso' : 'visao-geral';
}
