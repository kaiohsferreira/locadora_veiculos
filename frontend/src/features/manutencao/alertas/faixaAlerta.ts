/**
 * Faixa de cor do cartão de alerta de revisão (tela 14). Funções puras, sem React.
 *
 * A API não manda cor: a faixa sai só da situação e dos km que faltam, já vindos da API. Os limites 500 e 1.200 km são
 * números aprovados pelo Kaio em 20/11/2026 (P8) e ficam aqui, no front (pergunta 15 do contrato, resolvida no front).
 */
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_REVISAO_IMINENTE,
  MENSAGEM_MANUTENCAO_REVISAO_VENCIDA,
  mensagemManutencaoCicloAtingido,
  mensagemManutencaoCicloLimite,
  mensagemManutencaoFaltam,
  mensagemManutencaoFaltamApenas,
  mensagemManutencaoPassouLimite,
} from '@/lib/mensagens';
import type { AlertaRevisaoRetorno } from '@/types/manutencao';

/** Faltam até este número de km (inclusive), ou a revisão já venceu: faixa vermelha. */
export const FAIXA_ALERTA_VERMELHA_KM = 500;
/** Faltam mais que a vermelha e até este número de km (inclusive): faixa laranja. */
export const FAIXA_ALERTA_LARANJA_KM = 1200;

export type FaixaAlerta = 'VERMELHA' | 'LARANJA' | 'NEUTRA';

/** Vermelha: revisão vencida ou faltam até 500 km. Laranja: até 1.200 km. Azul-cinza (neutra): o resto. */
export function faixaDoAlerta(alerta: Pick<AlertaRevisaoRetorno, 'situacao' | 'faltamKm'>): FaixaAlerta {
  if (alerta.situacao === 'VENCIDA' || alerta.faltamKm <= FAIXA_ALERTA_VERMELHA_KM) return 'VERMELHA';
  if (alerta.faltamKm <= FAIXA_ALERTA_LARANJA_KM) return 'LARANJA';
  return 'NEUTRA';
}

/** Texto de quanto falta: revisão vencida diz quanto passou; a vermelha diz "apenas"; as outras, "Faltam X km". */
export function textoFaltamDoAlerta(alerta: Pick<AlertaRevisaoRetorno, 'situacao' | 'faltamKm'>): string {
  if (alerta.situacao === 'VENCIDA') {
    return mensagemManutencaoPassouLimite(formatarNumero(Math.abs(alerta.faltamKm)));
  }
  const kmTexto = formatarNumero(alerta.faltamKm);
  return faixaDoAlerta(alerta) === 'VERMELHA'
    ? mensagemManutencaoFaltamApenas(kmTexto)
    : mensagemManutencaoFaltam(kmTexto);
}

/** Percentual do ciclo como texto inteiro. A vermelha diz "do ciclo limite"; as outras, "do ciclo atingido". */
export function textoCicloDoAlerta(alerta: Pick<AlertaRevisaoRetorno, 'situacao' | 'faltamKm' | 'percentualCiclo'>): string {
  const percentual = formatarNumero(Math.round(alerta.percentualCiclo));
  return faixaDoAlerta(alerta) === 'VERMELHA'
    ? mensagemManutencaoCicloLimite(percentual)
    : mensagemManutencaoCicloAtingido(percentual);
}

/** Largura da barra do ciclo, em porcentagem: de 0 a 100 (a barra trava em 100 quando a revisão já passou). */
export function larguraBarraCiclo(percentualCiclo: number): number {
  return Math.min(Math.max(percentualCiclo, 0), 100);
}

/** Selo do cartão vermelho: "Revisão vencida" ou "Revisão Iminente". Laranja e azul-cinza não têm selo. */
export function seloDoAlerta(alerta: Pick<AlertaRevisaoRetorno, 'situacao' | 'faltamKm'>): string | null {
  if (faixaDoAlerta(alerta) !== 'VERMELHA') return null;
  return alerta.situacao === 'VENCIDA' ? MENSAGEM_MANUTENCAO_REVISAO_VENCIDA : MENSAGEM_MANUTENCAO_REVISAO_IMINENTE;
}
