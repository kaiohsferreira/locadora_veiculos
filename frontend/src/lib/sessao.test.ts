/**
 * Testes da sessão sem DOM: o `window` é substituído por um dublê com
 * localStorage em memória, e o módulo é recarregado a cada cenário.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { UsuarioSessao } from '@/types/autenticacao';

type ModuloSessao = Awaited<ReturnType<typeof importarSessao>>;

const CHAVE_TOKEN = 'autovia:token';
const CHAVE_USUARIO = 'autovia:usuario';

// Hora local fixa: 12/11/2026 10:00.
const INSTANTE = new Date(2026, 10, 12, 10, 0, 0);

class ArmazenamentoFalso {
  private readonly itens = new Map<string, string>();

  getItem(chave: string): string | null {
    return this.itens.get(chave) ?? null;
  }

  setItem(chave: string, valor: string): void {
    this.itens.set(chave, valor);
  }

  removeItem(chave: string): void {
    this.itens.delete(chave);
  }
}

let armazenamento: ArmazenamentoFalso;
/** Listeners de `storage` registrados pelo módulo no dublê de window (o último é o do módulo carregado). */
let ouvintesStorage: ((evento: StorageEvent) => void)[] = [];

function importarSessao() {
  return import('@/lib/sessao');
}

/** Recarrega o módulo como se a página tivesse sido aberta de novo (com o mesmo armazenamento). */
async function recarregarSessao(): Promise<ModuloSessao> {
  // clearAllTimers reinicia o relógio falso: a hora atual é preservada logo depois.
  const agora = new Date(Date.now());
  vi.clearAllTimers();
  vi.setSystemTime(agora);
  vi.resetModules();
  return importarSessao();
}

/** Grava uma sessão crua no armazenamento, como ela estaria após uma visita anterior. */
function gravarSessaoCrua(token: string, usuario: unknown): void {
  armazenamento.setItem(CHAVE_TOKEN, token);
  armazenamento.setItem(CHAVE_USUARIO, JSON.stringify(usuario));
}

let sessao: ModuloSessao;

beforeEach(async () => {
  vi.useFakeTimers();
  vi.setSystemTime(INSTANTE);
  armazenamento = new ArmazenamentoFalso();
  ouvintesStorage = [];
  vi.stubGlobal('window', {
    localStorage: armazenamento,
    addEventListener: (tipo: string, ouvinte: (evento: StorageEvent) => void) => {
      if (tipo === 'storage') ouvintesStorage.push(ouvinte);
    },
  });
  sessao = await recarregarSessao();
});

