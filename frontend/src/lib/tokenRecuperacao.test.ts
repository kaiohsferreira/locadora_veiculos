import { afterEach, describe, expect, it, vi } from 'vitest';
import { lerTokenDoFragmento, removerFragmentoDaBarra } from './tokenRecuperacao';

describe('lerTokenDoFragmento', () => {
  it('lê o token de "#token=abc"', () => {
    expect(lerTokenDoFragmento('#token=abc')).toBe('abc');
  });

  it('ignora outros parâmetros antes do token', () => {
    expect(lerTokenDoFragmento('#outro=1&token=abc')).toBe('abc');
  });

  it('ignora outros parâmetros depois do token', () => {
    expect(lerTokenDoFragmento('#token=abc&outro=1')).toBe('abc');
  });

  it('usa a primeira ocorrência quando o parâmetro aparece duas vezes', () => {
    expect(lerTokenDoFragmento('#token=primeiro&token=segundo')).toBe('primeiro');
  });

  it('rejeita token vazio', () => {
    expect(lerTokenDoFragmento('#token=')).toBeNull();
  });

  it('rejeita fragmento sem o parâmetro token', () => {
    expect(lerTokenDoFragmento('#outro=1')).toBeNull();
    expect(lerTokenDoFragmento('#')).toBeNull();
    expect(lerTokenDoFragmento('')).toBeNull();
  });

  it('rejeita parâmetro sem "=" (ex.: "#token")', () => {
    expect(lerTokenDoFragmento('#token')).toBeNull();
  });

  it('exige o "#" no início (sem "#" não há fragmento)', () => {
    expect(lerTokenDoFragmento('token=abc')).toBeNull();
  });

  it('nunca lê o token da query string ("?token=abc" devolve null)', () => {
    // A query vai ao servidor, ao histórico e ao Referer: o token só é aceito no fragmento.
    expect(lerTokenDoFragmento('?token=abc')).toBeNull();
    expect(lerTokenDoFragmento('?outro=1&token=abc')).toBeNull();
  });

  it('decodifica percent-encoding (%2B vira "+")', () => {
    expect(lerTokenDoFragmento('#token=ab%2Bcd')).toBe('ab+cd');
  });

  it('mantém o "+" literal (não converte em espaço)', () => {
    expect(lerTokenDoFragmento('#token=ab+cd')).toBe('ab+cd');
  });

  it('aceita exatamente 128 caracteres', () => {
    const token = 'a'.repeat(128);
    expect(lerTokenDoFragmento(`#token=${token}`)).toBe(token);
  });

  it('rejeita 129 caracteres', () => {
    expect(lerTokenDoFragmento(`#token=${'a'.repeat(129)}`)).toBeNull();
  });

  it('conta o tamanho depois de decodificar', () => {
    // 387 caracteres brutos, mas 129 depois de decodificar "%41" (A).
    expect(lerTokenDoFragmento(`#token=${'%41'.repeat(129)}`)).toBeNull();
    expect(lerTokenDoFragmento(`#token=${'%41'.repeat(128)}`)).toBe('A'.repeat(128));
  });

  it('não lança com percent-encoding malformado', () => {
    expect(() => lerTokenDoFragmento('#token=%E0%A4%A')).not.toThrow();
    expect(lerTokenDoFragmento('#token=%E0%A4%A')).toBeNull();
  });
});

/** Barra de endereços falsa: o teste simula `window` (o ambiente de teste é node, sem DOM). */
function simularJanela(hash: string, pathname: string, search: string, state: unknown) {
  const replaceState = vi.fn((_estado: unknown, _titulo: string, url: string) => {
    // O navegador troca a URL: o fragmento some e caminho e query ficam.
    const [caminho, consulta = ''] = url.split('?');
    janela.location.pathname = caminho;
    janela.location.search = consulta === '' ? '' : `?${consulta}`;
    janela.location.hash = '';
  });
  const janela = {
    location: { hash, pathname, search, origin: 'http://localhost:5173' },
    history: { state, replaceState },
  };
  vi.stubGlobal('window', janela);
  return { replaceState };
}

describe('removerFragmentoDaBarra', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('tira o fragmento e mantém caminho, query e history.state', () => {
    const estado = { usr: 'x', key: 'abc' };
    const { replaceState } = simularJanela('#token=abc', '/redefinir-senha', '?origem=email', estado);

    removerFragmentoDaBarra();

    expect(replaceState).toHaveBeenCalledTimes(1);
    expect(replaceState).toHaveBeenCalledWith(estado, '', '/redefinir-senha?origem=email');
  });

  it('sem query, troca só o fragmento pelo caminho', () => {
    const { replaceState } = simularJanela('#token=abc', '/redefinir-senha', '', null);

    removerFragmentoDaBarra();

    expect(replaceState).toHaveBeenCalledWith(null, '', '/redefinir-senha');
  });

  it('sem fragmento não faz nada', () => {
    const { replaceState } = simularJanela('', '/redefinir-senha', '?x=1', null);

    removerFragmentoDaBarra();

    expect(replaceState).not.toHaveBeenCalled();
  });

  it('chamar de novo é seguro: depois de tirado, não há o que trocar', () => {
    const { replaceState } = simularJanela('#token=abc', '/redefinir-senha', '', null);

    removerFragmentoDaBarra();
    removerFragmentoDaBarra();

    expect(replaceState).toHaveBeenCalledTimes(1);
  });
});

describe('leitura do token pela página', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('endereço com ?token=abc e sem fragmento não traz token', () => {
    // A página (RedefinirSenhaPagina) chama lerTokenDoFragmento(window.location.hash). Aqui a janela
    // simulada tem a query "?token=abc" e hash vazio, que é exatamente o que o navegador entregaria.
    simularJanela('', '/redefinir-senha', '?token=abc', null);

    expect(lerTokenDoFragmento(window.location.hash)).toBeNull();
  });
});
