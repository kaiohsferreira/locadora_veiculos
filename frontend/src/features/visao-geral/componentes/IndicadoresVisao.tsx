import { Esqueleto, Icone } from '@/components/ui';
import {
  MENSAGEM_PAINEL_CARREGANDO_RESUMO,
  MENSAGEM_PAINEL_INDICADOR_A_RECEBER,
  MENSAGEM_PAINEL_INDICADOR_EM_ATRASO,
  MENSAGEM_PAINEL_INDICADOR_LOCACOES,
  MENSAGEM_PAINEL_INDICADOR_OCUPACAO,
  MENSAGEM_PAINEL_INDICADOR_RECEITA,
} from '@/lib/mensagens';
import type { SentidoVariacao } from '@/lib/formatadoresPainel';
import type { CartaoIndicador, ChaveIndicador, TamanhoValor, TomDetalhe } from '@/features/visao-geral/indicadores';

/** Classes do número por tamanho (tokens de tipografia). */
const CLASSE_TAMANHO: Readonly<Record<TamanhoValor, string>> = {
  grande: 'text-titulo',
  medio: 'text-secao',
  pequeno: 'text-corpo',
  minimo: 'text-corpo-denso',
};

/** Seta e cor da variação: alta sobe em verde, queda desce em vermelho (perigo), neutra fica horizontal e cinza. */
const VISUAL_VARIACAO: Readonly<Record<SentidoVariacao, { icone: string; classe: string }>> = {
  alta: { icone: 'trending_up', classe: 'text-status-sucesso' },
  queda: { icone: 'trending_down', classe: 'text-status-perigo' },
  neutra: { icone: 'trending_flat', classe: 'text-texto-suave' },
};

/** Cor do texto de apoio por tom. */
const CLASSE_DETALHE: Readonly<Record<TomDetalhe, string>> = {
  neutro: 'text-texto-suave',
  destaque: 'text-orange-escuro font-semibold',
  perigo: 'text-status-perigo font-semibold',
};

interface EstiloIndicador {
  /** Faixa de cor embaixo do cartão. */
  faixa: string;
  /** Círculo do ícone. */
  icone: string;
}

/** Cores de cada cartão, na mesma ordem e com as mesmas famílias do desenho. */
const ESTILO_INDICADOR: Readonly<Record<ChaveIndicador, EstiloIndicador>> = {
  receita: { faixa: 'bg-navy-escuro', icone: 'bg-steel-suave text-navy' },
  aReceber: { faixa: 'bg-steel', icone: 'bg-steel-suave text-navy-claro' },
  emAtraso: { faixa: 'bg-status-perigo', icone: 'bg-status-perigo-fundo text-status-perigo' },
  ocupacao: { faixa: 'bg-navy', icone: 'bg-steel-suave text-navy' },
  locacoes: { faixa: 'bg-orange', icone: 'bg-orange-claro text-orange-escuro' },
};

/** Títulos dos cartões enquanto os números carregam (são os mesmos do modelo). */
const CARTOES_CARREGANDO: readonly { chave: ChaveIndicador; titulo: string; icone: string }[] = [
  { chave: 'receita', titulo: MENSAGEM_PAINEL_INDICADOR_RECEITA, icone: 'payments' },
  { chave: 'aReceber', titulo: MENSAGEM_PAINEL_INDICADOR_A_RECEBER, icone: 'receipt_long' },
  { chave: 'emAtraso', titulo: MENSAGEM_PAINEL_INDICADOR_EM_ATRASO, icone: 'warning' },
  { chave: 'ocupacao', titulo: MENSAGEM_PAINEL_INDICADOR_OCUPACAO, icone: 'speed' },
  { chave: 'locacoes', titulo: MENSAGEM_PAINEL_INDICADOR_LOCACOES, icone: 'key' },
];

export interface IndicadoresVisaoProps {
  /** Modelos dos cinco cartões. Ausente enquanto a consulta carrega (aparecem os esqueletos). */
  cartoes: readonly CartaoIndicador[] | undefined;
}

