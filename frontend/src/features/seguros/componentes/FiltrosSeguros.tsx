import { Icone } from '@/components/ui';
import {
  SEGUROS_FILTRO_A_VENCER,
  SEGUROS_FILTRO_PLACA,
  SEGUROS_FILTRO_PLACA_ETIQUETA,
  SEGUROS_FILTRO_REMOVER_PLACA,
  SEGUROS_FILTRO_SITUACAO,
  SEGUROS_FILTRO_TODAS,
} from '@/lib/mensagensSeguros';
import type { VeiculoPlaca } from '@/types/seguros';
import { CampoVeiculoPlaca } from './CampoVeiculoPlaca';

export interface FiltrosSegurosProps {
  /** Veículo do filtro (com a placa, para a etiqueta), ou nulo para todos. */
  veiculo: { idVeiculo: number; placa: string } | null;
  apenasAVencer: boolean;
  aoEscolherVeiculo: (veiculo: VeiculoPlaca | null) => void;
  aoMudarSituacao: (apenasAVencer: boolean) => void;
}

/** Botão de uma das duas opções da situação. */
function OpcaoSituacao({ rotulo, ativa, aoEscolher }: { rotulo: string; ativa: boolean; aoEscolher: () => void }) {
  return (
    <button
      type="button"
      aria-pressed={ativa}
      onClick={aoEscolher}
      className={`flex-1 whitespace-nowrap rounded-md px-4 py-2 text-corpo-denso font-semibold transition-colors ${
        ativa ? 'bg-navy text-branco' : 'text-navy hover:bg-steel-claro'
      }`}
    >
      {rotulo}
    </button>
  );
}

/**
 * Faixa de filtros: veículo pela placa (escolhido, vira etiqueta com ✕) e situação "Todas" / "A vencer (60 dias)".
 * No computador ficam lado a lado; no celular, um embaixo do outro.
 */
export function FiltrosSeguros({ veiculo, apenasAVencer, aoEscolherVeiculo, aoMudarSituacao }: FiltrosSegurosProps) {
  return (
    <section className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-cartao md:flex-row md:items-end md:justify-between">
      <div className="w-full md:max-w-sm">
        {veiculo === null ? (
          <CampoVeiculoPlaca rotulo={SEGUROS_FILTRO_PLACA} valor={null} aoEscolher={aoEscolherVeiculo} />
        ) : (
          <div className="flex flex-col gap-1">
            <span className="text-corpo-denso font-medium text-navy">{SEGUROS_FILTRO_PLACA}</span>
            <button
              type="button"
              onClick={() => aoEscolherVeiculo(null)}
              aria-label={`${SEGUROS_FILTRO_REMOVER_PLACA} ${veiculo.placa}`}
              className="inline-flex w-fit items-center gap-1 rounded-full border border-borda-forte bg-branco py-1 pl-3 pr-2 text-corpo-denso text-navy hover:bg-steel-claro"
            >
              {SEGUROS_FILTRO_PLACA_ETIQUETA} {veiculo.placa}
              <Icone nome="close" tamanho={16} />
            </button>
          </div>
        )}
      </div>
      <fieldset className="flex min-w-0 flex-col gap-1 border-0 p-0">
        <legend className="mb-1 text-corpo-denso font-medium text-navy">{SEGUROS_FILTRO_SITUACAO}</legend>
        <div className="flex gap-1 rounded-lg bg-offwhite p-1">
          <OpcaoSituacao rotulo={SEGUROS_FILTRO_TODAS} ativa={!apenasAVencer} aoEscolher={() => aoMudarSituacao(false)} />
          <OpcaoSituacao rotulo={SEGUROS_FILTRO_A_VENCER} ativa={apenasAVencer} aoEscolher={() => aoMudarSituacao(true)} />
        </div>
      </fieldset>
    </section>
  );
}
