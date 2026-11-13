import type { ReactNode } from 'react';

export interface CartaoRecuperacaoProps {
  children: ReactNode;
}

/**
 * Cartão centralizado das telas de recuperação de senha. Largura máxima de medio (28rem), com
 * margem lateral: sem rolagem horizontal a partir de 375 px.
 */
export function CartaoRecuperacao({ children }: CartaoRecuperacaoProps) {
  return (
    <div className="mx-auto w-full max-w-md px-4 py-10 sm:px-6">
      <section className="rounded-cartao bg-branco p-6 shadow-cartao sm:p-8">{children}</section>
    </div>
  );
}
