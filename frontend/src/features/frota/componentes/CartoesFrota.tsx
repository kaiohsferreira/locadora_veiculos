import { Esqueleto, Icone } from '@/components/ui';
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGEM_FROTA_CARREGANDO_RESUMO, MENSAGEM_FROTA_NOVOS_NO_MES } from '@/lib/mensagens';
import type { CartaoFrota, ChaveCartaoFrota } from '@/features/frota/cartoes/cartoesFrota';

/** Cores de cada cartão, com as famílias do desenho 11. */
const ESTILO_CARTAO: Readonly<
  Record<ChaveCartaoFrota, { icone: string; faixa: string; chip: string; apoio: string }>
> = {
  total: { icone: 'bg-steel-suave text-navy', faixa: 'bg-navy', chip: '', apoio: '' },
  disponiveis: {
    icone: 'bg-frota-disponivel-fundo text-frota-disponivel',
    faixa: 'bg-frota-disponivel-ponto',
    chip: 'bg-frota-disponivel-fundo font-bold text-frota-disponivel',
    apoio: 'font-semibold text-frota-disponivel',
  },
  locados: {
    icone: 'bg-steel-suave text-orange-acao',
    faixa: 'bg-orange-acao',
    chip: 'bg-steel-alto font-bold text-navy',
    apoio: 'font-medium text-texto-apoio',
  },
  oficina: {
    icone: 'bg-frota-oficina-fundo text-frota-oficina',
    faixa: 'bg-frota-oficina',
    chip: 'bg-frota-oficina-fundo font-bold text-frota-oficina',
    apoio: 'font-semibold text-frota-oficina',
  },
  reservados: {
    icone: 'bg-steel-alto text-navy',
    faixa: 'bg-steel',
    chip: 'bg-steel-alto font-bold text-navy',
    apoio: 'font-medium text-texto-apoio',
  },
};

/** Ordem dos cartões no esqueleto de carregamento (a mesma da tela). */
const CHAVES_CARREGANDO: readonly ChaveCartaoFrota[] = ['total', 'disponiveis', 'locados', 'oficina', 'reservados'];

export interface CartoesFrotaProps {
  /** Cinco cartões prontos. Ausente enquanto o resumo carrega (aparecem os esqueletos). */
  cartoes: readonly CartaoFrota[] | undefined;
}

/** Cinco cartões da frota: 1 coluna no celular, 2 em telas médias e 5 a partir de 1024 px. */
export function CartoesFrota({ cartoes }: CartoesFrotaProps) {
  if (cartoes === undefined) {
    return (
      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5" aria-label={MENSAGEM_FROTA_CARREGANDO_RESUMO}>
        {CHAVES_CARREGANDO.map((chave) => (
          <li key={chave} aria-hidden="true">
            <section className="relative flex h-full min-w-0 flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-sm">
              <Esqueleto className="h-4 w-24" />
              <div className="mt-3 flex flex-col gap-2">
                <Esqueleto className="h-8 w-20" />
                <Esqueleto className="h-3 w-28" />
              </div>
            </section>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
      {cartoes.map((cartao) => (
        <li key={cartao.chave} className="min-w-0">
          <CartaoFrotaVisao cartao={cartao} />
        </li>
      ))}
    </ul>
  );
}

interface CartaoFrotaVisaoProps {
  cartao: CartaoFrota;
}

/** Um cartão: título e ícone, número com a unidade, percentual e chip. O cartão total mostra os novos do mês. */
function CartaoFrotaVisao({ cartao }: CartaoFrotaVisaoProps) {
  const estilo = ESTILO_CARTAO[cartao.chave];
  const mostraNovos = cartao.chave === 'total' && cartao.novosNoMes !== null;
  return (
    <section
      aria-labelledby={`cartao-frota-${cartao.chave}`}
      className="relative flex h-full min-w-0 flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-sm"
    >
      <div className="flex items-center justify-between gap-2">
        <h2
          id={`cartao-frota-${cartao.chave}`}
          className="text-rotulo font-medium uppercase tracking-wider text-texto-apoio"
        >
          {cartao.titulo}
        </h2>
        <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-sm ${estilo.icone}`}>
          <Icone nome={cartao.icone} tamanho={18} />
        </span>
      </div>

      <div className="mt-3 flex min-w-0 flex-col gap-1">
        <div className="flex flex-wrap items-baseline gap-1">
          <span className="whitespace-nowrap text-titulo-acesso font-bold text-texto-desenho">{cartao.valor}</span>
          <span className="text-micro text-texto-apoio">{cartao.unidade}</span>
        </div>

        {mostraNovos && cartao.novosNoMes !== null && (
          <p className="flex items-center gap-1 text-micro font-medium text-texto-apoio">
            {/* Seta e "+N" juntos, sem espaço entre eles, como no desenho. A seta só aparece com novos no mês. */}
            <span className="inline-flex items-center font-semibold text-frota-disponivel">
              {cartao.novosNoMes > 0 && <Icone nome="arrow_upward" tamanho={14} />}
              <span>+{formatarNumero(cartao.novosNoMes)}</span>
            </span>
            <span>{MENSAGEM_FROTA_NOVOS_NO_MES}</span>
          </p>
        )}

        {!mostraNovos && cartao.chave !== 'total' && (
          <div className="flex items-center justify-between gap-2">
            <span className={`text-micro ${estilo.apoio}`}>{cartao.apoio ?? VALOR_AUSENTE}</span>
            {cartao.chip !== null && (
              <span
                className={`inline-flex shrink-0 items-center rounded-sm px-1.5 py-0.5 text-chip-frota uppercase ${estilo.chip}`}
              >
                {cartao.chip}
              </span>
            )}
          </div>
        )}
      </div>

      <span aria-hidden="true" className={`absolute inset-x-0 bottom-0 h-1 ${estilo.faixa}`} />
    </section>
  );
}
