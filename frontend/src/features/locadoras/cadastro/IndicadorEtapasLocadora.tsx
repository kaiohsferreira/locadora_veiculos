import { Icone } from '@/components/ui';
import {
  MENSAGEM_CADASTRO_ETAPAS_ROTULO,
  MENSAGEM_CADASTRO_SITUACAO_PENDENTE,
  MENSAGEM_LOCADORA_ETAPA_1_TITULO,
  MENSAGEM_LOCADORA_ETAPA_2_TITULO,
  MENSAGEM_LOCADORA_ETAPA_3_TITULO,
  MENSAGEM_LOCADORA_SITUACAO_AGUARDANDO,
  MENSAGEM_LOCADORA_SITUACAO_CONCLUIDA,
  MENSAGEM_LOCADORA_SITUACAO_EM_PREENCHIMENTO,
} from '@/lib/mensagens';
import type { EtapaLocadora } from './etapasLocadora';

interface EtapaIndicador {
  numero: EtapaLocadora;
  titulo: string;
}

const ETAPAS: readonly EtapaIndicador[] = [
  { numero: 1, titulo: MENSAGEM_LOCADORA_ETAPA_1_TITULO },
  { numero: 2, titulo: MENSAGEM_LOCADORA_ETAPA_2_TITULO },
  { numero: 3, titulo: MENSAGEM_LOCADORA_ETAPA_3_TITULO },
];

export interface IndicadorEtapasLocadoraProps {
  /** Etapa que a pessoa está vendo. As anteriores ficam concluídas; a seguinte, aguardando; as demais, pendentes. */
  etapaAtual: EtapaLocadora;
}

/**
 * Indicador das três etapas do cadastro da locadora (desenho 36), só informativo. Cada situação é dita em
 * texto (não só pela cor) e a etapa atual leva `aria-current="step"`.
 */
export function IndicadorEtapasLocadora({ etapaAtual }: IndicadorEtapasLocadoraProps) {
  return (
    <ol aria-label={MENSAGEM_CADASTRO_ETAPAS_ROTULO} className="grid grid-cols-1 bg-steel-fundo md:grid-cols-3">
      {ETAPAS.map((etapa) => {
        const atual = etapa.numero === etapaAtual;
        const concluida = etapa.numero < etapaAtual;
        const proxima = etapa.numero === etapaAtual + 1;
        return (
          <li
            key={etapa.numero}
            aria-current={atual ? 'step' : undefined}
            className={`relative flex items-center justify-between gap-2 p-4 ${atual ? 'bg-branco' : ''}`}
          >
            <div className="flex min-w-0 items-center gap-2">
              <span
                aria-hidden="true"
                className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-corpo-denso font-semibold ${
                  atual ? 'bg-orange text-branco' : concluida ? 'bg-status-sucesso text-branco' : 'bg-steel-alto text-status-neutro'
                }`}
              >
                {concluida ? <Icone nome="check" tamanho={14} preenchido /> : etapa.numero}
              </span>
              <span className={`truncate text-corpo-denso font-semibold ${atual ? 'text-navy' : 'text-status-neutro'}`}>
                {etapa.titulo}
              </span>
            </div>

            {atual && (
              <span className="inline-flex shrink-0 items-center gap-1 rounded-sm bg-status-pendente-fundo px-2 py-0.5 text-legenda font-semibold uppercase text-status-pendente">
                <span aria-hidden="true" className="h-1.5 w-1.5 animate-pulse rounded-full bg-status-pendente motion-reduce:animate-none" />
                {MENSAGEM_LOCADORA_SITUACAO_EM_PREENCHIMENTO}
              </span>
            )}
            {concluida && (
              <span className="shrink-0 text-legenda uppercase text-status-sucesso">{MENSAGEM_LOCADORA_SITUACAO_CONCLUIDA}</span>
            )}
            {!atual && !concluida && (
              <span className="shrink-0 text-legenda uppercase text-status-neutro">
                {proxima ? MENSAGEM_LOCADORA_SITUACAO_AGUARDANDO : MENSAGEM_CADASTRO_SITUACAO_PENDENTE}
              </span>
            )}

            {atual && <span aria-hidden="true" className="absolute inset-x-0 bottom-0 h-1 bg-orange" />}
          </li>
        );
      })}
    </ol>
  );
}
