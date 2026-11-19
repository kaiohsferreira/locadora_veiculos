import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { Link, type LinkProps } from 'react-router-dom';
import { Icone } from './Icone';

export type VarianteBotao = 'primario' | 'secundario' | 'contorno' | 'fantasma' | 'perigo';
/**
 * Variantes do back office do cadastro de veículo (tela 05). Têm altura, espaçamento e cantos próprios do desenho, então
 * ignoram `tamanho`. Só existem para essa tela: as variantes de `VarianteBotao` seguem iguais.
 */
export type VarianteBotaoDesenho = 'cinzaAzulado' | 'azulMarinho' | 'laranjaAcao' | 'azulProfundo';
export type TamanhoBotao = 'pequeno' | 'medio' | 'grande';

interface PropsVisuaisBotao {
  /**
   * primario (laranja, ação principal da tela), secundario (navy), contorno, fantasma ou perigo. Variantes da tela 05
   * (`cinzaAzulado`, `azulMarinho`, `laranjaAcao`, `azulProfundo`) têm o visual do desenho. Padrão: primario.
   */
  variante?: VarianteBotao | VarianteBotaoDesenho;
  /** Padrão: medio. */
  tamanho?: TamanhoBotao;
  /** Ocupa toda a largura disponível. */
  larguraTotal?: boolean;
  /** Ícone Material Symbols à esquerda do texto. */
  icone?: string;
  /** Ícone Material Symbols à direita do texto. */
  iconeDireita?: string;
}

export interface BotaoProps extends ButtonHTMLAttributes<HTMLButtonElement>, PropsVisuaisBotao {
  /** Mostra o indicador de carregamento e bloqueia novos cliques. */
  carregando?: boolean;
}

/**
 * Variante do link da tela 11 (Gestão de Frota): o botão laranja "Cadastrar Novo Veículo" do desenho, com 40px, canto de
 * 2px e hover em vermelho-ferrugem. As demais variantes de link seguem iguais.
 */
export type VarianteBotaoLinkDesenho = 'frota';

/** Link com a aparência do Botao: as variantes gerais e a da tela 11 (as da tela 05 são de botão, não de link). */
export interface BotaoLinkProps extends LinkProps, Omit<PropsVisuaisBotao, 'variante'> {
  variante?: VarianteBotao | VarianteBotaoLinkDesenho;
}

const CLASSES_BASE =
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md font-semibold transition-colors ' +
  'disabled:cursor-not-allowed disabled:opacity-60';

const CLASSES_VARIANTE: Readonly<Record<VarianteBotao, string>> = {
  primario: 'bg-orange text-branco enabled:hover:bg-orange-escuro',
  secundario: 'bg-navy text-branco enabled:hover:bg-navy-claro',
  contorno: 'border border-borda-forte bg-branco text-navy enabled:hover:bg-offwhite',
  fantasma: 'bg-transparent text-navy enabled:hover:bg-steel-claro',
  perigo: 'bg-status-perigo text-branco enabled:hover:bg-status-perigo-escuro',
};

/** Links não têm estado "disabled": o hover é aplicado direto. */
const CLASSES_VARIANTE_LINK: Readonly<Record<VarianteBotao, string>> = {
  primario: 'bg-orange text-branco hover:bg-orange-escuro',
  secundario: 'bg-navy text-branco hover:bg-navy-claro',
  contorno: 'border border-borda-forte bg-branco text-navy hover:bg-offwhite',
  fantasma: 'bg-transparent text-navy hover:bg-steel-claro',
  perigo: 'bg-status-perigo text-branco hover:bg-status-perigo-escuro',
};

const CLASSES_TAMANHO: Readonly<Record<TamanhoBotao, string>> = {
  pequeno: 'h-8 px-3 text-corpo-denso',
  medio: 'h-10 px-4 text-corpo-denso',
  grande: 'h-12 px-6 text-corpo',
};

const TAMANHO_ICONE: Readonly<Record<TamanhoBotao, number>> = {
  pequeno: 16,
  medio: 18,
  grande: 20,
};

