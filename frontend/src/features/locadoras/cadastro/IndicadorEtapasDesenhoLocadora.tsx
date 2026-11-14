import { Icone } from '@/components/ui';
import {
  MENSAGEM_CADASTRO_ETAPAS_ROTULO,
  MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO,
  MENSAGEM_CADASTRO_SITUACAO_PENDENTE,
  MENSAGEM_LOCADORA_ETAPA_1_TITULO,
  MENSAGEM_LOCADORA_ETAPA_2_TITULO,
  MENSAGEM_LOCADORA_ETAPA_3_TITULO,
  MENSAGEM_LOCADORA_PASSO,
  MENSAGEM_LOCADORA_SITUACAO_EM_ANDAMENTO,
} from '@/lib/mensagens';

export interface IndicadorEtapasDesenhoLocadoraProps {
  /** 2 = responsável e acesso (desenho 37); 3 = confirmação (desenho 38). */
  etapaAtual: 2 | 3;
}

interface EtapaDesenho {
  numero: 1 | 2 | 3;
  titulo: string;
}

const ETAPAS: readonly EtapaDesenho[] = [
  { numero: 1, titulo: MENSAGEM_LOCADORA_ETAPA_1_TITULO },
  { numero: 2, titulo: MENSAGEM_LOCADORA_ETAPA_2_TITULO },
  { numero: 3, titulo: MENSAGEM_LOCADORA_ETAPA_3_TITULO },
];

/**
 * Indicador dos desenhos 37 e 38. Na etapa 2, a 1 aparece concluída, a 2 em andamento e a 3 pendente.
 * Na etapa 3, as duas primeiras aparecem concluídas e a 3 é a conclusão (layout alinhado à direita, como no
 * desenho 38). Cada situação é dita em texto, e a etapa atual leva `aria-current="step"`.
 */
export function IndicadorEtapasDesenhoLocadora({ etapaAtual }: IndicadorEtapasDesenhoLocadoraProps) {
  const confirmacao = etapaAtual === 3;

  return (
    <ol
      aria-label={MENSAGEM_CADASTRO_ETAPAS_ROTULO}
      className="grid grid-cols-1 gap-4 bg-steel-fundo px-6 py-4 md:grid-cols-3"
    >
      {ETAPAS.map((etapa) => {
        const atual = etapa.numero === etapaAtual;
        const concluida = confirmacao ? etapa.numero <= 2 || atual : etapa.numero < etapaAtual;
        const pendente = !concluida && !atual;
        const alinhadaADireita = confirmacao && etapa.numero === 3;

        return (
          <li
            key={etapa.numero}
            aria-current={atual ? 'step' : undefined}
            className={[
              'flex items-center gap-2',
              alinhadaADireita ? 'justify-end text-right md:col-start-3' : '',
              atual && !confirmacao ? 'rounded-lg bg-branco p-2 shadow-sm' : '',
              pendente ? 'opacity-60' : '',
            ].join(' ')}
          >
            {!alinhadaADireita && (
              <BolinhaEtapa numero={etapa.numero} concluida={concluida} atual={atual && !confirmacao} />
            )}

            <div className="min-w-0">
              <div className={`flex flex-wrap items-center gap-1.5 ${alinhadaADireita ? 'justify-end' : ''}`}>
                <span className={`text-legenda uppercase ${rotuloDaEtapa(atual, confirmacao)}`}>
                  {MENSAGEM_LOCADORA_PASSO} {etapa.numero}
                </span>
                <Pilula numero={etapa.numero} etapaAtual={etapaAtual} />
              </div>
              <p className={`truncate text-corpo-denso ${classesTitulo(atual, pendente, confirmacao)}`}>
                {etapa.titulo}
              </p>
            </div>
          </li>
        );
      })}
    </ol>
  );
}

/** Cor e peso do rótulo "Passo n": a etapa em andamento se destaca (laranja na 2, verde na conclusão da 3). */
function rotuloDaEtapa(atual: boolean, confirmacao: boolean): string {
  if (!atual) return 'text-status-neutro';
  return confirmacao ? 'font-semibold text-status-sucesso' : 'font-bold text-status-pendente';
}

/** Cor e peso do título: pendente em cinza; a etapa em andamento (2) em negrito. */
function classesTitulo(atual: boolean, pendente: boolean, confirmacao: boolean): string {
  if (pendente) return 'font-semibold text-status-neutro';
  return atual && !confirmacao ? 'font-bold text-navy' : 'font-semibold text-navy';
}

interface BolinhaEtapaProps {
  numero: 1 | 2 | 3;
  concluida: boolean;
  atual: boolean;
}

/** Círculo da etapa: check quando concluída, número nas outras (laranja na etapa em andamento). */
function BolinhaEtapa({ numero, concluida, atual }: BolinhaEtapaProps) {
  if (concluida) {
    return (
      <span
        aria-hidden="true"
        className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-status-sucesso-fundo text-status-sucesso"
      >
        <Icone nome="check" tamanho={18} preenchido />
      </span>
    );
  }
  return (
    <span
      aria-hidden="true"
      className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-corpo-denso font-semibold ${
        atual ? 'bg-orange text-branco' : 'bg-steel-alto text-status-neutro'
      }`}
    >
      {numero}
    </span>
  );
}

interface PilulaProps {
  numero: 1 | 2 | 3;
  etapaAtual: 2 | 3;
}

/**
 * Situação em texto ao lado do título. Na etapa 2: concluída, em andamento ou pendente. Na etapa 3, só a 3
 * aparece ("Concluído"), como no desenho 38.
 */
function Pilula({ numero, etapaAtual }: PilulaProps) {
  if (etapaAtual === 3) {
    if (numero !== 3) return <span className="sr-only">{MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO}</span>;
    return (
      <span className="inline-flex items-center gap-1 rounded-lg bg-status-sucesso-fundo px-2 py-0.5 text-legenda font-bold uppercase tracking-wide text-status-sucesso">
        <Icone nome="verified" tamanho={14} preenchido />
        {MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO}
      </span>
    );
  }
  if (numero < etapaAtual) {
    return (
      <span className="rounded bg-status-sucesso-fundo px-1.5 py-0.5 text-legenda font-semibold uppercase text-status-sucesso">
        {MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO}
      </span>
    );
  }
  if (numero === etapaAtual) {
    return (
      <span className="inline-flex items-center gap-1 rounded bg-status-pendente-fundo px-1.5 py-0.5 text-legenda font-bold uppercase text-status-pendente">
        <span aria-hidden="true" className="h-1.5 w-1.5 animate-pulse rounded-full bg-status-pendente motion-reduce:animate-none" />
        {MENSAGEM_LOCADORA_SITUACAO_EM_ANDAMENTO}
      </span>
    );
  }
  return (
    <span className="rounded bg-steel-suave px-1.5 py-0.5 text-legenda uppercase text-status-neutro">
      {MENSAGEM_CADASTRO_SITUACAO_PENDENTE}
    </span>
  );
}
