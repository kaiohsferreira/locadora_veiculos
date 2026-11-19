/** Leitura centralizada das variáveis de ambiente (arquivo .env). */

/**
 * Normaliza a URL base da API: tira espaços das pontas e as barras do fim, para que
 * `VITE_API_URL=https://api.exemplo.com/` não gere `https://api.exemplo.com//api/...`.
 * Sem valor (variável ausente) devolve string vazia, como antes.
 */
export function normalizarUrlApi(valor: string | undefined): string {
  return (valor ?? '').trim().replace(/\/+$/, '');
}

/** URL base da API do backend. */
export const URL_API: string = normalizarUrlApi(import.meta.env.VITE_API_URL);

/**
 * Desvio da autenticação para navegar no desenvolvimento sem passar pelo login
 * (P2-BE-01), útil quando a API não está rodando. Só vale no `npm run dev`: em
 * build de produção a proteção de rotas sempre é aplicada, mesmo que a variável
 * esteja "true".
 */
export const IGNORAR_AUTENTICACAO: boolean =
  import.meta.env.DEV && import.meta.env.VITE_IGNORAR_AUTENTICACAO === 'true';
