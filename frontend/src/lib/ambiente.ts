/** Leitura centralizada das variáveis de ambiente (arquivo .env). */

/** URL base da API do backend. */
export const URL_API: string = import.meta.env.VITE_API_URL ?? '';

/**
 * Desvio da autenticação para navegar no desenvolvimento enquanto o login
 * (P2-BE-01) não existe. Só vale no `npm run dev`: em build de produção a
 * proteção de rotas sempre é aplicada, mesmo que a variável esteja "true".
 */
export const IGNORAR_AUTENTICACAO: boolean =
  import.meta.env.DEV && import.meta.env.VITE_IGNORAR_AUTENTICACAO === 'true';
