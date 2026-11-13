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

/**
 * Variante visual do campo. `padrao` é o visual de sempre; `portal` é o estilo da tela de acesso
 * (campo branco sem borda aparente, sombra leve, 44px de altura e ícone inicial opcional).
 */
export type VarianteCampo = 'padrao' | 'portal' | 'cadastro' | 'cartao';

/** Rótulo da variante portal (14/20 semibold, com mais respiro até o campo). */
export const CLASSES_ROTULO_PORTAL = 'block text-corpo-denso font-semibold text-navy';

/** Espaço entre rótulo, controle e mensagens, por variante. */
export function classesEnvoltorioCampo(variante: VarianteCampo): string {
  switch (variante) {
    case 'portal':
    case 'cadastro':
      return 'flex flex-col gap-1.5';
    case 'cartao':
      // Cartão-campo da etapa de documentos: fundo branco, sombra leve e rótulo em maiúsculas.
      return 'flex flex-col gap-1 rounded-lg bg-branco p-4 shadow-sm';
    default:
      return 'flex flex-col gap-1';
  }
}

/** Rótulo conforme a variante. */
export function classesRotulo(variante: VarianteCampo): string {
  switch (variante) {
    case 'portal':
      return CLASSES_ROTULO_PORTAL;
    case 'cadastro':
      return 'flex items-center gap-1 text-corpo text-navy';
    case 'cartao':
      return 'text-corpo uppercase text-status-neutro';
    default:
      return CLASSES_ROTULO;
  }
}

/** Texto de apoio à direita do rótulo (variante cadastro e demais): mesma linha, alinhado ao fim. */
export const CLASSES_APOIO_ROTULO = 'text-corpo text-status-neutro';

/** Texto de ajuda conforme a variante (padrão: legenda pequena). */
export function classesDica(variante: VarianteCampo): string {
  switch (variante) {
    case 'cadastro':
      return 'flex items-center gap-1 pt-0.5 text-corpo text-status-neutro';
    case 'cartao':
      return 'mt-1 text-corpo text-status-neutro';
    default:
      return CLASSES_DICA;
  }
}

/**
 * Controle da variante cadastro (design 20): campo branco sem borda, sombra leve, 44px, canto de 2px.
 * Com ícone inicial reserva 40px à esquerda; sempre reserva 40px à direita para o ícone de conferido.
 * O foco mostra anel (o design tira o contorno; sem ele o foco ficaria invisível).
 */
export function classesControleCadastro(temErro: boolean, comIcone: boolean): string {
  const base =
    'h-11 w-full rounded-sm bg-branco text-corpo text-texto shadow-sm transition-all ' +
    'placeholder:text-texto-suave focus:outline-none focus:ring-2 ' +
    'disabled:cursor-not-allowed disabled:bg-offwhite disabled:text-texto-suave ' +
    (comIcone ? 'pl-10 pr-10' : 'pl-3 pr-10');
  return temErro ? `${base} ring-2 ring-status-perigo focus:ring-status-perigo` : `${base} focus:ring-steel`;
}

/** Controle da variante cartão (design 21): dentro do cartão branco, com borda para mostrar que é editável. */
export function classesControleCartao(temErro: boolean): string {
  const base =
    'h-11 w-full rounded-sm border bg-branco px-3 text-corpo text-texto transition-colors ' +
    'placeholder:text-texto-suave focus:outline-none focus:ring-2 ' +
    'disabled:cursor-not-allowed disabled:bg-offwhite disabled:text-texto-suave';
  return temErro
    ? `${base} border-status-perigo focus:border-status-perigo focus:ring-status-perigo/20`
    : `${base} border-borda-forte focus:border-steel focus:ring-steel/30`;
}

/** Controle da variante portal. Sem borda: o erro aparece em anel vermelho e o foco em anel de destaque. */
export function classesControlePortal(temErro: boolean, comIcone: boolean): string {
  const base =
    'h-11 w-full rounded bg-branco text-corpo-denso text-texto shadow-sm transition-all ' +
    'placeholder:text-texto-suave focus:bg-offwhite focus:outline-none focus:ring-2 ' +
    'disabled:cursor-not-allowed disabled:bg-offwhite disabled:text-texto-suave ' +
    (comIcone ? 'pl-11 pr-4' : 'px-4');
  return temErro ? `${base} ring-2 ring-status-perigo focus:ring-status-perigo` : `${base} focus:ring-steel`;
}

/** Monta o aria-describedby a partir dos ids de dica e erro presentes. */
export function idsDescricao(...ids: (string | false | undefined)[]): string | undefined {
  const presentes = ids.filter((id): id is string => typeof id === 'string' && id.length > 0);
  return presentes.length > 0 ? presentes.join(' ') : undefined;
}
