import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { MENSAGEM_VISTORIA_CONCLUIDO, MENSAGEM_VISTORIA_EM_ANDAMENTO } from '@/lib/mensagensVistoriaAvarias';

export type SituacaoEtapa = 'concluida' | 'atual' | 'futura';

export interface EtapaTrilha {
  numero: number;
  situacao: SituacaoEtapa;
}

/** Etapas da trilha da tela 19 (nomes da tela 16, N11.1). A vistoria é a etapa 2. */
export function etapasDaTrilha(vistoriaRegistrada: boolean): EtapaTrilha[] {
  return [
    { numero: 1, situacao: 'concluida' },
    { numero: 2, situacao: vistoriaRegistrada ? 'concluida' : 'atual' },
    { numero: 3, situacao: vistoriaRegistrada ? 'atual' : 'futura' },
    { numero: 4, situacao: 'futura' },
  ];
}

/** Legenda da etapa: "Etapa 2 • Em Andamento", "Etapa 1 • Concluído" ou só "Etapa 3" quando ainda não começou. */
export function rotuloDaEtapa(etapa: EtapaTrilha): string {
  const base = MENSAGENS_RETIRADA.etapa(etapa.numero);
  if (etapa.situacao === 'concluida') return `${base} • ${MENSAGEM_VISTORIA_CONCLUIDO}`;
  if (etapa.situacao === 'atual' && etapa.numero === 2) return `${base} • ${MENSAGEM_VISTORIA_EM_ANDAMENTO}`;
  return base;
}
