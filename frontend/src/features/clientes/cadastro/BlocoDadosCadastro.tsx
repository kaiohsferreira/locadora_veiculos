import { useId, type ReactNode } from 'react';
import { Icone } from '@/components/ui';

export interface BlocoDadosCadastroProps {
  /** Prefixo dos ids do título e do texto (o texto pode descrever campos: `${id}-texto`). Padrão: gerado. */
  id?: string;
  /** Ícone Material Symbols do cabeçalho. */
  icone: string;
  titulo: string;
  descricao: string;
  /** Colunas dos cartões a partir do breakpoint médio (uma coluna no celular). */
  colunas: 2 | 3;
  children: ReactNode;
}

const CLASSES_COLUNAS: Readonly<Record<2 | 3, string>> = {
  2: 'md:grid-cols-2',
  3: 'md:grid-cols-3',
};

/**
 * Bloco da etapa de documentos (estilo do bloco "Dados extraídos" do design 21): fundo azulado,
 * cabeçalho branco com ícone, título e texto, e os cartões-campo em grade abaixo. É uma região
 * nomeada pelo título (h2).
 */
export function BlocoDadosCadastro({ id, icone, titulo, descricao, colunas, children }: BlocoDadosCadastroProps) {
  const idGerado = useId();
  const base = id ?? `bloco-${idGerado}`;
  const idTitulo = `${base}-titulo`;

  return (
    <section aria-labelledby={idTitulo} className="mb-6 w-full rounded-lg bg-steel-fundo p-6">
      <div className="mb-6 flex items-start gap-2 rounded bg-branco p-4 shadow-sm">
        <Icone nome={icone} tamanho={24} className="text-status-pendente" />
        <div className="flex flex-col">
          <h2 id={idTitulo} className="text-corpo text-navy">
            {titulo}
          </h2>
          <p id={`${base}-texto`} className="mt-0.5 text-corpo text-status-neutro">
            {descricao}
          </p>
        </div>
      </div>
      <div className={`grid grid-cols-1 gap-6 ${CLASSES_COLUNAS[colunas]}`}>{children}</div>
    </section>
  );
}
