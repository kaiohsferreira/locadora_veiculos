import { Icone, ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { formatarPlaca, formatarQuilometragem } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_BLOQUEAR_OFICINA,
  MENSAGEM_MANUTENCAO_BOTAO_ABRIR_ORDEM,
  MENSAGEM_MANUTENCAO_CRITICO,
  MENSAGEM_MANUTENCAO_PROXIMA_ROTULO,
  MENSAGEM_MANUTENCAO_VER_HISTORICO,
  mensagemFotoAlt,
  mensagemManutencaoCategoria,
} from '@/lib/mensagens';
import { faixaDoAlerta, larguraBarraCiclo, seloDoAlerta, textoCicloDoAlerta, textoFaltamDoAlerta, type FaixaAlerta } from '@/features/manutencao/alertas/faixaAlerta';
import type { TipoManutencao } from '@/types/enums';
import type { AlertaRevisaoRetorno } from '@/types/manutencao';

/** Classes de cada faixa, só com tokens do tema (desenho 14, cartões 1 a 4). */
interface EstiloFaixa {
  /** Anel e sombra do cartão. */
  cartao: string;
  /** Faixa do topo do cartão. */
  barraTopo: string;
  /** Trilho da barra do ciclo. */
  trilho: string;
  /** Barra do ciclo. */
  barra: string;
  /** Linha "Faltam X km". */
  textoFaltam: string;
  /** Selo de km sobre a foto. */
  kmFundo: string;
  kmIcone: string;
  kmIconeCor: string;
}

const ESTILO_POR_FAIXA: Readonly<Record<FaixaAlerta, EstiloFaixa>> = {
  VERMELHA: {
    cartao: 'shadow-md ring-1 ring-manutencao-alerta/20',
    barraTopo: 'bg-manutencao-alerta',
    trilho: 'bg-manutencao-alerta-trilho',
    barra: 'bg-manutencao-alerta motion-safe:animate-pulse',
    textoFaltam: 'font-bold text-manutencao-alerta',
    kmFundo: 'bg-manutencao-alerta font-semibold text-branco',
    kmIcone: 'emergency',
    kmIconeCor: 'text-branco',
  },
  LARANJA: {
    cartao: 'shadow-sm',
    barraTopo: 'bg-orange-acao',
    trilho: 'bg-steel-suave',
    barra: 'bg-orange-acao',
    textoFaltam: 'font-semibold text-frota-oficina',
    kmFundo: 'bg-navy/80 text-branco',
    kmIcone: 'speed',
    kmIconeCor: 'text-orange-acao',
  },
  NEUTRA: {
    cartao: 'shadow-sm',
    barraTopo: 'bg-manutencao-neutro',
    trilho: 'bg-steel-suave',
    barra: 'bg-manutencao-neutro',
    textoFaltam: 'font-semibold text-texto-apoio',
    kmFundo: 'bg-navy/80 text-branco',
    kmIcone: 'speed',
    kmIconeCor: 'text-manutencao-neutro-icone',
  },
};

export interface CartaoAlertaRevisaoProps {
  alerta: AlertaRevisaoRetorno;
  /**
   * Abre o modal de abrir ordem com o veículo preenchido. Bloquear p/ Oficina passa o tipo Preventiva; Abrir ordem não
   * passa tipo (o campo fica vazio).
   */
  aoAbrirOrdem: (idVeiculo: number, tipoInicial?: TipoManutencao) => void;
  /** Filtra a tabela pelas ordens deste veículo (Ver Histórico). */
  aoVerHistorico: (idVeiculo: number) => void;
  /** Chamado na primeira falha da capa (a página refaz a consulta, uma vez por visita). */
  aoFalharFotoCapa: () => void;
}

/**
 * Cartão de um veículo perto da revisão (desenho 14, cartões 1 a 4). A faixa (vermelha, laranja ou azul-cinza) sai da
 * situação e dos km que faltam. Só o vermelho tem Bloquear p/ Oficina e Ver Histórico; os outros têm Abrir ordem.
 */
