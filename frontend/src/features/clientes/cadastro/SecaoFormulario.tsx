import type { ReactNode } from 'react';

export interface SecaoFormularioProps {
  /** Título da seção, ligado aos campos pelo <legend> (leitores de tela leem junto). */
  titulo: string;
  /** Texto de apoio abaixo do título. */
  descricao?: string;
  children: ReactNode;
}

/**
 * Seção do formulário: <fieldset> com <legend>, e os campos em grade de duas colunas
 * a partir do breakpoint médio (uma coluna no celular). Para um campo ocupar as duas
 * colunas, envolva-o em um <div className="md:col-span-2">.
 */
export function SecaoFormulario({ titulo, descricao, children }: SecaoFormularioProps) {
  return (
    <fieldset className="min-w-0 border-0 p-0">
      <legend className="text-secao text-navy">{titulo}</legend>
      {descricao && <p className="mt-1 text-corpo-denso text-texto-suave">{descricao}</p>}
      <div className="mt-4 grid grid-cols-1 gap-x-6 gap-y-5 md:grid-cols-2">{children}</div>
    </fieldset>
  );
}
