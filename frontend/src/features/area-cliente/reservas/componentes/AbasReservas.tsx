import { useRef, type KeyboardEvent } from 'react';
import { Icone } from '@/components/ui';
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_RESERVAS_ABA_ATIVAS,
  MENSAGEM_RESERVAS_ABA_HISTORICO,
  MENSAGEM_RESERVAS_NAVEGACAO,
} from '@/lib/mensagens';
import { ID_ABA_ATIVAS, ID_ABA_HISTORICO, ID_PAINEL_RESERVAS } from '@/features/area-cliente/reservas/parametrosReservas';
import type { SituacaoReserva } from '@/types/reservas';

/** Contador de uma aba: número, ainda carregando, ou indisponível (a aba sai sem número). */
export type ContagemAba = number | 'carregando' | 'indisponivel';

export interface AbasReservasProps {
  situacao: SituacaoReserva;
  aoMudar: (situacao: SituacaoReserva) => void;
  contagens: Readonly<Record<SituacaoReserva, ContagemAba>>;
  /** Texto de "Atualizado"; null esconde a faixa. */
  atualizacao: string | null;
  /** Consulta em andamento: o ícone gira. */
  atualizando: boolean;
}

interface AbaReservas {
  situacao: SituacaoReserva;
  rotulo: string;
  id: string;
}

const ABAS: readonly AbaReservas[] = [
  { situacao: 'ATIVAS', rotulo: MENSAGEM_RESERVAS_ABA_ATIVAS, id: ID_ABA_ATIVAS },
  { situacao: 'HISTORICO', rotulo: MENSAGEM_RESERVAS_ABA_HISTORICO, id: ID_ABA_HISTORICO },
];

const CLASSES_ABA =
  'inline-flex items-center gap-2 rounded-cartao px-4 py-2 text-corpo-denso font-semibold transition-colors ' +
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

const CLASSES_CONTADOR = 'inline-flex min-w-5 items-center justify-center rounded-full px-2 py-0.5 text-rotulo leading-none';

/**
 * Abas Ativas e Próximas / Histórico, com contadores e "Atualizado". Setas, Home e End trocam de aba pelo teclado
 * (padrão de tablist): só a aba ativa entra na ordem de Tab.
 */
export function AbasReservas({ situacao, aoMudar, contagens, atualizacao, atualizando }: AbasReservasProps) {
  const refsAbas = useRef<Array<HTMLButtonElement | null>>([]);

  function aoTeclar(evento: KeyboardEvent<HTMLButtonElement>, indice: number) {
    let proximo: number | null = null;
    if (evento.key === 'ArrowRight') proximo = (indice + 1) % ABAS.length;
    else if (evento.key === 'ArrowLeft') proximo = (indice - 1 + ABAS.length) % ABAS.length;
    else if (evento.key === 'Home') proximo = 0;
    else if (evento.key === 'End') proximo = ABAS.length - 1;
    if (proximo === null) return;
    evento.preventDefault();
    aoMudar(ABAS[proximo].situacao);
    refsAbas.current[proximo]?.focus();
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-borda pb-1">
      <div role="tablist" aria-label={MENSAGEM_RESERVAS_NAVEGACAO} className="flex flex-wrap items-center gap-2">
        {ABAS.map((aba, indice) => {
          const ativa = aba.situacao === situacao;
          const contagem = contagens[aba.situacao];
          return (
            <button
              key={aba.situacao}
              ref={(elemento) => {
                refsAbas.current[indice] = elemento;
              }}
              id={aba.id}
              type="button"
              role="tab"
              aria-selected={ativa}
              aria-controls={ID_PAINEL_RESERVAS}
              tabIndex={ativa ? 0 : -1}
              onClick={() => aoMudar(aba.situacao)}
              onKeyDown={(evento) => aoTeclar(evento, indice)}
              className={`${CLASSES_ABA} ${ativa ? 'bg-navy text-branco shadow-sm' : 'bg-branco text-texto hover:bg-steel-suave'}`}
            >
              <span>{aba.rotulo}</span>
              {contagem !== 'indisponivel' && (
                <span
                  className={`${CLASSES_CONTADOR} ${ativa ? 'bg-orange font-bold text-branco' : 'bg-steel-alto font-medium text-status-neutro'}`}
                >
                  {contagem === 'carregando' ? (
                    <span className="inline-block h-3 w-4 rounded bg-borda motion-safe:animate-pulse" aria-hidden="true" />
                  ) : (
                    formatarNumero(contagem)
                  )}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {atualizacao && (
        <div className="hidden items-center gap-1 text-micro text-texto-suave sm:flex">
          <Icone nome="sync" tamanho={16} className={atualizando ? 'motion-safe:animate-spin' : ''} />
          <span>{atualizacao}</span>
        </div>
      )}
    </div>
  );
}
