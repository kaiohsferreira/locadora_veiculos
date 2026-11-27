import { generatePath } from 'react-router-dom';
import { CAMINHOS } from '@/routes/caminhos';
import type { FiltroCatalogo } from '@/types/vendas';
import { CHAVES_FILTRO, TAMANHO_MAXIMO_MARCA, filtroInicial, type ChaveFiltroCatalogo } from './filtrosCatalogo';

/**
 * Filtros e página do catálogo no endereço (`/seminovos?marca=jeep&precoMaximo=100000&pagina=2`). Assim, voltar da
 * proposta pelo navegador mantém a busca e o link pode ser compartilhado. Valor inválido no endereço é ignorado, sem
 * quebrar a tela; o tamanho da página não vai para o endereço (é sempre 20).
 */
const PARAMETRO_PAGINA = 'pagina';

/** Número do endereço: só aceita o formato e o limite esperados. */
function numeroDoEndereco(texto: string | null, inteiro: boolean): number | undefined {
  if (texto === null) return undefined;
  const formato = inteiro ? /^\d{1,9}$/ : /^\d{1,10}(\.\d{1,2})?$/;
  if (!formato.test(texto)) return undefined;
  return Number(texto);
}

/** Lê o filtro do endereço. Página inválida vira 1; filtro inválido fica de fora. */
export function lerFiltroDoEndereco(parametros: URLSearchParams): FiltroCatalogo {
  const filtro = filtroInicial();
  const pagina = numeroDoEndereco(parametros.get(PARAMETRO_PAGINA), true);
  if (pagina !== undefined && pagina >= 1) filtro.pagina = pagina;

  const marca = parametros.get('marca')?.trim();
  if (marca && marca.length <= TAMANHO_MAXIMO_MARCA) filtro.marca = marca;

  const numericos: readonly [Exclude<ChaveFiltroCatalogo, 'marca'>, boolean][] = [
    ['precoMinimo', false],
    ['precoMaximo', false],
    ['anoMinimo', true],
    ['anoMaximo', true],
    ['kmMaximo', true],
  ];
  for (const [chave, inteiro] of numericos) {
    const valor = numeroDoEndereco(parametros.get(chave), inteiro);
    if (valor !== undefined) filtro[chave] = valor;
  }

  if (filtro.precoMinimo !== undefined && filtro.precoMaximo !== undefined && filtro.precoMinimo > filtro.precoMaximo) {
    delete filtro.precoMaximo;
  }
  if (filtro.anoMinimo !== undefined && filtro.anoMaximo !== undefined && filtro.anoMinimo > filtro.anoMaximo) {
    delete filtro.anoMaximo;
  }
  return filtro;
}

/** Escreve o filtro no endereço: só os filtros ativos e a página quando não é a primeira. */
export function escreverFiltroNoEndereco(filtro: FiltroCatalogo): URLSearchParams {
  const parametros = new URLSearchParams();
  for (const chave of CHAVES_FILTRO) {
    const valor = filtro[chave];
    if (valor !== undefined && valor !== '') parametros.set(chave, String(valor));
  }
  if (filtro.pagina > 1) parametros.set(PARAMETRO_PAGINA, String(filtro.pagina));
  return parametros;
}

/**
 * Página fora do intervalo (ex.: `?pagina=99` com 2 páginas, ou depois que carros saíram do catálogo): devolve a última
 * página que existe, para a tela ir para ela. Devolve nulo quando a página está certa ou quando o catálogo está vazio.
 */
export function paginaCorrigida(pagina: number, total: number, tamanhoPagina: number): number | null {
  if (total <= 0) return null;
  const ultimaPagina = Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
  return pagina > ultimaPagina ? ultimaPagina : null;
}

/** Caminho da proposta de compra de um veículo (destino de cada cartão do catálogo). */
export function caminhoProposta(idVeiculo: number): string {
  return generatePath(CAMINHOS.propostaCompra, { veiculoId: String(idVeiculo) });
}
