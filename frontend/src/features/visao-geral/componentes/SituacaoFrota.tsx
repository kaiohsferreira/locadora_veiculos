import { Esqueleto } from '@/components/ui';
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_PAINEL_FROTA_CENTRO,
  MENSAGEM_PAINEL_FROTA_INVENTARIO,
  MENSAGEM_PAINEL_FROTA_LEGENDA,
  MENSAGEM_PAINEL_FROTA_TITULO,
  mensagemPainelFrotaGrafico,
  mensagemPainelFrotaResumo,
} from '@/lib/mensagens';
import type { StatusVeiculo } from '@/types/enums';
import {
  calcularSegmentosRosca,
  type LinhaSituacaoFrota,
} from '@/features/visao-geral/frota';

/** Cores de cada situação (tokens): cor do ponto da legenda e do arco da rosca. */
const COR_SITUACAO: Readonly<Record<StatusVeiculo, { ponto: string; traco: string }>> = {
  LOCADO: { ponto: 'bg-navy', traco: 'stroke-navy' },
  DISPONIVEL: { ponto: 'bg-navy-claro', traco: 'stroke-navy-claro' },
  RESERVADO: { ponto: 'bg-steel', traco: 'stroke-steel' },
  MANUTENCAO: { ponto: 'bg-status-pendente', traco: 'stroke-status-pendente' },
  VENDIDO: { ponto: 'bg-orange', traco: 'stroke-orange' },
  BAIXADO: { ponto: 'bg-status-neutro', traco: 'stroke-status-neutro' },
};

/** Raio do círculo: 2 × π × 15,9155 ≈ 100 unidades, o que faz a conta de percentual direta. */
const RAIO_ROSCA = 15.9155;
const ESPESSURA_ROSCA = 3.8;
/** Id do título do SVG: o nome acessível da rosca vem dele (a legenda ao lado traz os números). */
const ID_ROTULO_ROSCA = 'rotulo-rosca-situacao-frota';

export interface SituacaoFrotaProps {
  /** Legenda pronta (uma linha por situação em operação). Ausente enquanto a consulta carrega. */
  linhas: readonly LinhaSituacaoFrota[] | undefined;
  /** `totalVeiculos` da resposta: é o número do centro da rosca, o mesmo do cartão de ocupação. */
  totalVeiculos: number | undefined;
}

/**
 * Situação da frota em operação (desenho da tela 39): rosca com o total no centro e legenda com a quantidade de cada
 * situação. A legenda é a alternativa textual da rosca; o SVG leva um rótulo com o total.
 */
export function SituacaoFrota({ linhas, totalVeiculos }: SituacaoFrotaProps) {
  return (
    <section
      aria-labelledby="titulo-situacao-frota"
      aria-busy={linhas === undefined ? true : undefined}
      className="flex h-full flex-col gap-4 rounded-cartao bg-branco p-6 shadow-cartao"
    >
      <div className="flex items-center justify-between gap-2">
        <h2 id="titulo-situacao-frota" className="text-secao text-navy">
          {MENSAGEM_PAINEL_FROTA_TITULO}
        </h2>
        <span className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">
          {MENSAGEM_PAINEL_FROTA_INVENTARIO}
        </span>
      </div>

      {linhas === undefined ? (
        <div className="flex flex-col items-center gap-4">
          <Esqueleto className="h-48 w-48 rounded-full" />
          <Esqueleto className="h-4 w-full" />
          <Esqueleto className="h-4 w-full" />
        </div>
      ) : (
        <>
          <Rosca linhas={linhas} total={totalVeiculos ?? 0} />
          <ul aria-label={MENSAGEM_PAINEL_FROTA_LEGENDA} className="flex flex-col">
            {linhas.map((linha) => (
              <li
                key={linha.situacao}
                className="flex items-center justify-between gap-3 border-b border-borda py-2 text-corpo-denso last:border-b-0"
              >
                <span className="flex min-w-0 items-center gap-2">
                  <span className={`h-2.5 w-2.5 shrink-0 rounded-full ${COR_SITUACAO[linha.situacao].ponto}`} aria-hidden="true" />
                  <span className="truncate text-texto">{linha.rotulo}</span>
                </span>
                <span className="shrink-0 font-semibold tabular-nums text-navy">{formatarNumero(linha.quantidade)}</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

interface RoscaProps {
  linhas: readonly LinhaSituacaoFrota[];
  /** Total da frota em operação (o centro e o rótulo do SVG); os arcos vêm das linhas. */
  total: number;
}

/** Rosca em SVG: trilha de fundo e um arco por situação com veículo. Total zero mostra só a trilha. */
function Rosca({ linhas, total }: RoscaProps) {
  const segmentos = calcularSegmentosRosca(linhas);
  return (
    <div className="relative mx-auto h-48 w-48 shrink-0">
      <svg aria-labelledby={ID_ROTULO_ROSCA} viewBox="0 0 36 36" className="h-full w-full -rotate-90">
        <title id={ID_ROTULO_ROSCA}>{mensagemPainelFrotaGrafico(mensagemPainelFrotaResumo(total))}</title>
        <circle cx="18" cy="18" r={RAIO_ROSCA} fill="none" strokeWidth={ESPESSURA_ROSCA} className="stroke-steel-fundo" />
        {segmentos.map((segmento) => (
          <circle
            key={segmento.situacao}
            cx="18"
            cy="18"
            r={RAIO_ROSCA}
            fill="none"
            strokeWidth={ESPESSURA_ROSCA}
            strokeDasharray={`${segmento.comprimento} 100`}
            strokeDashoffset={segmento.deslocamento}
            className={COR_SITUACAO[segmento.situacao].traco}
          />
        ))}
      </svg>
      <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-titulo font-semibold tabular-nums text-navy">{formatarNumero(total)}</span>
        <span className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">
          {MENSAGEM_PAINEL_FROTA_CENTRO}
        </span>
      </div>
    </div>
  );
}
