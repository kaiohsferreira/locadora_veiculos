import { Icone } from '@/components/ui';
import {
  MENSAGEM_CADASTRO_ETAPAS_ROTULO,
  MENSAGEM_CADASTRO_ETAPA_1_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_1_TITULO,
  MENSAGEM_CADASTRO_ETAPA_2_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_2_TITULO,
  MENSAGEM_CADASTRO_ETAPA_3_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_3_TITULO,
  MENSAGEM_CADASTRO_SITUACAO_ATUAL,
  MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO,
  MENSAGEM_CADASTRO_SITUACAO_PENDENTE,
} from '@/lib/mensagens';

/**
 * Indicador de três etapas, só informativo (não é clicável). O estado de cada etapa é dito em texto
 * (não só pela cor) e a etapa atual leva `aria-current="step"`. Há dois formatos, um por tela do
 * cadastro: o cabeçalho com linha de progresso (etapa 1) e o cartão com barra em degradê (etapa 2).
 */

interface EtapaDaTela1 {
  titulo: string;
  situacao: string;
}

const ETAPAS_TELA_1: readonly EtapaDaTela1[] = [
  { titulo: MENSAGEM_CADASTRO_ETAPA_1_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_1_SITUACAO },
  { titulo: MENSAGEM_CADASTRO_ETAPA_2_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_2_SITUACAO },
  { titulo: MENSAGEM_CADASTRO_ETAPA_3_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_3_SITUACAO },
];

/** Etapa 1 (design 20): faixa azulada no topo do cartão, com a linha de progresso e a etapa 1 ativa. */
export function IndicadorEtapasCabecalho() {
  return (
    <div className="bg-steel-fundo px-8 pb-6 pt-8">
      <div className="relative">
        {/* Linha de fundo e trecho já percorrido (decorativos). */}
        <div aria-hidden="true" className="pointer-events-none absolute left-[16.67%] right-[16.67%] top-4 h-0.5 bg-steel-linha md:left-6 md:right-6" />
        <div aria-hidden="true" className="pointer-events-none absolute left-[16.67%] top-4 h-0.5 w-1/3 bg-status-pendente md:left-6" />
        <ol aria-label={MENSAGEM_CADASTRO_ETAPAS_ROTULO} className="relative grid grid-cols-3 items-start md:flex md:justify-between">
          {ETAPAS_TELA_1.map((etapa, indice) => {
            const atual = indice === 0;
            return (
              <li
                key={etapa.titulo}
                aria-current={atual ? 'step' : undefined}
                className="relative z-10 flex min-w-0 flex-col items-center px-1 md:px-0"
              >
                {atual ? (
                  <span
                    aria-hidden="true"
                    className="relative flex h-9 w-9 items-center justify-center rounded-xl bg-navy text-corpo text-branco shadow-md ring-4 ring-orange/20"
                  >
                    <span className="absolute h-2.5 w-2.5 animate-ping rounded-xl bg-orange motion-reduce:animate-none" />
                    <span className="relative z-10">{indice + 1}</span>
                  </span>
                ) : (
                  <span
                    aria-hidden="true"
                    className="flex h-9 w-9 items-center justify-center rounded-xl bg-steel-alto text-corpo text-status-neutro opacity-60"
                  >
                    {indice + 1}
                  </span>
                )}
                <span className="mt-1 min-w-0 break-words text-center">
                  <span className={`block text-legenda md:text-corpo ${atual ? 'text-navy' : 'text-status-neutro'}`}>
                    {indice + 1}. {etapa.titulo}
                  </span>
                  <span
                    className={`block text-legenda md:text-corpo ${
                      atual ? 'font-semibold uppercase tracking-wider text-status-pendente' : 'text-status-neutro'
                    }`}
                  >
                    {etapa.situacao}
                  </span>
                </span>
              </li>
            );
          })}
        </ol>
      </div>
    </div>
  );
}

/** Etapa 2 (design 21): cartão com as três etapas (concluída, atual e pendente) e barra em degradê. */
export function IndicadorEtapasProgresso() {
  return (
    <div className="mb-8 w-full rounded-lg bg-branco p-6 shadow-sm">
      <ol aria-label={MENSAGEM_CADASTRO_ETAPAS_ROTULO} className="relative grid grid-cols-1 items-center gap-6 md:grid-cols-3">
        <li className="flex cursor-default items-center gap-4">
          <span
            aria-hidden="true"
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-status-sucesso text-branco shadow-sm"
          >
            <Icone nome="check" tamanho={20} preenchido />
          </span>
          <span className="flex min-w-0 flex-col">
            <span className="text-corpo uppercase tracking-wide text-status-sucesso">
              {MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO}
            </span>
            <span className="truncate text-corpo text-texto">
              1. {MENSAGEM_CADASTRO_ETAPA_1_TITULO}
            </span>
          </span>
        </li>

        <li aria-current="step" className="relative flex items-center gap-4 rounded bg-navy/5 p-2">
          <span aria-hidden="true" className="relative shrink-0">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-navy text-branco shadow-sm">
              <Icone nome="badge" tamanho={20} />
            </span>
            <span className="absolute -bottom-1 -right-1 flex h-4 w-4 items-center justify-center rounded-xl bg-orange shadow-sm">
              <span className="h-1.5 w-1.5 animate-ping rounded-xl bg-branco motion-reduce:animate-none" />
            </span>
          </span>
          <span className="flex min-w-0 flex-col">
            <span className="flex items-center gap-1">
              <span className="text-corpo uppercase tracking-wider text-status-pendente">
                {MENSAGEM_CADASTRO_SITUACAO_ATUAL}
              </span>
              <span aria-hidden="true" className="h-1.5 w-1.5 rounded-xl bg-orange" />
            </span>
            <span className="truncate text-corpo text-navy">
              2. {MENSAGEM_CADASTRO_ETAPA_2_TITULO}
            </span>
          </span>
        </li>

        <li className="flex items-center gap-4">
          <span
            aria-hidden="true"
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-steel-alto text-status-neutro opacity-60"
          >
            <Icone nome="task_alt" tamanho={20} />
          </span>
          <span className="flex min-w-0 flex-col">
            <span className="text-corpo uppercase text-status-neutro">{MENSAGEM_CADASTRO_SITUACAO_PENDENTE}</span>
            <span className="truncate text-corpo text-status-neutro">
              3. {MENSAGEM_CADASTRO_ETAPA_3_TITULO}
            </span>
          </span>
        </li>
      </ol>

      {/* Trilho decorativo: dois terços percorridos (a informação está no texto acima). */}
      <div aria-hidden="true" className="mt-4 h-1.5 w-full overflow-hidden rounded-xl bg-steel-suave">
        <div className="h-full w-2/3 rounded-xl bg-gradient-to-r from-status-sucesso via-orange to-status-pendente" />
      </div>
    </div>
  );
}
