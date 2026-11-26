import type { ReactNode } from 'react';
import { Icone } from '@/components/ui';

export interface CabecalhoColunaProps {
  id: string;
  icone: string;
  /** Classes do quadrado do ícone (fundo e cor), só com tokens. */
  classesIcone: string;
  titulo: string;
  /** Linha de apoio (data, hora e operador). */
  linha: string;
  /** Selo ao lado do título, quando houver. */
  selo?: ReactNode;
}

/** Cabeçalho das colunas da tela 06 (desenho: quadrado do ícone, título em negrito e linha de apoio). */
export function CabecalhoColuna({ id, icone, classesIcone, titulo, linha, selo }: CabecalhoColunaProps) {
  return (
    <header className="flex items-start justify-between gap-3 pb-2">
      <div className="flex min-w-0 items-center gap-3">
        <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded ${classesIcone}`}>
          <Icone nome={icone} tamanho={20} />
        </div>
        <div className="flex min-w-0 flex-col">
          <h2 id={id} className="text-titulo-md font-bold text-navy-profundo break-words">{titulo}</h2>
          <p className="text-micro text-texto-apoio break-words">{linha}</p>
        </div>
      </div>
      {selo}
    </header>
  );
}
