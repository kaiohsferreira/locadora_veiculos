import { Icone } from '@/components/ui';

/** Onde o botão aparece: faixa do topo, cartão de pendentes ou linha da tabela. */
export type VarianteBotaoPagar = 'faixa' | 'cartao' | 'linha';

export interface BotaoPagarProps {
  variante: VarianteBotaoPagar;
  /** Texto do botão (vem da camada de mensagens). */
  rotulo: string;
  /** Ícone à esquerda do texto (Material Symbols). */
  icone?: string;
  /** Ícone à direita do texto (Material Symbols). */
  iconeDireita?: string;
  /** Tamanho dos ícones, em px. */
  tamanhoIcone?: number;
  /** Busca em andamento: bloqueia o clique até a cobrança chegar. */
  carregando?: boolean;
  /** Abre a gaveta ou a aba. Recebe o próprio botão, que vira o gatilho (o foco volta a ele ao fechar a gaveta). */
  aoPagar: (gatilho: HTMLElement) => void;
}

const CLASSES_VARIANTE: Readonly<Record<VarianteBotaoPagar, string>> = {
  faixa:
    'inline-flex items-center gap-1 rounded-sm bg-branco px-2 py-1 text-rotulo font-semibold text-frota-oficina shadow-sm',
  cartao: 'text-rotulo font-bold text-frota-oficina',
  linha:
    'inline-flex items-center justify-center gap-1.5 whitespace-nowrap rounded-sm bg-orange-acao px-4 py-2 text-corpo-denso font-semibold text-branco shadow-sm',
};

/**
 * Botão de pagar da tela 30 (ligado). Não chama a API: quem decide o que fazer é a tela, pelo `aoPagar`. Com `carregando`,
 * fica bloqueado enquanto a cobrança é buscada.
 */
export function BotaoPagar({
  variante,
  rotulo,
  icone,
  iconeDireita,
  tamanhoIcone = 16,
  carregando = false,
  aoPagar,
}: BotaoPagarProps) {
  return (
    <button
      type="button"
      disabled={carregando}
      aria-busy={carregando || undefined}
      onClick={(evento) => aoPagar(evento.currentTarget)}
      className={`${CLASSES_VARIANTE[variante]} transition-colors hover:opacity-90 disabled:cursor-wait`}
    >
      {icone && <Icone nome={icone} tamanho={tamanhoIcone} />}
      {rotulo}
      {iconeDireita && <Icone nome={iconeDireita} tamanho={tamanhoIcone} />}
    </button>
  );
}
