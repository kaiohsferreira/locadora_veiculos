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
 * (campo branco sem borda aparente, sombra leve, 44px de altura e ícone inicial opcional); `desenho` é o campo do
 * back office do cadastro de veículo (tela 05): fundo azul claro sem borda, 40px de altura e cantos de 4px.
 */
export type VarianteCampo = 'padrao' | 'portal' | 'cadastro' | 'cartao' | 'desenho';

/**
 * Fonte do texto digitado na variante `desenho`: `corpo` (14/20), `dado` (número e código, 13/18), `placa` (16/22
 * em caixa alta, negrito) e `valor` (valor em reais, 18/24 negrito).
 */
export type TipografiaCampo = 'corpo' | 'dado' | 'placa' | 'valor';

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
    case 'desenho':
      return 'text-rotulo text-texto-desenho';
    default:
      return CLASSES_ROTULO;
  }
}

/** Texto de apoio à direita do rótulo (variante cadastro e demais): mesma linha, alinhado ao fim. */
export const CLASSES_APOIO_ROTULO = 'text-corpo text-status-neutro';

/** Apoio à direita do rótulo conforme a variante (a `desenho` usa micro texto em laranja, como o desenho 05). */
export function classesApoioRotulo(variante: VarianteCampo): string {
  return variante === 'desenho' ? 'text-micro font-normal text-status-pendente' : CLASSES_APOIO_ROTULO;
}

/** Texto de ajuda conforme a variante (padrão: legenda pequena). */
export function classesDica(variante: VarianteCampo): string {
  switch (variante) {
    case 'cadastro':
      return 'flex items-center gap-1 pt-0.5 text-corpo text-status-neutro';
    case 'cartao':
      return 'mt-1 text-corpo text-status-neutro';
    case 'desenho':
      return 'flex items-center gap-1 text-apoio text-texto-apoio';
    default:
      return CLASSES_DICA;
  }
}

/** Fonte do texto digitado na variante `desenho`. */
export function classesTipografiaDesenho(tipografia: TipografiaCampo): string {
  switch (tipografia) {
    case 'dado':
      return 'text-dado-tabular';
    case 'placa':
      return 'text-titulo-md font-bold uppercase tracking-widest';
    case 'valor':
      return 'text-nome-veiculo';
    default:
      return 'text-corpo-denso';
  }
}

/** Prefixo ("R$" ou emblema) dentro do campo `desenho`: posicionado à esquerda, sem receber cliques. */
export const CLASSES_PREFIXO_DESENHO =
  'pointer-events-none absolute left-3 flex items-center text-corpo-denso font-bold text-texto-apoio';

/** Sufixo ("KM" ou ícone) dentro do campo `desenho`: à direita, sem receber cliques. */
export const CLASSES_SUFIXO_DESENHO =
  'pointer-events-none absolute right-3 flex items-center text-rotulo font-bold text-texto-apoio';

/**
 * Controle `desenho` do campo de texto (tela 05): fundo azul claro, sem borda, 40px, canto de 4px e foco com fundo
 * branco e anel laranja. Os recuos à esquerda e à direita são escolhidos aqui, para o prefixo e o sufixo não
 * sobreporem o texto. Não tem estilo de desabilitado: o desenho não mostra um.
 */
export function classesControleDesenho(
  temErro: boolean,
  opcoes: { recuoEsquerdo: 'nenhum' | 'prefixo' | 'prefixoLargo'; sufixo: boolean; tipografia: TipografiaCampo },
): string {
  const recuoEsquerdo =
    opcoes.recuoEsquerdo === 'prefixoLargo' ? 'pl-14' : opcoes.recuoEsquerdo === 'prefixo' ? 'pl-11' : 'pl-4';
  const recuoDireito = opcoes.sufixo ? 'pr-12' : 'pr-4';
  const base =
    `h-10 w-full rounded bg-steel-fundo text-texto-desenho transition-all focus:bg-branco focus:outline-none focus:ring-2 focus:ring-orange-acao ` +
    `${recuoEsquerdo} ${recuoDireito} ${classesTipografiaDesenho(opcoes.tipografia)}`;
  return temErro ? `${base} ring-2 ring-status-perigo focus:ring-status-perigo` : base;
}

/** Controle `desenho` da lista de seleção (tela 05): mesmo visual do campo, com espaço para a seta. */
export function classesSelectDesenho(temErro: boolean, destaque: boolean): string {
  const base =
    'h-10 w-full appearance-none rounded bg-steel-fundo px-4 pr-10 text-corpo-denso transition-all focus:bg-branco focus:outline-none focus:ring-2 focus:ring-orange-acao ' +
    (destaque ? 'font-semibold text-status-pendente' : 'text-texto-desenho');
  return temErro ? `${base} ring-2 ring-status-perigo focus:ring-status-perigo` : base;
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
