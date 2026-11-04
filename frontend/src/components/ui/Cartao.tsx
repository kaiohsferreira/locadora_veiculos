import type { ReactNode } from 'react';

export interface CartaoProps {
  /** Título da seção do cartão. */
  titulo?: string;
  /** Texto de apoio abaixo do título. */
  descricao?: string;
  /** Ações no canto do cabeçalho (botões, links). */
  acoes?: ReactNode;
  children: ReactNode;
  /** Remove o espaçamento interno do corpo (ex.: cartão que contém uma Tabela). */
  semEspacamento?: boolean;
  className?: string;
}

/** Cartão branco do design system: canto de 8px e sombra discreta. */
export function Cartao({ titulo, descricao, acoes, children, semEspacamento = false, className = '' }: CartaoProps) {
  const temCabecalho = Boolean(titulo || descricao || acoes);

  return (
    <section className={`rounded-cartao bg-branco shadow-cartao ${className}`}>
      {temCabecalho && (
        <header className="flex flex-wrap items-start justify-between gap-4 border-b border-borda px-6 py-4">
          <div>
            {titulo && <h2 className="text-secao text-navy">{titulo}</h2>}
            {descricao && <p className="mt-1 text-corpo-denso text-texto-suave">{descricao}</p>}
          </div>
          {acoes && <div className="flex items-center gap-2">{acoes}</div>}
        </header>
      )}
      <div className={semEspacamento ? '' : 'p-6'}>{children}</div>
    </section>
  );
}
