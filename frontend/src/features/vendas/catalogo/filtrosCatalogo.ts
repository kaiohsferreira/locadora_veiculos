import { converterMoedaPtBrParaNumero, formatarMoeda, formatarQuilometragem, limitarDigitos, mascararMoedaDigitada } from '@/lib/formatters';
import {
  CATALOGO_ERRO_ANO_INVALIDO,
  CATALOGO_ERRO_FAIXA_ANO,
  CATALOGO_ERRO_FAIXA_PRECO,
  CATALOGO_ERRO_KM_INVALIDO,
  CATALOGO_ERRO_MARCA_TAMANHO,
  CATALOGO_ERRO_PRECO_INVALIDO,
  CATALOGO_ETIQUETA_ANO_ATE,
  CATALOGO_ETIQUETA_ANO_DE,
  CATALOGO_ETIQUETA_ATE,
  CATALOGO_ETIQUETA_DE,
  CATALOGO_ETIQUETA_KM,
  CATALOGO_ETIQUETA_MARCA,
} from '@/lib/mensagensCatalogo';
import type { FiltroCatalogo } from '@/types/vendas';

/** 20 veículos por página, como pedido na P6-FE-01 (o padrão do servidor). */
export const TAMANHO_PAGINA_CATALOGO = 20;

/** Mesmo limite da API para a busca por marca. */
export const TAMANHO_MAXIMO_MARCA = 40;

const DIGITOS_ANO = 4;
const DIGITOS_KM = 7;

/** Filtros que a pessoa pode aplicar (sem paginação). */
export type ChaveFiltroCatalogo = 'marca' | 'precoMinimo' | 'precoMaximo' | 'anoMinimo' | 'anoMaximo' | 'kmMaximo';

export const CHAVES_FILTRO: readonly ChaveFiltroCatalogo[] = [
  'marca',
  'precoMinimo',
  'precoMaximo',
  'anoMinimo',
  'anoMaximo',
  'kmMaximo',
];

/** Texto digitado em cada campo do cartão de filtros (preços com a máscara de reais). */
export type CamposFiltro = Record<ChaveFiltroCatalogo, string>;

/** Erro de cada campo; ausente quando o campo está certo. */
export type ErrosFiltro = Partial<Record<ChaveFiltroCatalogo, string>>;

/** Uma etiqueta de filtro ativo, removível. */
export interface EtiquetaFiltro {
  chave: ChaveFiltroCatalogo;
  rotulo: string;
}

export const CAMPOS_FILTRO_VAZIOS: CamposFiltro = {
  marca: '',
  precoMinimo: '',
  precoMaximo: '',
  anoMinimo: '',
  anoMaximo: '',
  kmMaximo: '',
};

/** Filtro inicial: primeira página, sem filtros. */
export function filtroInicial(): FiltroCatalogo {
  return { pagina: 1, tamanhoPagina: TAMANHO_PAGINA_CATALOGO };
}

/** Campo de ano: só dígitos, no máximo 4. */
export function mascararAno(valor: string): string {
  return limitarDigitos(valor, DIGITOS_ANO);
}

/** Campo de km: só dígitos, no máximo 7. */
export function mascararKm(valor: string): string {
  return limitarDigitos(valor, DIGITOS_KM);
}

/** Número inteiro de um campo só de dígitos; vazio ou inválido vira nulo. */
function inteiro(texto: string): number | null {
  const limpo = texto.trim();
  return /^\d+$/.test(limpo) ? Number(limpo) : null;
}

/**
 * Regras dos campos, as mesmas da API (para a pessoa ver o erro no campo, sem esperar um 400). Campo vazio é "sem
 * filtro" e nunca é erro.
 */
export function validarCamposFiltro(campos: CamposFiltro): ErrosFiltro {
  const erros: ErrosFiltro = {};

  if (campos.marca.trim().length > TAMANHO_MAXIMO_MARCA) erros.marca = CATALOGO_ERRO_MARCA_TAMANHO;

  const precoMinimo = converterMoedaPtBrParaNumero(campos.precoMinimo);
  const precoMaximo = converterMoedaPtBrParaNumero(campos.precoMaximo);
  if (campos.precoMinimo.trim() && precoMinimo === null) erros.precoMinimo = CATALOGO_ERRO_PRECO_INVALIDO;
  if (campos.precoMaximo.trim() && precoMaximo === null) erros.precoMaximo = CATALOGO_ERRO_PRECO_INVALIDO;
  if (precoMinimo !== null && precoMaximo !== null && precoMinimo > precoMaximo) erros.precoMaximo = CATALOGO_ERRO_FAIXA_PRECO;

  const anoMinimo = inteiro(campos.anoMinimo);
  const anoMaximo = inteiro(campos.anoMaximo);
  const anoValido = (ano: number | null): boolean => ano !== null && String(ano).length === DIGITOS_ANO;
  if (campos.anoMinimo.trim() && !anoValido(anoMinimo)) erros.anoMinimo = CATALOGO_ERRO_ANO_INVALIDO;
  if (campos.anoMaximo.trim() && !anoValido(anoMaximo)) erros.anoMaximo = CATALOGO_ERRO_ANO_INVALIDO;
  if (!erros.anoMinimo && !erros.anoMaximo && anoMinimo !== null && anoMaximo !== null && anoMinimo > anoMaximo) {
    erros.anoMaximo = CATALOGO_ERRO_FAIXA_ANO;
  }

  if (campos.kmMaximo.trim() && inteiro(campos.kmMaximo) === null) erros.kmMaximo = CATALOGO_ERRO_KM_INVALIDO;

  return erros;
}

