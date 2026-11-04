import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { Link, type LinkProps } from 'react-router-dom';
import { Icone } from './Icone';

export type VarianteBotao = 'primario' | 'secundario' | 'contorno' | 'fantasma' | 'perigo';
export type TamanhoBotao = 'pequeno' | 'medio' | 'grande';

interface PropsVisuaisBotao {
  /** primario (laranja, ação principal da tela), secundario (navy), contorno, fantasma ou perigo. Padrão: primario. */
  variante?: VarianteBotao;
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

export interface BotaoLinkProps extends LinkProps, PropsVisuaisBotao {}

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
  return (
    <button
      ref={ref}
      type={type}
      disabled={disabled || carregando}
      aria-busy={carregando || undefined}
      className={`${CLASSES_BASE} ${CLASSES_VARIANTE[variante]} ${CLASSES_TAMANHO[tamanho]} ${larguraTotal ? 'w-full' : ''} ${className}`}
      {...props}
    >
      {carregando ? (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden="true"
        />
      ) : (
        icone && <Icone nome={icone} tamanho={TAMANHO_ICONE[tamanho]} />
      )}
      {children}
      {iconeDireita && !carregando && <Icone nome={iconeDireita} tamanho={TAMANHO_ICONE[tamanho]} />}
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
  return (
    <Link
      ref={ref}
      className={`${CLASSES_BASE} ${CLASSES_VARIANTE_LINK[variante]} ${CLASSES_TAMANHO[tamanho]} ${larguraTotal ? 'w-full' : ''} ${className}`}
      {...props}
    >
      {icone && <Icone nome={icone} tamanho={TAMANHO_ICONE[tamanho]} />}
      {children}
      {iconeDireita && <Icone nome={iconeDireita} tamanho={TAMANHO_ICONE[tamanho]} />}
    </Link>
  );
});