export function CartaoAlertaRevisao({ alerta, aoAbrirOrdem, aoVerHistorico, aoFalharFotoCapa }: CartaoAlertaRevisaoProps) {
  const faixa = faixaDoAlerta(alerta);
  const vermelha = faixa === 'VERMELHA';
  const estilo = ESTILO_POR_FAIXA[faixa];
  const nome = `${alerta.marca} ${alerta.modelo}`;
  const selo = seloDoAlerta(alerta);
  const larguraBarra = larguraBarraCiclo(alerta.percentualCiclo);

  return (
    <article
      aria-label={nome}
      className={`group relative flex flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 ${estilo.cartao}`}
    >
      <span aria-hidden="true" className={`absolute inset-x-0 top-0 h-1 ${estilo.barraTopo}`} />

      <div className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex min-w-0 flex-col">
            {selo !== null ? (
              <span className="flex items-center gap-0.5 text-micro font-bold uppercase tracking-wider text-manutencao-alerta">
                <Icone nome="warning" tamanho={14} />
                {selo}
              </span>
            ) : (
              <span className="text-micro font-semibold uppercase tracking-wider text-texto-apoio">
                {mensagemManutencaoCategoria(alerta.categoriaNome)}
              </span>
            )}
            <h3 className="text-titulo-md font-bold leading-tight text-texto-desenho">{nome}</h3>
          </div>
          <span className="shrink-0 rounded-sm bg-steel-alto px-1 py-0.5 text-dado-tabular font-bold tracking-wide text-texto-desenho">
            {formatarPlaca(alerta.placa)}
          </span>
        </div>

        <div className="relative my-1">
          <ImagemVeiculo
            url={alerta.fotoCapa ? urlDaImagem(alerta.fotoCapa) : null}
            alt={mensagemFotoAlt(nome)}
            className="h-28 w-full rounded"
            tamanhoIcone={32}
            aoFalhar={aoFalharFotoCapa}
          />
          <span
            className={`absolute bottom-2 left-2 flex items-center gap-1 rounded-sm px-2 py-0.5 text-micro backdrop-blur ${estilo.kmFundo}`}
          >
            <Icone nome={estilo.kmIcone} tamanho={13} className={estilo.kmIconeCor} />
            <span>{formatarQuilometragem(alerta.kmAtual)}</span>
          </span>
        </div>

        <div className="flex flex-col gap-1.5">
          <div className="flex items-baseline justify-between gap-2 text-apoio">
            <span className="text-texto-apoio">
              {MENSAGEM_MANUTENCAO_PROXIMA_ROTULO}{' '}
              <strong className="text-texto-desenho">{formatarQuilometragem(alerta.kmProximaRevisao)}</strong>
            </span>
            <span className={`whitespace-nowrap ${estilo.textoFaltam}`}>{textoFaltamDoAlerta(alerta)}</span>
          </div>
          <div className={`h-2 w-full overflow-hidden rounded-full ${estilo.trilho}`}>
            <div className={`h-full rounded-full ${estilo.barra}`} style={{ width: `${larguraBarra}%` }} />
          </div>
          <div className="flex items-center justify-between gap-2 text-micro text-texto-apoio">
            <span className={vermelha ? 'font-semibold text-manutencao-alerta' : undefined}>
              {textoCicloDoAlerta(alerta)}
            </span>
            {vermelha && <span className="font-bold text-manutencao-alerta">{MENSAGEM_MANUTENCAO_CRITICO}</span>}
          </div>
        </div>
      </div>

      <div className="mt-2 flex gap-2 pt-4">
        {vermelha ? (
          <>
            <button
              type="button"
              onClick={() => aoAbrirOrdem(alerta.idVeiculo, 'PREVENTIVA')}
              className="inline-flex h-9 flex-1 items-center justify-center gap-1 rounded bg-manutencao-alerta text-corpo-denso font-semibold text-branco shadow-sm transition-colors enabled:hover:bg-manutencao-alerta-escuro focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
            >
              <Icone nome="lock" tamanho={16} />
              {MENSAGEM_MANUTENCAO_BLOQUEAR_OFICINA}
            </button>
            <button
              type="button"
              title={MENSAGEM_MANUTENCAO_VER_HISTORICO}
              aria-label={MENSAGEM_MANUTENCAO_VER_HISTORICO}
              onClick={() => aoVerHistorico(alerta.idVeiculo)}
              className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded bg-steel-suave text-texto-desenho transition-colors hover:bg-steel-alto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
            >
              <Icone nome="history" tamanho={18} />
            </button>
          </>
        ) : (
          <button
            type="button"
            onClick={() => aoAbrirOrdem(alerta.idVeiculo)}
            className="inline-flex h-9 flex-1 items-center justify-center gap-1 rounded bg-steel-fundo text-corpo-denso font-semibold text-texto-desenho transition-colors hover:bg-steel-alto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
          >
            <Icone nome="calendar_today" tamanho={16} />
            {MENSAGEM_MANUTENCAO_BOTAO_ABRIR_ORDEM}
          </button>
        )}
      </div>
    </article>
  );
}