/** Os campos não têm erro. */
export function filtrosValidos(erros: ErrosFiltro): boolean {
  return CHAVES_FILTRO.every((chave) => erros[chave] === undefined);
}

/** Campos válidos viram o filtro da API, sempre na primeira página. Campo vazio fica fora do filtro. */
export function camposParaFiltro(campos: CamposFiltro): FiltroCatalogo {
  const filtro = filtroInicial();
  const marca = campos.marca.trim();
  if (marca) filtro.marca = marca;
  const precoMinimo = converterMoedaPtBrParaNumero(campos.precoMinimo);
  const precoMaximo = converterMoedaPtBrParaNumero(campos.precoMaximo);
  if (precoMinimo !== null) filtro.precoMinimo = precoMinimo;
  if (precoMaximo !== null) filtro.precoMaximo = precoMaximo;
  const anoMinimo = inteiro(campos.anoMinimo);
  const anoMaximo = inteiro(campos.anoMaximo);
  if (anoMinimo !== null) filtro.anoMinimo = anoMinimo;
  if (anoMaximo !== null) filtro.anoMaximo = anoMaximo;
  const kmMaximo = inteiro(campos.kmMaximo);
  if (kmMaximo !== null) filtro.kmMaximo = kmMaximo;
  return filtro;
}

/** Valor em reais para o texto mascarado do campo ("100000" → "100.000,00"). */
function precoParaCampo(valor: number | undefined): string {
  return valor === undefined ? '' : mascararMoedaDigitada(String(Math.round(valor * 100)));
}

/** O filtro aplicado volta para os campos (para abrir o cartão ou a janela com o que está valendo). */
export function filtroParaCampos(filtro: FiltroCatalogo): CamposFiltro {
  return {
    marca: filtro.marca ?? '',
    precoMinimo: precoParaCampo(filtro.precoMinimo),
    precoMaximo: precoParaCampo(filtro.precoMaximo),
    anoMinimo: filtro.anoMinimo === undefined ? '' : String(filtro.anoMinimo),
    anoMaximo: filtro.anoMaximo === undefined ? '' : String(filtro.anoMaximo),
    kmMaximo: filtro.kmMaximo === undefined ? '' : String(filtro.kmMaximo),
  };
}

/** Etiquetas dos filtros ativos, na ordem dos campos. */
export function etiquetasFiltro(filtro: FiltroCatalogo): EtiquetaFiltro[] {
  const etiquetas: EtiquetaFiltro[] = [];
  if (filtro.marca) etiquetas.push({ chave: 'marca', rotulo: `${CATALOGO_ETIQUETA_MARCA}: ${filtro.marca}` });
  if (filtro.precoMinimo !== undefined) {
    etiquetas.push({ chave: 'precoMinimo', rotulo: `${CATALOGO_ETIQUETA_DE} ${formatarMoeda(filtro.precoMinimo)}` });
  }
  if (filtro.precoMaximo !== undefined) {
    etiquetas.push({ chave: 'precoMaximo', rotulo: `${CATALOGO_ETIQUETA_ATE} ${formatarMoeda(filtro.precoMaximo)}` });
  }
  if (filtro.anoMinimo !== undefined) etiquetas.push({ chave: 'anoMinimo', rotulo: `${CATALOGO_ETIQUETA_ANO_DE} ${filtro.anoMinimo}` });
  if (filtro.anoMaximo !== undefined) etiquetas.push({ chave: 'anoMaximo', rotulo: `${CATALOGO_ETIQUETA_ANO_ATE} ${filtro.anoMaximo}` });
  if (filtro.kmMaximo !== undefined) {
    etiquetas.push({ chave: 'kmMaximo', rotulo: `${CATALOGO_ETIQUETA_KM} ${formatarQuilometragem(filtro.kmMaximo)}` });
  }
  return etiquetas;
}

/** Tira um filtro e volta para a primeira página. */
export function removerFiltro(filtro: FiltroCatalogo, chave: ChaveFiltroCatalogo): FiltroCatalogo {
  const novo: FiltroCatalogo = { ...filtro, pagina: 1 };
  delete novo[chave];
  return novo;
}

/** Quantos filtros estão ativos (para o botão "Filtros (N)" do celular). */
export function contarFiltrosAtivos(filtro: FiltroCatalogo): number {
  return etiquetasFiltro(filtro).length;
}
