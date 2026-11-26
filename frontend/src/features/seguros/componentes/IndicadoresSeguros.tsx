import { Esqueleto, Icone } from '@/components/ui';
import { proporcaoAVencer } from '@/features/seguros/regras/vencimento';
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import {
  SEGUROS_INDICADOR_A_VENCER,
  SEGUROS_INDICADOR_A_VENCER_ACAO,
  SEGUROS_INDICADOR_A_VENCER_DETALHE,
  SEGUROS_INDICADOR_CADASTRADAS,
  SEGUROS_INDICADOR_CADASTRADAS_DETALHE,
  SEGUROS_SELO_RENOVACAO,
} from '@/lib/mensagensSeguros';

export interface IndicadoresSegurosProps {
  /** Total de apólices cadastradas (todas, inclusive as vencidas). Ausente enquanto carrega ou se falhou. */
  totalCadastradas: number | undefined;
  /** Total das que vencem de hoje até 60 dias. */
  totalAVencer: number | undefined;
  carregando: boolean;
  /** Clicar no cartão dos 60 dias liga o filtro "Vencendo em breve". */
  aoVerAVencer: () => void;
}

/** Número do indicador: esqueleto enquanto carrega; "—" quando a consulta falhou. */
function Numero({ valor, carregando, cor }: { valor: number | undefined; carregando: boolean; cor: string }) {
  if (carregando && valor === undefined) return <Esqueleto className="h-8 w-16" />;
  return <span className={`text-titulo font-semibold ${cor}`}>{valor === undefined ? VALOR_AUSENTE : formatarNumero(valor)}</span>;
}

/**
 * Cartões em linha do desenho 13 (os dois que têm dado no sistema): "Apólices cadastradas", com a barra cheia, e
 * "Apólices a vencer (≤ 60 dias)", com o selo "Requer renovação imediata" e a barra proporcional às cadastradas. O
 * segundo é um botão que liga o filtro "Vencendo em breve".
 *
 * Fora do desenho por não existirem no sistema: "Sinistros em regulação" e "Custo mensal em prêmios".
 */
export function IndicadoresSeguros({ totalCadastradas, totalAVencer, carregando, aoVerAVencer }: IndicadoresSegurosProps) {
  const proporcao = proporcaoAVencer(totalAVencer, totalCadastradas);
  const temAVencer = totalAVencer !== undefined && totalAVencer > 0;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <section className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-cartao">
        <div className="flex items-start justify-between gap-2">
          <p className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">{SEGUROS_INDICADOR_CADASTRADAS}</p>
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-steel-suave text-navy">
            <Icone nome="verified_user" tamanho={20} />
          </span>
        </div>
        <div className="flex flex-wrap items-baseline gap-2">
          <Numero valor={totalCadastradas} carregando={carregando} cor="text-navy" />
          <span className="text-corpo-denso text-texto-suave">{SEGUROS_INDICADOR_CADASTRADAS_DETALHE}</span>
        </div>
        <span aria-hidden="true" className="mt-auto block h-1.5 w-full rounded-full bg-navy" />
      </section>

      <button
        type="button"
        onClick={aoVerAVencer}
        title={SEGUROS_INDICADOR_A_VENCER_ACAO}
        className="flex flex-col gap-4 rounded-cartao bg-branco p-4 text-left shadow-cartao transition-shadow hover:shadow-elevado focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
      >
        <span className="flex w-full items-start justify-between gap-2">
          <span className="text-legenda font-semibold uppercase tracking-wide text-status-pendente">{SEGUROS_INDICADOR_A_VENCER}</span>
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-status-pendente-fundo text-status-pendente">
            <Icone nome="warning" tamanho={20} />
          </span>
        </span>
        <span className="flex flex-col gap-1">
          <span className="flex flex-wrap items-baseline gap-2">
            <Numero valor={totalAVencer} carregando={carregando} cor="text-status-pendente" />
            <span className="text-corpo-denso text-texto-suave">{SEGUROS_INDICADOR_A_VENCER_DETALHE}</span>
          </span>
          {temAVencer && (
            <span
              className="inline-flex w-fit items-center gap-1 rounded bg-status-pendente-fundo px-2 py-0.5 text-legenda font-semibold text-status-pendente"
              data-testid="selo-renovacao"
            >
              <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-status-pendente" />
              {SEGUROS_SELO_RENOVACAO}
            </span>
          )}
        </span>
        <span aria-hidden="true" className="mt-auto block h-1.5 w-full overflow-hidden rounded-full bg-steel-claro">
          <span className="block h-full rounded-full bg-orange" style={{ width: `${proporcao}%` }} data-testid="barra-a-vencer" />
        </span>
      </button>
    </div>
  );
}
