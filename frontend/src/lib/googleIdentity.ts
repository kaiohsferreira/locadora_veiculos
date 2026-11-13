/**
 * Carregador do script oficial do Google (Google Identity Services) — N3, RF-51 e A5.
 *
 * O script só é inserido no modo real, sob demanda. A URL é constante: nunca é montada a partir
 * de dado da API. A parte que decide (criar uma tag só, reaproveitar a que já existe, resolver,
 * rejeitar, tentar de novo depois de uma falha) fica em `criarCarregadorScriptGoogle`, que recebe
 * o documento por parâmetro e pode ser testada sem DOM.
 */

/** Único recurso externo novo do front (G12). Constante de propósito. */
export const URL_SCRIPT_GOOGLE = 'https://accounts.google.com/gsi/client';

/** Tag `<script>` do carregamento. Só o necessário para a máquina de estados. */
export interface ElementoScriptGoogle {
  /** Ouve o carregamento ('load') ou a falha ('error'). */
  adicionarOuvinte(evento: 'load' | 'error', ouvinte: () => void): void;
  /** Insere a tag no documento. Não é chamada para uma tag que já estava no documento. */
  inserir(): void;
  /** Remove a tag (usado depois de uma falha, para a próxima tentativa começar limpa). */
  remover(): void;
}

/** Documento usado pelo carregador. No navegador é o `document`; nos testes, um fake. */
export interface DocumentoScriptGoogle {
  /** Tag do script que já está no documento, ou null. Evita uma segunda tag para a mesma URL. */
  encontrarScript(url: string): ElementoScriptGoogle | null;
  /** Cria a tag com `src` = url e `async`. Não insere: quem decide é o carregador. */
  criarScript(url: string): ElementoScriptGoogle;
  /** Verdadeiro quando `window.google.accounts.id` existe (o script de fato carregou e iniciou). */
  googleDisponivel(): boolean;
}

/** Erro interno do carregamento (não é mostrado; a tela mostra a mensagem de GOOGLE_FALHA_SCRIPT). */
export class ErroCarregarScriptGoogle extends Error {
  constructor() {
    super('Falha ao carregar o script do Google.');
    this.name = 'ErroCarregarScriptGoogle';
  }
}

/**
 * Cria a função que carrega o script.
 * - Chamadas repetidas enquanto carrega devolvem a mesma promessa.
 * - Depois de carregar (com `window.google` disponível), resolvem sem criar outra tag.
 * - Se o `load` chegar sem `window.google`, rejeita, remove a tag e não marca como carregado: a
 *   próxima chamada tenta de novo com uma tag nova.
 * - Se já existe uma tag desse script no documento, reaproveita essa tag em vez de criar outra.
 *   Limite: se a tag existente já tiver terminado antes deste carregador ouvi-la, o evento não se
 *   repete e a promessa fica pendente. Na prática o carregador é único por página.
 */
export function criarCarregadorScriptGoogle(documento: DocumentoScriptGoogle): () => Promise<void> {
  let carregado = false;
  let carregamento: Promise<void> | null = null;

  return function carregarScriptGoogle(): Promise<void> {
    if (carregado) return Promise.resolve();
    if (carregamento !== null) return carregamento;

    carregamento = new Promise<void>((resolver, rejeitar) => {
      if (documento.googleDisponivel()) {
        carregado = true;
        resolver();
        return;
      }

      const existente = documento.encontrarScript(URL_SCRIPT_GOOGLE);
      const elemento = existente ?? documento.criarScript(URL_SCRIPT_GOOGLE);

      const falhar = (): void => {
        carregamento = null;
        elemento.remover();
        rejeitar(new ErroCarregarScriptGoogle());
      };

      elemento.adicionarOuvinte('load', () => {
        // Carregou o arquivo, mas sem o objeto global: não serve. Conta como falha.
        if (!documento.googleDisponivel()) {
          falhar();
          return;
        }
        carregado = true;
        resolver();
      });
      elemento.adicionarOuvinte('error', falhar);

      if (existente === null) elemento.inserir();
    });
    return carregamento;
  };
}

/** Envolve um `<script>` do navegador na interface do carregador. */
function envolverScript(script: HTMLScriptElement): ElementoScriptGoogle {
  return {
    adicionarOuvinte: (evento, ouvinte) => script.addEventListener(evento, ouvinte),
    inserir: () => document.head.appendChild(script),
    remover: () => script.remove(),
  };
}

/** Documento real do navegador. O `document` só é tocado quando a função é chamada. */
const documentoDoNavegador: DocumentoScriptGoogle = {
  encontrarScript(url: string): ElementoScriptGoogle | null {
    // A URL é constante do módulo (não vem da API), então entra no seletor sem escape.
    const existente = document.querySelector<HTMLScriptElement>(`script[src="${url}"]`);
    return existente === null ? null : envolverScript(existente);
  },
  criarScript(url: string): ElementoScriptGoogle {
    const script = document.createElement('script');
    script.src = url;
    script.async = true;
    return envolverScript(script);
  },
  googleDisponivel(): boolean {
    return window.google?.accounts?.id !== undefined;
  },
};

/** Carrega o script oficial do Google, para toda a aplicação. */
export const carregarScriptGoogle: () => Promise<void> = criarCarregadorScriptGoogle(documentoDoNavegador);
