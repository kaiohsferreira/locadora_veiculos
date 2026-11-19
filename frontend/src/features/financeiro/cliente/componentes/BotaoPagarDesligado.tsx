import { Icone } from '@/components/ui';
import { MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE } from '@/lib/mensagens';

/** Onde o botão aparece: faixa do topo, cartão de pendentes ou linha da tabela. */
export type VarianteBotaoPagarDesligado = 'faixa' | 'cartao' | 'linha';

interface BotaoPagarDesligadoProps {
  variante: VarianteBotaoPagarDesligado;
  /** Texto do botão (vem da camada de mensagens). */
  rotulo: string;
  /** Ícone à esquerda do texto (Material Symbols). */
  icone?: string;
  /** Ícone à direita do texto (Material Symbols). */
  iconeDireita?: string;
  /** Tamanho dos ícones, em px. */
  tamanhoIcone?: number;
}

const CLASSES_VARIANTE: Readonly<Record<VarianteBotaoPagarDesligado, string>> = {
  faixa:
    'inline-flex items-center gap-1 rounded-sm bg-branco px-2 py-1 text-rotulo font-semibold text-frota-oficina shadow-sm',
  cartao: 'text-rotulo font-bold text-frota-oficina',
  linha:
    'inline-flex items-center justify-center gap-1.5 whitespace-nowrap rounded-sm bg-orange-acao px-4 py-2 text-corpo-denso font-semibold text-branco shadow-sm',
};

/**
 * Botão de pagar da tela 30. Fica SEMPRE desligado (disabled e aria-disabled) até a semana 4 (P7-BE-02). Não chama API
 * nem navega. O aviso fica no title e, na faixa e no cartão, também em texto visível ao lado.
 */
export function BotaoPagarDesligado({
  variante,
  rotulo,
  icone,
  iconeDireita,
  tamanhoIcone = 16,
}: BotaoPagarDesligadoProps) {
  return (
    <button
      type="button"
      disabled
      aria-disabled="true"
      title={MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE}
      className={`${CLASSES_VARIANTE[variante]} opacity-60 disabled:cursor-not-allowed`}
    >
      {icone && <Icone nome={icone} tamanho={tamanhoIcone} />}
      {rotulo}
      {iconeDireita && <Icone nome={iconeDireita} tamanho={tamanhoIcone} />}
    </button>
  );
}
