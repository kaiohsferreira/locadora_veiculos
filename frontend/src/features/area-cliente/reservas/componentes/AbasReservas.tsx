import { useRef, type KeyboardEvent } from 'react';
import { Icone } from '@/components/ui';
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_RESERVAS_ABA_ATIVAS,
  MENSAGEM_RESERVAS_ABA_HISTORICO,
  MENSAGEM_RESERVAS_ABA_HISTORICO_CELULAR,
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
  /** Rótulo curto só no celular (abaixo de md). Sem ele, a aba usa o mesmo rótulo em todas as larguras. */
  rotuloCelular?: string;
  id: string;
}

const ABAS: readonly AbaReservas[] = [
  { situacao: 'ATIVAS', rotulo: MENSAGEM_RESERVAS_ABA_ATIVAS, id: ID_ABA_ATIVAS },
  {
    situacao: 'HISTORICO',
    rotulo: MENSAGEM_RESERVAS_ABA_HISTORICO,
    rotuloCelular: MENSAGEM_RESERVAS_ABA_HISTORICO_CELULAR,
    id: ID_ABA_HISTORICO,
  },
];

/**
 * Celular (desenho 28): as duas abas dividem a largura, lado a lado, num trilho claro. Do computador para cima
 * (md, 768 px), voltam os botões separados de antes (desenho 29).
 */
const CLASSES_ABA =
  'inline-flex flex-1 items-center justify-center gap-1 rounded-cartao px-1.5 py-2 text-corpo-denso font-semibold transition-colors ' +
  'md:flex-none md:justify-start md:gap-2 md:px-4 ' +
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

const CLASSES_CONTADOR = 'inline-flex min-w-5 items-center justify-center rounded-full px-1.5 py-0.5 text-rotulo leading-none md:px-2';

/**
 * Contador da aba ativa: no celular, o laranja exato do desenho 28 (#AB3500, frota-oficina) com texto branco. No
 * computador, o laranja do desenho 29 (secondary-container, #FE6A34, que é orange-acao) com texto branco.
 */
const CLASSE_CONTADOR_ATIVA = 'bg-frota-oficina font-bold text-branco md:bg-orange-acao';

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
      <div
        role="tablist"
        aria-label={MENSAGEM_RESERVAS_NAVEGACAO}
        className="flex w-full items-center gap-1 rounded-cartao bg-steel-suave p-0.5 md:w-auto md:flex-wrap md:gap-2 md:bg-transparent md:p-0"
      >
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
              className={`${CLASSES_ABA} ${
                ativa
                  ? 'bg-branco text-navy shadow-sm md:bg-navy md:text-branco'
                  : 'bg-transparent text-texto hover:bg-steel-alto md:bg-branco md:hover:bg-steel-suave'
              }`}
            >
              {aba.rotuloCelular ? (
                <>
                  <span className="md:hidden">{aba.rotuloCelular}</span>
                  <span className="hidden md:inline">{aba.rotulo}</span>
                </>
              ) : (
                <span>{aba.rotulo}</span>
              )}
              {contagem !== 'indisponivel' && (
                <span
                  className={`${CLASSES_CONTADOR} ${ativa ? CLASSE_CONTADOR_ATIVA : 'bg-steel-alto font-medium text-status-neutro'}`}
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
