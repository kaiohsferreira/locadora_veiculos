import type { ReactNode } from 'react';

export interface CartaoRetiradaProps {
  children: ReactNode;
  /** Id do título do cartão, para o leitor de tela anunciar a seção. */
  labelledBy?: string;
  /** Sem o respiro interno: o cartão que tem cabeçalho com fundo leva o próprio espaçamento. */
  semEspacamento?: boolean;
  className?: string;
}

/**
 * Cartão branco do desenho 16: canto de 8px (rounded-cartao), sombra padrão do Tailwind e 24px de respiro. Não é o Cartao
 * de `@/components/ui`, que tem cabeçalho com linha separadora que o desenho não tem.
 */
export function CartaoRetirada({ children, labelledBy, semEspacamento = false, className = '' }: CartaoRetiradaProps) {
  return (
    <section aria-labelledby={labelledBy} className={`overflow-hidden rounded-cartao bg-branco shadow-sm ${semEspacamento ? '' : 'p-6'} ${className}`}>
      {children}
    </section>
  );
}
