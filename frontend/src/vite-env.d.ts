/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** URL base da API do backend (ex.: http://localhost:5000). */
  readonly VITE_API_URL: string;
  /**
   * "true" libera as rotas protegidas sem passar pelo login (P2-BE-01).
   * Só tem efeito no modo de desenvolvimento (npm run dev).
   */
  readonly VITE_IGNORAR_AUTENTICACAO?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