/** Base das variantes do desenho: 40px, canto de 2px, texto semibold e anel de foco do próprio botão. */
const CLASSES_BASE_DESENHO =
  'inline-flex h-10 items-center justify-center gap-1 whitespace-nowrap rounded-sm text-corpo-denso font-semibold transition-colors ' +
  'disabled:cursor-not-allowed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao';

/** Link da tela 11: 40px, canto de 2px, ícone e texto com 4px entre eles, sombra leve e hover em `frota-oficina`. */
const CLASSES_LINK_FROTA =
  'inline-flex h-10 items-center justify-center gap-1 whitespace-nowrap rounded-sm bg-orange-acao px-4 text-corpo-denso font-semibold text-branco shadow-sm transition-all ' +
  'hover:bg-frota-oficina focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao active:scale-95';

/** Estilo completo de cada variante do desenho (tela 05). Hover só com o botão habilitado, como nas demais variantes. */
const CLASSES_DESENHO: Readonly<Record<VarianteBotaoDesenho, string>> = {
  cinzaAzulado: `${CLASSES_BASE_DESENHO} bg-steel-suave px-4 text-texto-desenho enabled:hover:bg-steel-alto`,
  azulMarinho: `${CLASSES_BASE_DESENHO} bg-navy px-4 text-branco enabled:hover:bg-navy-marinho`,
  laranjaAcao: `${CLASSES_BASE_DESENHO} gap-1.5 bg-orange-acao px-6 font-bold text-branco shadow-acao-laranja enabled:hover:bg-status-pendente`,
  azulProfundo: `${CLASSES_BASE_DESENHO} gap-1.5 bg-navy-profundo px-6 font-bold text-branco shadow-acao-laranja`,
};

function ehVarianteDesenho(variante: VarianteBotao | VarianteBotaoDesenho): variante is VarianteBotaoDesenho {
  return variante in CLASSES_DESENHO;
}

/** Botão de ação. Para navegação, use BotaoLink. */
export const Botao = forwardRef<HTMLButtonElement, BotaoProps>(function Botao(
  {
    variante = 'primario',
    tamanho = 'medio',
    larguraTotal = false,
    icone,
    iconeDireita,
    carregando = false,
    disabled,
    type = 'button',
    className = '',
    children,
    ...props
  },
  ref,
) {
  const desenho = ehVarianteDesenho(variante);
  const classesEstilo = desenho
    ? CLASSES_DESENHO[variante]
    : `${CLASSES_BASE} ${CLASSES_VARIANTE[variante]} ${CLASSES_TAMANHO[tamanho]}`;
  const tamanhoIcone = desenho ? TAMANHO_ICONE.medio : TAMANHO_ICONE[tamanho];

  return (
    <button
      ref={ref}
      type={type}
      disabled={disabled || carregando}
      aria-busy={carregando || undefined}
      className={`${classesEstilo} ${larguraTotal ? 'w-full' : ''} ${className}`}
      {...props}
    >
      {carregando ? (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden="true"
        />
      ) : (
        icone && <Icone nome={icone} tamanho={tamanhoIcone} />
      )}
      {children}
      {iconeDireita && !carregando && <Icone nome={iconeDireita} tamanho={tamanhoIcone} />}
    </button>
  );
});

/** Link de navegação com a aparência do Botao. */
export const BotaoLink = forwardRef<HTMLAnchorElement, BotaoLinkProps>(function BotaoLink(
  {
    variante = 'primario',
    tamanho = 'medio',
    larguraTotal = false,
    icone,
    iconeDireita,
    className = '',
    children,
    ...props
  },
  ref,
) {
  const classesEstilo =
    variante === 'frota'
      ? CLASSES_LINK_FROTA
      : `${CLASSES_BASE} ${CLASSES_VARIANTE_LINK[variante]} ${CLASSES_TAMANHO[tamanho]}`;
  return (
    <Link
      ref={ref}
      className={`${classesEstilo} ${larguraTotal ? 'w-full' : ''} ${className}`}
      {...props}
    >
      {icone && <Icone nome={icone} tamanho={TAMANHO_ICONE[tamanho]} />}
      {children}
      {iconeDireita && <Icone nome={iconeDireita} tamanho={TAMANHO_ICONE[tamanho]} />}
    </Link>
  );
});
