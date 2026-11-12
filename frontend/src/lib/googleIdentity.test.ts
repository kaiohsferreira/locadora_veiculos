import { describe, expect, it } from 'vitest';
import {
  criarCarregadorScriptGoogle,
  URL_SCRIPT_GOOGLE,
  type DocumentoScriptGoogle,
  type ElementoScriptGoogle,
} from './googleIdentity';

/** Elemento falso: guarda os ouvintes e deixa o teste disparar 'load' ou 'error'. */
class ElementoFalso implements ElementoScriptGoogle {
  readonly ouvintes: Record<'load' | 'error', (() => void)[]> = { load: [], error: [] };
  inserido = false;
  removido = false;

  adicionarOuvinte(evento: 'load' | 'error', ouvinte: () => void): void {
    this.ouvintes[evento].push(ouvinte);
  }

  inserir(): void {
    this.inserido = true;
  }

  remover(): void {
    this.removido = true;
  }

  disparar(evento: 'load' | 'error'): void {
    for (const ouvinte of this.ouvintes[evento]) ouvinte();
  }
}

interface OpcoesDocumento {
  /** Tag que já está no documento antes do carregador (ou null). */
  tagExistente?: ElementoFalso | null;
  /** Se o `load` de uma tag criada pelo carregador deixa `window.google` disponível. Padrão: sim. */
  googleAoCarregar?: boolean;
}

/** Documento falso: registra as buscas, as URLs criadas e o estado de `window.google`. */
function criarDocumentoFalso(opcoes: OpcoesDocumento = {}) {
  const buscas: string[] = [];
  const urls: string[] = [];
  const elementos: ElementoFalso[] = [];
  const estado = {
    google: false,
    googleAoCarregar: opcoes.googleAoCarregar ?? true,
  };
  const tagExistente = opcoes.tagExistente ?? null;

  const documento: DocumentoScriptGoogle = {
    encontrarScript(url: string): ElementoScriptGoogle | null {
      buscas.push(url);
      // Uma tag removida some do documento, como no navegador.
      return tagExistente !== null && !tagExistente.removido ? tagExistente : null;
    },
    criarScript(url: string): ElementoScriptGoogle {
      urls.push(url);
      const elemento = new ElementoFalso();
      elementos.push(elemento);
      // Simula o script real: ao carregar, cria (ou não) o objeto global.
      elemento.adicionarOuvinte('load', () => {
        if (estado.googleAoCarregar) estado.google = true;
      });
      return elemento;
    },
    googleDisponivel(): boolean {
      return estado.google;
    },
  };
  return { documento, buscas, urls, elementos, estado };
}

describe('criarCarregadorScriptGoogle', () => {
  it('usa sempre a URL oficial fixa do script', () => {
    expect(URL_SCRIPT_GOOGLE).toBe('https://accounts.google.com/gsi/client');
    const { documento, urls } = criarDocumentoFalso();
    void criarCarregadorScriptGoogle(documento)();
    expect(urls).toEqual(['https://accounts.google.com/gsi/client']);
  });

  it('cria e insere uma única tag em chamadas repetidas enquanto carrega', async () => {
    const { documento, elementos } = criarDocumentoFalso();
    const carregar = criarCarregadorScriptGoogle(documento);

    const primeira = carregar();
    const segunda = carregar();
    expect(elementos).toHaveLength(1);
    expect(elementos[0].inserido).toBe(true);

    elementos[0].disparar('load');
    await Promise.all([primeira, segunda]);
  });

  it('resolve quando o script carrega com window.google', async () => {
    const { documento, elementos } = criarDocumentoFalso();
    const promessa = criarCarregadorScriptGoogle(documento)();
    elementos[0].disparar('load');
    await expect(promessa).resolves.toBeUndefined();
  });

  it('não cria outra tag depois que o script já carregou', async () => {
    const { documento, elementos } = criarDocumentoFalso();
    const carregar = criarCarregadorScriptGoogle(documento);
    const promessa = carregar();
    elementos[0].disparar('load');
    await promessa;

    await expect(carregar()).resolves.toBeUndefined();
    expect(elementos).toHaveLength(1);
  });

  it('rejeita quando o script falha e remove a tag', async () => {
    const { documento, elementos } = criarDocumentoFalso();
    const promessa = criarCarregadorScriptGoogle(documento)();
    elementos[0].disparar('error');
    await expect(promessa).rejects.toThrow('Falha ao carregar o script do Google.');
    expect(elementos[0].removido).toBe(true);
  });

  it('depois de uma falha, a próxima chamada tenta de novo com uma tag nova', async () => {
    const { documento, elementos } = criarDocumentoFalso();
    const carregar = criarCarregadorScriptGoogle(documento);

    const falha = carregar();
    elementos[0].disparar('error');
    await expect(falha).rejects.toThrow();

    const nova = carregar();
    expect(elementos).toHaveLength(2);
    elementos[1].disparar('load');
    await expect(nova).resolves.toBeUndefined();
  });

  it('load sem window.google rejeita, remove a tag e a próxima chamada tenta de novo', async () => {
    const { documento, elementos, estado } = criarDocumentoFalso({ googleAoCarregar: false });
    const carregar = criarCarregadorScriptGoogle(documento);

    const primeira = carregar();
    elementos[0].disparar('load');
    await expect(primeira).rejects.toThrow('Falha ao carregar o script do Google.');
    expect(elementos[0].removido).toBe(true);

    // Na próxima tentativa o script traz o objeto global: a tag nova é criada e a promessa resolve.
    estado.googleAoCarregar = true;
    const nova = carregar();
    expect(elementos).toHaveLength(2);
    elementos[1].disparar('load');
    await expect(nova).resolves.toBeUndefined();
  });

  it('não cria tag quando window.google já existe', async () => {
    const { documento, elementos, estado } = criarDocumentoFalso();
    estado.google = true;
    await expect(criarCarregadorScriptGoogle(documento)()).resolves.toBeUndefined();
    expect(elementos).toHaveLength(0);
  });

  it('tag já presente no documento não é duplicada: busca a existente e espera o load dela', async () => {
    const existente = new ElementoFalso();
    const { documento, buscas, elementos, estado } = criarDocumentoFalso({ tagExistente: existente });
    const promessa = criarCarregadorScriptGoogle(documento)();

    expect(buscas).toEqual([URL_SCRIPT_GOOGLE]);
    expect(elementos).toHaveLength(0);
    expect(existente.inserido).toBe(false);

    estado.google = true;
    existente.disparar('load');
    await expect(promessa).resolves.toBeUndefined();
    expect(elementos).toHaveLength(0);
  });

  it('tag já presente que falha é removida e a próxima chamada cria uma tag nova', async () => {
    const existente = new ElementoFalso();
    const { documento, elementos } = criarDocumentoFalso({ tagExistente: existente });
    const carregar = criarCarregadorScriptGoogle(documento);

    const falha = carregar();
    existente.disparar('error');
    await expect(falha).rejects.toThrow();
    expect(existente.removido).toBe(true);

    // Sem a tag existente, a próxima chamada cria a sua própria tag.
    const nova = carregar();
    expect(elementos).toHaveLength(1);
    elementos[0].disparar('load');
    await expect(nova).resolves.toBeUndefined();
  });
});
