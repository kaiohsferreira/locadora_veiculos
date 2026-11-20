import { Campo, Icone, Select, type OpcaoSelect } from '@/components/ui';
import {
  MENSAGEM_MANUTENCAO_ATUALIZAR,
  MENSAGEM_MANUTENCAO_BUSCA,
  MENSAGEM_MANUTENCAO_FILTRO_STATUS,
  MENSAGEM_MANUTENCAO_FILTRO_TIPO,
  MENSAGEM_MANUTENCAO_LIMPAR_FILTROS,
  MENSAGEM_MANUTENCAO_STATUS_CONCLUIDA,
  MENSAGEM_MANUTENCAO_STATUS_EXECUCAO,
  MENSAGEM_MANUTENCAO_STATUS_TODOS,
  MENSAGEM_MANUTENCAO_TIPO_TODAS,
  MENSAGEM_MANUTENCAO_COLUNA_TIPO,
} from '@/lib/mensagens';
import { TAMANHO_MAXIMO_BUSCA_MANUTENCAO } from '@/features/manutencao/listagem/filtrosManutencao';
import { ROTULO_TIPO_MANUTENCAO, type TipoManutencao } from '@/types/enums';
import type { SituacaoOrdemManutencao } from '@/types/manutencao';

const OPCOES_TIPO: readonly { valor: TipoManutencao | ''; rotulo: string }[] = [
  { valor: '', rotulo: MENSAGEM_MANUTENCAO_TIPO_TODAS },
  { valor: 'PREVENTIVA', rotulo: ROTULO_TIPO_MANUTENCAO.PREVENTIVA },
  { valor: 'CORRETIVA', rotulo: ROTULO_TIPO_MANUTENCAO.CORRETIVA },
];

/** Status do desenho: "Em Execução" é a ordem aberta; "Concluída", a encerrada. */
const OPCOES_SITUACAO: readonly OpcaoSelect[] = [
  { valor: 'ABERTA', rotulo: MENSAGEM_MANUTENCAO_STATUS_EXECUCAO },
  { valor: 'ENCERRADA', rotulo: MENSAGEM_MANUTENCAO_STATUS_CONCLUIDA },
];

export interface BarraFiltrosManutencaoProps {
  /** Texto digitado na busca. */
  busca: string;
  aoMudarBusca: (busca: string) => void;
  tipo: TipoManutencao | '';
  aoMudarTipo: (tipo: TipoManutencao | '') => void;
  situacao: SituacaoOrdemManutencao | '';
  aoMudarSituacao: (situacao: SituacaoOrdemManutencao | '') => void;
  /** Há algum filtro aplicado: só então aparece o Limpar filtros. */
  podeLimpar: boolean;
  aoLimpar: () => void;
  /** Refaz as consultas da tela (Atualizar). */
  aoAtualizar: () => void;
  atualizando: boolean;
}

/**
 * Barra de filtros da tabela (desenho 14, seção 2): busca por placa, modelo ou código OS, Tipo (Todas, Preventiva,
 * Corretiva) e Status. Colunas foi cortado pelo P7; Atualizar fica só com o ícone, como no desenho.
 */
export function BarraFiltrosManutencao({
  busca,
  aoMudarBusca,
  tipo,
  aoMudarTipo,
  situacao,
  aoMudarSituacao,
  podeLimpar,
  aoLimpar,
  aoAtualizar,
  atualizando,
}: BarraFiltrosManutencaoProps) {
  return (
    <div className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-sm md:flex-row md:items-center md:justify-between">
      <div className="flex w-full flex-1 flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-center md:w-auto">
        <div className="w-full sm:w-80">
          <Campo
            variante="frota"
            type="search"
            rotulo={MENSAGEM_MANUTENCAO_BUSCA}
            ocultarRotulo
            prefixo={<Icone nome="search" tamanho={18} className="text-contorno" />}
            placeholder={MENSAGEM_MANUTENCAO_BUSCA}
            maxLength={TAMANHO_MAXIMO_BUSCA_MANUTENCAO}
            autoComplete="off"
            value={busca}
            onChange={(evento) => aoMudarBusca(evento.target.value)}
          />
        </div>

        <div className="flex items-center gap-2">
          <span className="text-micro font-semibold text-texto-apoio">{MENSAGEM_MANUTENCAO_FILTRO_TIPO}</span>
          <fieldset aria-label={MENSAGEM_MANUTENCAO_COLUNA_TIPO} className="flex rounded bg-steel-fundo p-1">
            {OPCOES_TIPO.map((opcao) => {
              const ativo = opcao.valor === tipo;
              return (
                <button
                  key={opcao.rotulo}
                  type="button"
                  aria-pressed={ativo}
                  onClick={() => aoMudarTipo(opcao.valor)}
                  className={`rounded-sm px-2 py-1 text-rotulo font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao ${
                    ativo ? 'bg-branco text-texto-desenho shadow-sm' : 'text-texto-apoio hover:text-texto-desenho'
                  }`}
                >
                  {opcao.rotulo}
                </button>
              );
            })}
          </fieldset>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-micro font-semibold text-texto-apoio">{MENSAGEM_MANUTENCAO_FILTRO_STATUS}</span>
          <div className="w-full sm:w-52">
            <Select
              variante="frota"
              rotulo={MENSAGEM_MANUTENCAO_FILTRO_STATUS}
              ocultarRotulo
              opcaoVazia={MENSAGEM_MANUTENCAO_STATUS_TODOS}
              opcoes={OPCOES_SITUACAO}
              value={situacao}
              onChange={(evento) => {
                const valor = evento.target.value;
                aoMudarSituacao(valor === 'ABERTA' || valor === 'ENCERRADA' ? valor : '');
              }}
            />
          </div>
        </div>
      </div>

      <div className="flex w-full items-center justify-end gap-2 md:w-auto">
        {podeLimpar && (
          <button
            type="button"
            onClick={aoLimpar}
            className="inline-flex h-10 items-center gap-1 rounded-sm px-2 text-corpo-denso font-semibold text-contorno transition-colors hover:bg-steel-fundo hover:text-texto-desenho focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
          >
            <Icone nome="restart_alt" tamanho={18} />
            {MENSAGEM_MANUTENCAO_LIMPAR_FILTROS}
          </button>
        )}
        <button
          type="button"
          onClick={aoAtualizar}
          disabled={atualizando}
          title={MENSAGEM_MANUTENCAO_ATUALIZAR}
          aria-label={MENSAGEM_MANUTENCAO_ATUALIZAR}
          className="inline-flex h-10 items-center justify-center rounded bg-steel-fundo px-2 text-texto-desenho transition-colors hover:bg-steel-alto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Icone nome="refresh" tamanho={18} />
        </button>
      </div>
    </div>
  );
}
