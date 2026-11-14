import type { RefObject } from 'react';
import { Campo, Select } from '@/components/ui';
import {
  MENSAGEM_EQUIPE_BUSCA_PLACEHOLDER,
  MENSAGEM_EQUIPE_BUSCA_ROTULO,
  MENSAGEM_EQUIPE_CARGO_ROTULO,
  MENSAGEM_EQUIPE_CARGO_TODOS,
  MENSAGEM_EQUIPE_SITUACAO_GRUPO,
} from '@/lib/mensagens';
import { CARGO_FUNCIONARIO, ROTULO_CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import {
  OPCOES_SITUACAO_EQUIPE,
  TAMANHO_MAXIMO_BUSCA_EQUIPE,
  type EstadoFiltrosEquipe,
  type SituacaoFiltroEquipe,
} from '../listagem/parametrosListagem';

const OPCOES_CARGO = CARGO_FUNCIONARIO.map((cargo) => ({ valor: cargo, rotulo: ROTULO_CARGO_FUNCIONARIO[cargo] }));

/** Valor do select de cargo: um dos seis cargos, ou vazio para "Todos os cargos". */
function paraCargoFiltro(valor: string): CargoFuncionario | '' {
  return (CARGO_FUNCIONARIO as readonly string[]).includes(valor) ? (valor as CargoFuncionario) : '';
}

export interface FiltrosEquipeProps {
  filtros: EstadoFiltrosEquipe;
  aoMudarBusca: (busca: string) => void;
  aoMudarCargo: (cargo: CargoFuncionario | '') => void;
  aoMudarSituacao: (situacao: SituacaoFiltroEquipe) => void;
  /** Campo de busca: a página leva o foco a ele quando o botão que abriu uma ação saiu da lista. */
  refBusca: RefObject<HTMLInputElement>;
}

/** Barra de busca, cargo e situação, acima da tabela. A busca é atrasada pelo pai (espera antes de consultar). */
export function FiltrosEquipe({ filtros, aoMudarBusca, aoMudarCargo, aoMudarSituacao, refBusca }: FiltrosEquipeProps) {
  return (
    <section className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-cartao lg:flex-row lg:items-end lg:justify-between">
      <div className="flex flex-1 flex-col gap-4 sm:flex-row sm:items-end">
        <div className="w-full sm:w-80">
          <Campo
            ref={refBusca}
            rotulo={MENSAGEM_EQUIPE_BUSCA_ROTULO}
            ocultarRotulo
            type="search"
            iconeInicial="search"
            placeholder={MENSAGEM_EQUIPE_BUSCA_PLACEHOLDER}
            maxLength={TAMANHO_MAXIMO_BUSCA_EQUIPE}
            autoComplete="off"
            value={filtros.busca}
            onChange={(evento) => aoMudarBusca(evento.target.value)}
          />
        </div>
        <div className="w-full sm:w-56">
          <Select
            rotulo={MENSAGEM_EQUIPE_CARGO_ROTULO}
            ocultarRotulo
            opcaoVazia={MENSAGEM_EQUIPE_CARGO_TODOS}
            opcoes={OPCOES_CARGO}
            value={filtros.cargo}
            onChange={(evento) => aoMudarCargo(paraCargoFiltro(evento.target.value))}
          />
        </div>
      </div>

      <fieldset className="flex min-w-0 shrink-0 items-center gap-1 self-start rounded-md bg-offwhite p-1 lg:self-auto">
        <legend className="sr-only">{MENSAGEM_EQUIPE_SITUACAO_GRUPO}</legend>
        {OPCOES_SITUACAO_EQUIPE.map((opcao) => {
          const selecionada = filtros.situacao === opcao.valor;
          return (
            <button
              key={opcao.valor}
              type="button"
              aria-pressed={selecionada}
              onClick={() => aoMudarSituacao(opcao.valor)}
              className={`rounded px-4 py-1.5 text-corpo-denso transition-colors ${
                selecionada
                  ? 'bg-branco font-semibold text-navy shadow-cartao'
                  : 'text-texto-suave hover:text-navy'
              }`}
            >
              {opcao.rotulo}
            </button>
          );
        })}
      </fieldset>
    </section>
  );
}