afterEach(() => {
  sessao.limparSessao();
  vi.clearAllTimers();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe('expiração inválida e armazenamento', () => {
  it('expiraEm que não é data não agenda temporizador e a sessão continua aberta', () => {
    sessao.salvarSessao('token-11', { nome: 'Lia', perfil: 'cliente', expiraEm: 'x' });

    expect(vi.getTimerCount()).toBe(0);
    vi.advanceTimersByTime(3_600_000);

    expect(sessao.obterToken()).toBe('token-11');
    expect(sessao.obterUsuarioSessao()).toEqual({ nome: 'Lia', perfil: 'cliente' });
  });

  it('salvarSessao com expiraEm já passado conta como sessão vencida e a apaga', () => {
    sessao.salvarSessao('token-12', { nome: 'Max', perfil: 'cliente', expiraEm: '2026-11-12T09:00:00' });

    expect(sessao.obterUsuarioSessao()).toBeNull();
    expect(sessao.obterToken()).toBeNull();
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBeNull();
  });

  it('armazenamento que lança erro mantém a sessão só em memória', async () => {
    const lancando = {
      getItem: (): string | null => {
        throw new Error('armazenamento bloqueado');
      },
      setItem: (): void => {
        throw new Error('armazenamento bloqueado');
      },
      removeItem: (): void => {
        throw new Error('armazenamento bloqueado');
      },
    };
    vi.stubGlobal('window', { localStorage: lancando, addEventListener: vi.fn() });
    const modulo = await recarregarSessao();

    modulo.salvarSessao('token-13', { nome: 'Nina', perfil: 'cliente' });
    expect(modulo.obterToken()).toBe('token-13');

    modulo.limparSessao();
    expect(modulo.obterToken()).toBeNull();
  });
});

describe('evento storage (outra aba)', () => {
  function dispararStorage(chave: string | null): void {
    const ouvinte = ouvintesStorage[ouvintesStorage.length - 1];
    ouvinte({ key: chave } as StorageEvent);
  }

  it('key null (limpeza total) recarrega a sessão do armazenamento e avisa os ouvintes', () => {
    sessao.salvarSessao('token-14', { nome: 'Otto', perfil: 'cliente' });
    armazenamento.removeItem(CHAVE_TOKEN);
    armazenamento.removeItem(CHAVE_USUARIO);
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    dispararStorage(null);

    expect(sessao.obterToken()).toBeNull();
    expect(ouvinte).toHaveBeenCalledTimes(1);
  });

  it('key da sessão recarrega a sessão trazida pela outra aba', () => {
    gravarSessaoCrua('token-15', { nome: 'Pia', perfil: 'funcionario' });

    dispararStorage(CHAVE_TOKEN);

    expect(sessao.obterToken()).toBe('token-15');
    expect(sessao.obterUsuarioSessao()).toEqual({ nome: 'Pia', perfil: 'funcionario' });
  });

  it('key alheia não altera a sessão nem avisa os ouvintes', () => {
    sessao.salvarSessao('token-16', { nome: 'Rafa', perfil: 'cliente' });
    gravarSessaoCrua('outro-token', { nome: 'Sol', perfil: 'funcionario' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    dispararStorage('chave-alheia');

    expect(sessao.obterToken()).toBe('token-16');
    expect(ouvinte).not.toHaveBeenCalled();
  });
});

describe('sessão sem vencimento', () => {
  it('sem sessão, obterToken e obterUsuarioSessao devolvem null', () => {
    expect(sessao.obterToken()).toBeNull();
    expect(sessao.obterUsuarioSessao()).toBeNull();
  });

  it('salvarSessao grava token e usuário e avisa os ouvintes', () => {
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);
    const usuario: UsuarioSessao = { nome: 'Ana', perfil: 'cliente' };

    sessao.salvarSessao('token-1', usuario);

    expect(sessao.obterToken()).toBe('token-1');
    expect(sessao.obterUsuarioSessao()).toEqual(usuario);
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBe('token-1');
    expect(ouvinte).toHaveBeenCalledTimes(1);
  });

  it('obterUsuarioSessao devolve sempre a mesma referência enquanto a sessão não muda', () => {
    sessao.salvarSessao('token-1', { nome: 'Ana', perfil: 'cliente' });

    const primeira = sessao.obterUsuarioSessao();
    const segunda = sessao.obterUsuarioSessao();

    expect(primeira).not.toBeNull();
    expect(segunda).toBe(primeira);
  });

  it('limparSessao apaga a sessão e avisa os ouvintes', () => {
    sessao.salvarSessao('token-1', { nome: 'Ana', perfil: 'cliente' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    sessao.limparSessao();

    expect(sessao.obterToken()).toBeNull();
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBeNull();
    expect(armazenamento.getItem(CHAVE_USUARIO)).toBeNull();
    expect(ouvinte).toHaveBeenCalledTimes(1);
  });
});

describe('campos opcionais do login', () => {
  it('guarda e relê idUsuario, expiraEm, cargo e statusCadastro', async () => {
    const usuario: UsuarioSessao = {
      nome: 'Bruno',
      perfil: 'funcionario',
      idUsuario: 7,
      expiraEm: '2026-11-12T12:00:00',
      cargo: 'GESTOR_FROTA',
    };
    sessao.salvarSessao('token-2', usuario);

    const relido = await recarregarSessao();

    expect(relido.obterToken()).toBe('token-2');
    expect(relido.obterUsuarioSessao()).toEqual(usuario);
  });

  it('descarta campo opcional com valor desconhecido sem invalidar a sessão', async () => {
    gravarSessaoCrua('token-3', {
      nome: 'Carla',
      perfil: 'funcionario',
      cargo: 'DIRETOR',
      statusCadastro: 'EM_ANALISE',
      idUsuario: 'abc',
    });

    const relido = await recarregarSessao();

    expect(relido.obterToken()).toBe('token-3');
    const usuario = relido.obterUsuarioSessao();
    expect(usuario).toEqual({ nome: 'Carla', perfil: 'funcionario' });
    expect(usuario).not.toHaveProperty('cargo');
    expect(usuario).not.toHaveProperty('statusCadastro');
    expect(usuario).not.toHaveProperty('idUsuario');
  });

  it('perfil desconhecido invalida a sessão guardada', async () => {
    gravarSessaoCrua('token-4', { nome: 'Dani', perfil: 'administrador' });

    const relido = await recarregarSessao();

    expect(relido.obterToken()).toBeNull();
    expect(relido.obterUsuarioSessao()).toBeNull();
  });
});

describe('sessão vencida', () => {
  it('sessão já vencida ao carregar conta como sem sessão e é apagada do armazenamento', async () => {
    gravarSessaoCrua('token-5', {
      nome: 'Eva',
      perfil: 'cliente',
      expiraEm: '2026-11-12T09:00:00',
    });

    const relido = await recarregarSessao();

    expect(relido.obterToken()).toBeNull();
    expect(relido.obterUsuarioSessao()).toBeNull();
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBeNull();
    expect(armazenamento.getItem(CHAVE_USUARIO)).toBeNull();
  });

  it('obterToken encerra a sessão que venceu e avisa os ouvintes', () => {
    sessao.salvarSessao('token-6', { nome: 'Fabio', perfil: 'cliente', expiraEm: '2026-11-12T10:30:00' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    vi.setSystemTime(new Date(2026, 10, 12, 10, 30, 0));

    expect(sessao.obterToken()).toBeNull();
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBeNull();
    expect(ouvinte).toHaveBeenCalledTimes(1);
  });

  it('obterUsuarioSessao devolve null após o vencimento sem limpar a sessão nem avisar ouvintes', () => {
    sessao.salvarSessao('token-7', { nome: 'Gabi', perfil: 'cliente', expiraEm: '2026-11-12T10:30:00' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    vi.setSystemTime(new Date(2026, 10, 12, 10, 31, 0));

    expect(sessao.obterUsuarioSessao()).toBeNull();
    expect(sessao.obterUsuarioSessao()).toBeNull();
    expect(ouvinte).not.toHaveBeenCalled();
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBe('token-7');
  });

  it('o temporizador encerra a sessão no instante exato do vencimento', () => {
    sessao.salvarSessao('token-8', { nome: 'Hugo', perfil: 'cliente', expiraEm: '2026-11-12T10:00:05' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    vi.advanceTimersByTime(4_999);
    expect(sessao.obterToken()).toBe('token-8');
    expect(ouvinte).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(armazenamento.getItem(CHAVE_TOKEN)).toBeNull();
    expect(ouvinte).toHaveBeenCalledTimes(1);
    expect(sessao.obterUsuarioSessao()).toBeNull();
  });

  it('vencimento além do limite do setTimeout é reagendado até acontecer', () => {
    const vencimento = new Date(INSTANTE.getTime() + 3_000_000_000);
    sessao.salvarSessao('token-9', {
      nome: 'Iago',
      perfil: 'funcionario',
      expiraEm: vencimento.toISOString(),
    });

    vi.advanceTimersByTime(2_147_483_647);
    expect(sessao.obterToken()).toBe('token-9');

    vi.advanceTimersByTime(852_516_352);
    expect(sessao.obterToken()).toBe('token-9');

    vi.advanceTimersByTime(1);
    expect(sessao.obterToken()).toBeNull();
  });

  it('limparSessao cancela o temporizador pendente', () => {
    sessao.salvarSessao('token-10', { nome: 'Joana', perfil: 'cliente', expiraEm: '2026-11-12T10:00:05' });
    const ouvinte = vi.fn();
    sessao.inscreverMudancasSessao(ouvinte);

    sessao.limparSessao();
    vi.advanceTimersByTime(10_000);

    expect(ouvinte).toHaveBeenCalledTimes(1);
  });
});
