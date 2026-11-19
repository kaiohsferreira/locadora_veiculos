import { Campo, Icone, Select, type OpcaoSelect } from '@/components/ui';
import {
  MENSAGEM_FROTA_BUSCA_PLACEHOLDER,
  MENSAGEM_FROTA_BUSCA_ROTULO,
  MENSAGEM_FROTA_CATEGORIA_ROTULO,
  MENSAGEM_FROTA_CATEGORIA_TODAS,
  MENSAGEM_FROTA_LIMPAR_BOTAO,
  MENSAGEM_FROTA_LIMPAR_TITULO,
  MENSAGEM_FROTA_STATUS_ROTULO,
  MENSAGEM_FROTA_STATUS_TODOS,
} from '@/lib/mensagens';
import { STATUS_FILTRO_LISTAGEM_FROTA, opcoesStatusListagemFrota } from '@/features/frota/listagem/opcoesListagemFrota';
import { TAMANHO_MAXIMO_BUSCA_FROTA } from '@/features/frota/listagem/parametrosListagemFrota';
import type { StatusVeiculo } from '@/types/enums';

export interface BarraFiltrosListagemFrotaProps {
  /** Texto digitado na busca (o provedor decide quando ele vai para a consulta). */
  busca: string;
  aoMudarBusca: (busca: string) => void;
  /** Categorias já montadas em opções (valor = id). Vazio enquanto carregam. */
  opcoesCategoria: readonly OpcaoSelect[];
  /** Id da categoria escolhida, ou vazio para todas. */
  idCategoria: number | '';
  aoMudarCategoria: (idCategoria: number | '') => void;
  /** Status escolhido, ou vazio para todos. */
  status: StatusVeiculo | '';
  aoMudarStatus: (status: StatusVeiculo | '') => void;
  /** Há algum filtro aplicado: sem filtro, o botão Limpar fica desligado. */
  podeLimpar: boolean;
  aoLimpar: () => void;
}

/**
 * Barra de filtros do desenho 12: busca, categoria e status (listas suspensas) e Limpar. A busca e a categoria são as
 * mesmas da tela 11 (mesmo rótulo e mesmo texto de ajuda); larguras do desenho (busca 320 px, listas 208 px) a partir do
 * tamanho de tela `sm`. Sem o "Geral | Patrimonial" do desenho (fora desta entrega).
 */
export function BarraFiltrosListagemFrota({
  busca,
  aoMudarBusca,
  opcoesCategoria,
  idCategoria,
  aoMudarCategoria,
  status,
  aoMudarStatus,
  podeLimpar,
  aoLimpar,
}: BarraFiltrosListagemFrotaProps) {
  return (
    <div className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-sm lg:flex-row lg:items-center lg:justify-between">
      <div className="flex w-full flex-1 flex-col items-center gap-2 sm:flex-row sm:flex-wrap lg:w-auto">
        <div className="w-full sm:w-80">
          <Campo
            variante="frota"
            type="search"
            rotulo={MENSAGEM_FROTA_BUSCA_ROTULO}
            ocultarRotulo
            prefixo={<Icone nome="search" tamanho={18} className="text-contorno" />}
            placeholder={MENSAGEM_FROTA_BUSCA_PLACEHOLDER}
            maxLength={TAMANHO_MAXIMO_BUSCA_FROTA}
            autoComplete="off"
            value={busca}
            onChange={(evento) => aoMudarBusca(evento.target.value)}
          />
        </div>
        <div className="w-full sm:w-52">
          <Select
            variante="frota"
            rotulo={MENSAGEM_FROTA_CATEGORIA_ROTULO}
            ocultarRotulo
            opcaoVazia={MENSAGEM_FROTA_CATEGORIA_TODAS}
            opcoes={opcoesCategoria}
            value={idCategoria === '' ? '' : String(idCategoria)}
            onChange={(evento) => {
              const valor = evento.target.value;
              aoMudarCategoria(valor === '' ? '' : Number(valor));
            }}
          />
        </div>
        <div className="w-full sm:w-52">
          <Select
            variante="frota"
            rotulo={MENSAGEM_FROTA_STATUS_ROTULO}
            ocultarRotulo
            opcaoVazia={MENSAGEM_FROTA_STATUS_TODOS}
            opcoes={opcoesStatusListagemFrota()}
            value={status}
            onChange={(evento) => {
              const valor = evento.target.value;
              aoMudarStatus(STATUS_FILTRO_LISTAGEM_FROTA.find((situacao) => situacao === valor) ?? '');
            }}
          />
        </div>
      </div>

      <div className="flex w-full items-center justify-end gap-2 lg:w-auto">
        <button
          type="button"
          onClick={aoLimpar}
          disabled={!podeLimpar}
          title={MENSAGEM_FROTA_LIMPAR_TITULO}
          aria-label={MENSAGEM_FROTA_LIMPAR_TITULO}
          className="inline-flex h-10 items-center gap-1 rounded px-2 text-corpo-denso font-semibold text-contorno transition-colors hover:bg-steel-fundo hover:text-texto-desenho focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao disabled:cursor-not-allowed disabled:opacity-40"
        >
          <Icone nome="restart_alt" tamanho={18} />
          <span className="hidden sm:inline">{MENSAGEM_FROTA_LIMPAR_BOTAO}</span>
        </button>
      </div>
    </div>
  );
}