/** Cinco cartões de indicador: 1 coluna no celular, 2 em telas médias e 5 a partir de 1280 px. */
export function IndicadoresVisao({ cartoes }: IndicadoresVisaoProps) {
  if (cartoes === undefined) {
    return (
      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5" aria-label={MENSAGEM_PAINEL_CARREGANDO_RESUMO}>
        {CARTOES_CARREGANDO.map((cartao) => (
          <li key={cartao.chave} aria-hidden="true">
            <CartaoCarregando chave={cartao.chave} titulo={cartao.titulo} icone={cartao.icone} />
          </li>
        ))}
      </ul>
    );
  }

  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
      {cartoes.map((cartao) => (
        <li key={cartao.chave}>
          <CartaoIndicadorVisao cartao={cartao} />
        </li>
      ))}
    </ul>
  );
}

interface CartaoIndicadorVisaoProps {
  cartao: CartaoIndicador;
}

/**
 * Um indicador: título, ícone, número (ou o texto de sem permissão), variação e detalhe. O número tem `whitespace-nowrap`
 * e o tamanho vem de `classificarTamanhoValor`, então um valor de 9 dígitos antes da vírgula não estoura o cartão.
 */
function CartaoIndicadorVisao({ cartao }: CartaoIndicadorVisaoProps) {
  const estilo = ESTILO_INDICADOR[cartao.chave];
  const cor = cartao.destacarValor ? 'text-status-perigo' : 'text-navy';
  const visualVariacao = VISUAL_VARIACAO[cartao.sentidoVariacao ?? 'neutra'];
  return (
    <section
      aria-labelledby={`indicador-${cartao.chave}`}
      className="relative flex h-full min-w-0 flex-col justify-between gap-3 overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao"
    >
      <div className="flex items-start justify-between gap-2">
        <h2 id={`indicador-${cartao.chave}`} className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">
          {cartao.titulo}
        </h2>
        <span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${estilo.icone}`}>
          <Icone nome={cartao.icone} tamanho={18} />
        </span>
      </div>

      <div className="flex min-w-0 flex-col gap-1">
        {cartao.valor !== null ? (
          <p className={`whitespace-nowrap font-semibold tabular-nums ${CLASSE_TAMANHO[cartao.tamanhoValor]} ${cor}`}>
            {cartao.valor}
          </p>
        ) : (
          <p className="text-corpo-denso text-texto-suave">{cartao.semValor}</p>
        )}
        {cartao.variacao !== null && (
          <p className={`flex items-center gap-1 text-legenda font-semibold ${visualVariacao.classe}`}>
            <Icone nome={visualVariacao.icone} tamanho={16} />
            <span>{cartao.variacao}</span>
          </p>
        )}
      </div>

      {cartao.detalhe !== null && (
        <p className={`flex items-center gap-1.5 text-legenda ${CLASSE_DETALHE[cartao.tomDetalhe]}`}>
          {cartao.tomDetalhe === 'perigo' && <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-status-perigo" aria-hidden="true" />}
          <span>{cartao.detalhe}</span>
        </p>
      )}

      <span aria-hidden="true" className={`absolute inset-x-0 bottom-0 h-1 ${estilo.faixa}`} />
    </section>
  );
}

interface CartaoCarregandoProps {
  chave: ChaveIndicador;
  titulo: string;
  icone: string;
}

/** Cartão em carregamento: título e ícone reais, número e detalhe como esqueletos. */
function CartaoCarregando({ chave, titulo, icone }: CartaoCarregandoProps) {
  const estilo = ESTILO_INDICADOR[chave];
  return (
    <section className="relative flex h-full min-w-0 flex-col justify-between gap-3 overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao">
      <div className="flex items-start justify-between gap-2">
        <p className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">{titulo}</p>
        <span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${estilo.icone}`}>
          <Icone nome={icone} tamanho={18} />
        </span>
      </div>
      <div className="flex flex-col gap-2">
        <Esqueleto className="h-7 w-28" />
        <Esqueleto className="h-3 w-20" />
      </div>
      <span className={`absolute inset-x-0 bottom-0 h-1 ${estilo.faixa}`} />
    </section>
  );
}
