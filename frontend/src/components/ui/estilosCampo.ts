/** Classes compartilhadas pelos controles de formulário (Campo e Select). */

export const CLASSES_ROTULO = 'text-corpo-denso font-medium text-texto';

export const CLASSES_DICA = 'text-legenda text-texto-suave';

export const CLASSES_ERRO = 'flex items-center gap-1 text-legenda font-medium text-status-perigo';

const CLASSES_CONTROLE_BASE =
  'h-10 w-full rounded-md border bg-branco px-3 text-corpo-denso text-texto transition-colors ' +
  'placeholder:text-texto-suave focus:outline-none focus:ring-2 ' +
  'disabled:cursor-not-allowed disabled:bg-offwhite disabled:text-texto-suave';

/** Classes do input/select conforme o estado de erro. */
export function classesControle(temErro: boolean): string {
  return temErro
    ? `${CLASSES_CONTROLE_BASE} border-status-perigo focus:border-status-perigo focus:ring-status-perigo/20`
    : `${CLASSES_CONTROLE_BASE} border-borda-forte focus:border-steel focus:ring-steel/30`;
}

/** Monta o aria-describedby a partir dos ids de dica e erro presentes. */
export function idsDescricao(...ids: (string | false | undefined)[]): string | undefined {
  const presentes = ids.filter((id): id is string => typeof id === 'string' && id.length > 0);
  return presentes.length > 0 ? presentes.join(' ') : undefined;
}
