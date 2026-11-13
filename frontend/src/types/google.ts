/**
 * Declaração mínima do script oficial de login do Google (Google Identity Services, carregado
 * de https://accounts.google.com/gsi/client). Só o que a AutoVia usa. Os tipos da API da
 * AutoVia (login, vínculo, configuração) ficam em `@/types/autenticacao`.
 */

/** Resposta do botão do Google: a credencial é um JWT assinado pelo Google (string). */
export interface RespostaCredencialGoogle {
  credential: string;
}

/** Callback chamado pelo Google quando o usuário conclui o login. Cancelar o popup não chama nada. */
export type CallbackCredencialGoogle = (resposta: RespostaCredencialGoogle) => void;

/** Opções de `google.accounts.id.initialize`. O `client_id` vem da API e só entra aqui. */
export interface OpcoesInicializacaoGoogle {
  client_id: string;
  callback: CallbackCredencialGoogle;
}

/** Opções de `google.accounts.id.renderButton` (botão padrão, em pt-BR). */
export interface OpcoesBotaoGoogle {
  type: 'standard';
  theme: 'outline';
  size: 'large';
  text: 'continue_with';
  locale: 'pt-BR';
}

/** Parte `google.accounts.id` usada pela AutoVia. */
export interface ApiIdGoogle {
  initialize(opcoes: OpcoesInicializacaoGoogle): void;
  renderButton(elemento: HTMLElement, opcoes: OpcoesBotaoGoogle): void;
  cancel(): void;
}

/** Parte `google.accounts` do objeto global. */
export interface ApiContasGoogle {
  id: ApiIdGoogle;
}

/** Objeto global `window.google`, criado pelo script oficial depois que ele carrega. */
export interface ApiGoogle {
  accounts: ApiContasGoogle;
}

declare global {
  interface Window {
    google?: ApiGoogle;
  }
}
