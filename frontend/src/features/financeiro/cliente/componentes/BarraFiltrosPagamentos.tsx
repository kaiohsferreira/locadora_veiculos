import { useRef, type KeyboardEvent } from 'react';
import { Icone, type OpcaoSelect } from '@/components/ui';
import {
  ID_ABA_PAGAMENTOS_PAGOS,
  ID_ABA_PAGAMENTOS_PENDENTES,
  ID_ABA_PAGAMENTOS_TODOS,
  ID_PAINEL_PAGAMENTOS,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import {
  MENSAGEM_PAGAMENTOS_ABA_TODOS,
  MENSAGEM_PAGAMENTOS_NAVEGACAO_ABAS,
  MENSAGEM_PAGAMENTOS_PERIODO_ROTULO,
  mensagemAbaPagos,
  mensagemAbaPendentes,
} from '@/lib/mensagens';
import type { AbaPagamentosCliente, PeriodoPagamentosCliente } from '@/types/pagamentos';

export interface BarraFiltrosPagamentosProps {
  aba: AbaPagamentosCliente;
  aoMudarAba: (aba: AbaPagamentosCliente) => void;
  periodo: PeriodoPagamentosCliente;
  aoMudarPeriodo: (periodo: PeriodoPagamentosCliente) => void;
  opcoesPeriodo: readonly OpcaoSelect[];
  /** Contagens das abas; nulo enquanto o resumo não chega (a aba sai sem número). */
  quantidadePendentes: number | null;
  quantidadePagos: number | null;
}

interface AbaPagamentos {
  aba: AbaPagamentosCliente;
  rotulo: string;
  id: string;
}

/** Abas Todos, Pendentes (n) e Pagos (n), na ordem do desenho. */
function montarAbas(quantidadePendentes: number | null, quantidadePagos: number | null): readonly AbaPagamentos[] {
  return [
    { aba: 'TODOS', rotulo: MENSAGEM_PAGAMENTOS_ABA_TODOS, id: ID_ABA_PAGAMENTOS_TODOS },
    { aba: 'PENDENTES', rotulo: mensagemAbaPendentes(quantidadePendentes), id: ID_ABA_PAGAMENTOS_PENDENTES },
    { aba: 'PAGOS', rotulo: mensagemAbaPagos(quantidadePagos), id: ID_ABA_PAGAMENTOS_PAGOS },
  ];
}

const CLASSES_ABA =
  'rounded-sm px-3 py-1.5 text-rotulo font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao';

/**
 * Barra do desenho: abas de situação à esquerda e seletor de período à direita. A busca do desenho não entra (decisão 21).
 * Setas, Home e End trocam de aba pelo teclado (padrão de tablist).
 */
export function BarraFiltrosPagamentos({
  aba,
  aoMudarAba,
  periodo,
  aoMudarPeriodo,
  opcoesPeriodo,
  quantidadePendentes,
  quantidadePagos,
}: BarraFiltrosPagamentosProps) {
  const abas = montarAbas(quantidadePendentes, quantidadePagos);
  const refsAbas = useRef<Array<HTMLButtonElement | null>>([]);

  function aoTeclar(evento: KeyboardEvent<HTMLButtonElement>, indice: number) {
    let proximo: number | null = null;
    if (evento.key === 'ArrowRight') proximo = (indice + 1) % abas.length;
    else if (evento.key === 'ArrowLeft') proximo = (indice - 1 + abas.length) % abas.length;
    else if (evento.key === 'Home') proximo = 0;
    else if (evento.key === 'End') proximo = abas.length - 1;
    if (proximo === null) return;
    evento.preventDefault();
    aoMudarAba(abas[proximo].aba);
    refsAbas.current[proximo]?.focus();
  }

  return (
    <div className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-sm lg:flex-row lg:items-center lg:justify-between">
      <div
        role="tablist"
        aria-label={MENSAGEM_PAGAMENTOS_NAVEGACAO_ABAS}
        className="flex flex-wrap items-center gap-1 rounded-sm bg-superficie p-1 text-apoio"
      >
        {abas.map((item, indice) => {
          const ativa = item.aba === aba;
          return (
            <button
              key={item.aba}
              ref={(elemento) => {
                refsAbas.current[indice] = elemento;
              }}
              id={item.id}
              type="button"
              role="tab"
              aria-selected={ativa}
              aria-controls={ID_PAINEL_PAGAMENTOS}
              tabIndex={ativa ? 0 : -1}
              onClick={() => aoMudarAba(item.aba)}
              onKeyDown={(evento) => aoTeclar(evento, indice)}
              className={`${CLASSES_ABA} ${
                ativa
                  ? 'bg-branco text-navy-profundo shadow-sm'
                  : 'text-texto-apoio hover:bg-steel-suave hover:text-texto-desenho'
              }`}
            >
              {item.rotulo}
            </button>
          );
        })}
      </div>

      <div className="relative">
        <label htmlFor="pagamentos-periodo" className="sr-only">
          {MENSAGEM_PAGAMENTOS_PERIODO_ROTULO}
        </label>
        <select
          id="pagamentos-periodo"
          value={periodo}
          onChange={(evento) => aoMudarPeriodo(evento.target.value as PeriodoPagamentosCliente)}
          className="h-10 w-full cursor-pointer appearance-none rounded-sm bg-superficie pl-3 pr-8 text-apoio font-medium text-texto-desenho focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao lg:w-auto"
        >
          {opcoesPeriodo.map((opcao) => (
            <option key={opcao.valor} value={opcao.valor}>
              {opcao.rotulo}
            </option>
          ))}
        </select>
        <Icone
          nome="expand_more"
          tamanho={18}
          className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-texto-apoio"
        />
      </div>
    </div>
  );
}
