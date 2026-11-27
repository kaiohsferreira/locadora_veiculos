import { Icone } from '@/components/ui';
import {
  SEGUROS_FILTRO_PLACA,
  SEGUROS_FILTRO_PLACA_ETIQUETA,
  SEGUROS_FILTRO_REMOVER_PLACA,
  SEGUROS_FILTRO_SITUACAO,
  SEGUROS_FILTRO_TODAS,
  SEGUROS_FILTRO_VENCENDO,
  textoPilula,
} from '@/lib/mensagensSeguros';
import type { VeiculoPlaca } from '@/types/seguros';
import { CampoVeiculoPlaca } from './CampoVeiculoPlaca';

export interface PilulasSituacaoProps {
  apenasAVencer: boolean;
  /** Contagem de "Todas" (as cadastradas) e de "Vencendo em breve"; ausentes enquanto carregam. */
  totalTodas: number | undefined;
  totalVencendo: number | undefined;
  aoMudarSituacao: (apenasAVencer: boolean) => void;
}

/**
 * Pílulas do desenho 13 no lugar do alternador: "Todas (N)" e "Vencendo em breve (N)". A ativa fica escura; a de
 * vencimento tem o ponto laranja.
 *
 * Fora do desenho por não existirem no sistema: "Vigentes" e "Em renovação".
 */
export function PilulasSituacao({ apenasAVencer, totalTodas, totalVencendo, aoMudarSituacao }: PilulasSituacaoProps) {
  const base = 'inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-3 py-1 text-legenda font-semibold transition-colors';
  return (
    <fieldset className="flex flex-wrap items-center gap-2">
      <legend className="sr-only">{SEGUROS_FILTRO_SITUACAO}</legend>
      <button
        type="button"
        aria-pressed={!apenasAVencer}
        onClick={() => aoMudarSituacao(false)}
        className={`${base} ${!apenasAVencer ? 'bg-navy text-branco' : 'bg-steel-fundo text-navy hover:bg-steel-suave'}`}
      >
        {textoPilula(SEGUROS_FILTRO_TODAS, totalTodas)}
      </button>
      <button
        type="button"
        aria-pressed={apenasAVencer}
        onClick={() => aoMudarSituacao(true)}
        className={`${base} ${apenasAVencer ? 'bg-navy text-branco' : 'bg-status-pendente-fundo text-status-pendente hover:bg-orange-claro'}`}
      >
        <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-orange" />
        {textoPilula(SEGUROS_FILTRO_VENCENDO, totalVencendo)}
      </button>
    </fieldset>
  );
}

export interface FiltroPlacaProps {
  /** Veículo do filtro (com a placa, para a etiqueta), ou nulo para todos. */
  veiculo: { idVeiculo: number; placa: string } | null;
  aoEscolherVeiculo: (veiculo: VeiculoPlaca | null) => void;
}

/** Filtro do veículo pela placa: escolhido, vira etiqueta com ✕. */
export function FiltroPlaca({ veiculo, aoEscolherVeiculo }: FiltroPlacaProps) {
  return (
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
  );
}
