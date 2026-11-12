/**
 * Token do link de redefinição de senha (N1, RF-49).
 *
 * O e-mail traz `https://<front>/redefinir-senha#token=<valor>`. O valor fica no FRAGMENTO (depois do #),
 * que não vai ao servidor nem ao cabeçalho Referer. Por isso o token nunca vai em query string.
 *
 * As funções de leitura são puras (testadas). `removerFragmentoDaBarra` é o único ponto que toca a
 * barra de endereços: tira o fragmento e mantém caminho e query.
 */

/** Nome do parâmetro no fragmento. Igual ao ParametroTokenFront do backend. */
export const PARAMETRO_TOKEN = 'token';

/** Maior tamanho aceito para o token (igual ao RedefinirSenhaVO no backend). */
export const TAMANHO_MAXIMO_TOKEN = 128;

/**
 * Lê o token do fragmento da URL (ex.: "#token=abc" ou "#outro=1&token=abc").
 * Devolve null quando não há token válido: sem "#", sem o parâmetro, vazio, com mais de
 * 128 caracteres ou com percent-encoding malformado. Nunca lança exceção.
 * Se o parâmetro aparecer mais de uma vez, vale a primeira ocorrência.
 */
export function lerTokenDoFragmento(hash: string): string | null {
  if (!hash.startsWith('#')) return null;

  for (const parametro of hash.slice(1).split('&')) {
    const separador = parametro.indexOf('=');
    if (separador < 0) continue;
    if (parametro.slice(0, separador) !== PARAMETRO_TOKEN) continue;
    return decodificarToken(parametro.slice(separador + 1));
  }

  return null;
}

/** Decodifica o valor (percent-encoding) e valida tamanho. Devolve null se for inválido. */
function decodificarToken(valorBruto: string): string | null {
  let valor: string;
  try {
    valor = decodeURIComponent(valorBruto);
  } catch {
    // Sequência percent malformada (ex.: "%E0%A4%A"): não é um token válido.
    return null;
  }
  if (valor.length === 0 || valor.length > TAMANHO_MAXIMO_TOKEN) return null;
  return valor;
}

/**
 * Tira o fragmento da barra de endereços (history.replaceState), mantendo caminho e query e o
 * state do roteador. Não faz nada se não houver fragmento, então é seguro chamar mais de uma vez
 * (o StrictMode repete os efeitos na montagem).
 */
export function removerFragmentoDaBarra(): void {
  if (typeof window === 'undefined' || window.location.hash === '') return;
  const { pathname, search } = window.location;
  window.history.replaceState(window.history.state, '', `${pathname}${search}`);
}
