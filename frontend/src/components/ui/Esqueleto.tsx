export interface EsqueletoProps {
  /** Tamanho e forma via classes do Tailwind (ex.: "h-4 w-32", "h-24 w-full"). */
  className?: string;
}

/** Bloco de carregamento (skeleton). Use no lugar de spinner solto ao carregar dados. */
export function Esqueleto({ className = 'h-4 w-full' }: EsqueletoProps) {
  return <div className={`rounded-md bg-borda motion-safe:animate-pulse ${className}`} aria-hidden="true" />;
}
