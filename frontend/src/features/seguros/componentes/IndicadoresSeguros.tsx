import { Esqueleto, Icone } from '@/components/ui';
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import {
  SEGUROS_INDICADOR_A_VENCER,
  SEGUROS_INDICADOR_A_VENCER_ACAO,
  SEGUROS_INDICADOR_A_VENCER_DETALHE,
  SEGUROS_INDICADOR_CADASTRADAS,
  SEGUROS_INDICADOR_CADASTRADAS_DETALHE,
} from '@/lib/mensagensSeguros';

export interface IndicadoresSegurosProps {
  /** Total de apólices cadastradas (todas, inclusive as vencidas). Ausente enquanto carrega ou se falhou. */
  totalCadastradas: number | undefined;
  /** Total das que vencem de hoje até 60 dias. */
  totalAVencer: number | undefined;
  carregando: boolean;
  /** Clicar no indicador "Vencem em até 60 dias" liga o filtro "A vencer". */
  aoVerAVencer: () => void;
}

/** Número do indicador: esqueleto enquanto carrega; "—" quando a consulta falhou. */
function Numero({ valor, carregando, cor }: { valor: number | undefined; carregando: boolean; cor: string }) {
  if (carregando && valor === undefined) return <Esqueleto className="h-8 w-16" />;
  return <span className={`text-titulo font-semibold ${cor}`}>{valor === undefined ? VALOR_AUSENTE : formatarNumero(valor)}</span>;
}

/**
 * Dois indicadores, no molde dos da Equipe: "Apólices cadastradas" (todas, inclusive as vencidas) e "Vencem em até 60
 * dias". O segundo é um botão que liga o filtro "A vencer".
 */
export function IndicadoresSeguros({ totalCadastradas, totalAVencer, carregando, aoVerAVencer }: IndicadoresSegurosProps) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:max-w-2xl">
      <section className="relative flex flex-col gap-3 overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao">
        <p className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">{SEGUROS_INDICADOR_CADASTRADAS}</p>
        <div className="flex flex-wrap items-baseline gap-2">
          <Numero valor={totalCadastradas} carregando={carregando} cor="text-navy" />
          <span className="text-corpo-denso text-texto-suave">{SEGUROS_INDICADOR_CADASTRADAS_DETALHE}</span>
        </div>
        <span aria-hidden="true" className="absolute inset-x-0 bottom-0 h-1 bg-navy" />
      </section>
      <button
        type="button"
        onClick={aoVerAVencer}
        title={SEGUROS_INDICADOR_A_VENCER_ACAO}
        className="relative flex flex-col gap-3 overflow-hidden rounded-cartao bg-branco p-4 text-left shadow-cartao transition-shadow hover:shadow-elevado focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
      >
        <span className="flex items-center gap-1 text-legenda font-semibold uppercase tracking-wide text-texto-suave">
          <Icone nome="schedule" tamanho={14} />
          {SEGUROS_INDICADOR_A_VENCER}
        </span>
        <span className="flex flex-wrap items-baseline gap-2">
          <Numero valor={totalAVencer} carregando={carregando} cor="text-orange-escuro" />
          <span className="text-corpo-denso text-texto-suave">{SEGUROS_INDICADOR_A_VENCER_DETALHE}</span>
        </span>
        <span aria-hidden="true" className="absolute inset-x-0 bottom-0 h-1 bg-orange" />
      </button>
    </div>
  );
}
