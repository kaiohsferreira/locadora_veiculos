import type { CSSProperties } from 'react';

export interface IconeProps {
  /** Nome do ícone Material Symbols (ex.: "directions_car", "search"), o mesmo usado nos protótipos. */
  nome: string;
  /** Tamanho em pixels. Padrão: 20. */
  tamanho?: number;
  /** Versão preenchida do ícone. */
  preenchido?: boolean;
  /**
   * Texto para leitores de tela quando o ícone carrega significado sozinho.
   * Sem rótulo, o ícone é decorativo e fica oculto para tecnologias assistivas.
   */
  rotulo?: string;
  className?: string;
}

/** Ícone da fonte Material Symbols Outlined (carregada no index.html). */
export function Icone({ nome, tamanho = 20, preenchido = false, rotulo, className = '' }: IconeProps) {
  const estilo: CSSProperties = {
    fontSize: `${tamanho}px`,
    fontVariationSettings: preenchido ? "'FILL' 1" : "'FILL' 0",
  };

  return (
    <span
      className={`material-symbols-outlined inline-block shrink-0 select-none leading-none ${className}`}
      style={estilo}
      role={rotulo ? 'img' : undefined}
      aria-label={rotulo}
      aria-hidden={rotulo ? undefined : true}
    >
      {nome}
    </span>
  );
}
