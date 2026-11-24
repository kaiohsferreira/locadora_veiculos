import { afterEach, describe, expect, it, vi } from 'vitest';
import { ErroApi } from '@/types/api';
import { CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import { contextoRetiradaComCaucaoTeste as comCaucao, contextoRetiradaTeste as semCaucao } from './contextoRetirada.fixture';
import {
  BANDEIRA_SIMULACAO,
  CARGOS_INICIAM_COM_CAUCAO,
  cargoIniciaComCaucao,
  caucaoLiberaAbertura,
  caucaoPodeSerTentada,
  dadosDaCaucaoParaAbrir,
  estadoCaucaoNaTela,
  gerarAleatorioHex,
  memoriaComEscolha,
  memoriaComTentativa,
  memoriaDaRetirada,
  mostrarBlocoCaucao,
  OPCOES_SIMULACAO_CAUCAO,
  repeteToken,
  SEM_CAUCAO_NA_TELA,
  situacaoDaPreAutorizacao,
  statusDoErro,
  tokenDaTentativa,
  type EscolhaSimulacaoCaucao,
  type MemoriaCaucaoRetirada,
  type TentativaCaucao,
} from './regrasCaucaoRetirada';

/** Cargos que NÃO iniciam a locação com a caução exigida (ajuste A1). */
const CARGOS_SEM_PERMISSAO: readonly CargoFuncionario[] = ['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO'];

/** Formato que o servidor aceita no token (caucao-retirada.contract.json, entrada_do_cartao). */
const FORMATO_TOKEN = /^[A-Za-z0-9_-]{8,128}$/;
/** Regra do servidor "parece número de cartão": sem '-', '_', '.', ':' e espaço, sobram só 12 a 19 dígitos. */
function pareceNumeroDeCartao(texto: string): boolean {
  return /^\d{12,19}$/.test(texto.replace(/[-_.: ]/g, ''));
}

/** Gerador injetado: devolve um valor novo a cada chamada, para provar que o token foi gerado de novo. */
function geradorSequencial(): () => string {
  let contador = 0;
  return () => {
    contador += 1;
    return `aleatorio${contador}`;
  };
}

function anterior(escolha: EscolhaSimulacaoCaucao, token: string, status: number): TentativaCaucao {
  return { escolha, token, status };
}

describe('caução pode ser tentada pela tela', () => {
  it('só com a caução exigida, disponível e em modo de simulação', () => {
    expect(caucaoPodeSerTentada(comCaucao)).toBe(true);
    expect(caucaoPodeSerTentada(semCaucao)).toBe(false);
    expect(caucaoPodeSerTentada({ ...comCaucao, caucaoDisponivel: false })).toBe(false);
    expect(caucaoPodeSerTentada({ ...comCaucao, caucaoModoSimulacao: false })).toBe(false);
    expect(caucaoPodeSerTentada({ ...comCaucao, caucaoExigida: false })).toBe(false);
  });
});

describe('bloco Garantia / Caução', () => {
  it('aparece com a caução exigida e sem locação aberta, disponível ou não', () => {
    expect(mostrarBlocoCaucao(comCaucao)).toBe(true);
    expect(mostrarBlocoCaucao({ ...comCaucao, caucaoDisponivel: false, caucaoModoSimulacao: false })).toBe(true);
    expect(mostrarBlocoCaucao({ ...comCaucao, statusReserva: 'CANCELADA' })).toBe(true);
  });
  it('não aparece sem caução nem com a locação já aberta', () => {
    expect(mostrarBlocoCaucao(semCaucao)).toBe(false);
    expect(mostrarBlocoCaucao({ ...comCaucao, idLocacaoExistente: 42 })).toBe(false);
  });
});

describe('caução no botão principal', () => {
  it('sem caução, nunca segura a abertura', () => {
    expect(caucaoLiberaAbertura(SEM_CAUCAO_NA_TELA)).toBe(true);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(semCaucao, null, 'ATENDENTE'))).toBe(true);
  });
  it('com caução, libera só com uma saída escolhida e a caução que pode ser tentada', () => {
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(comCaucao, 'APROVAR', 'ATENDENTE'))).toBe(true);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(comCaucao, 'RECUSAR', 'ATENDENTE'))).toBe(true);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(comCaucao, 'ANALISE', 'ATENDENTE'))).toBe(true);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(comCaucao, null, 'ATENDENTE'))).toBe(false);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela({ ...comCaucao, caucaoDisponivel: false }, 'APROVAR', 'ATENDENTE'))).toBe(false);
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela({ ...comCaucao, caucaoModoSimulacao: false }, 'APROVAR', 'ATENDENTE'))).toBe(false);
  });
});

describe('cargo que inicia a locação com a caução exigida (ajuste A1: só ATENDENTE e ADMINISTRADOR)', () => {
  it('a lista é exatamente ATENDENTE e ADMINISTRADOR', () => {
    expect(CARGOS_INICIAM_COM_CAUCAO).toEqual(['ATENDENTE', 'ADMINISTRADOR']);
  });
  it.each(['ATENDENTE', 'ADMINISTRADOR'] as const)('libera %s', (cargo) => {
    expect(cargoIniciaComCaucao(cargo)).toBe(true);
  });
  it.each(CARGOS_SEM_PERMISSAO)('nega %s', (cargo) => {
    expect(cargoIniciaComCaucao(cargo)).toBe(false);
  });
  it('nega sessão sem cargo (cliente, sessão sem cargo ou sem sessão) e cargo que a lista não conhece', () => {
    expect(cargoIniciaComCaucao(null)).toBe(false);
    expect(cargoIniciaComCaucao('GERENTE' as unknown as CargoFuncionario)).toBe(false);
  });
  it('o estado da caução na tela guarda se o cargo inicia', () => {
    expect(estadoCaucaoNaTela(comCaucao, 'APROVAR', 'ADMINISTRADOR').cargoPodeIniciar).toBe(true);
    expect(estadoCaucaoNaTela(comCaucao, 'APROVAR', 'VENDEDOR').cargoPodeIniciar).toBe(false);
    expect(estadoCaucaoNaTela(comCaucao, 'APROVAR', null).cargoPodeIniciar).toBe(false);
  });
  it.each([...CARGOS_SEM_PERMISSAO, null])('com a caução exigida e o cargo %s, não libera nem com a saída "Aprovar"', (cargo) => {
    for (const escolha of ['APROVAR', 'RECUSAR', 'ANALISE'] as const) {
      expect(caucaoLiberaAbertura(estadoCaucaoNaTela(comCaucao, escolha, cargo))).toBe(false);
    }
  });
  it.each([...CARGO_FUNCIONARIO, null])('sem caução, libera para o cargo %s, como antes (RF-17)', (cargo) => {
    expect(caucaoLiberaAbertura(estadoCaucaoNaTela(semCaucao, null, cargo))).toBe(true);
  });
});

describe('o que o item da pré-autorização mostra (ajustes A1 e A4)', () => {
  const indisponivel = { ...comCaucao, caucaoDisponivel: false, caucaoModoSimulacao: false };
  it('INDISPONIVEL vence: a caução que não pode ser tentada, com qualquer cargo', () => {
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(indisponivel, null, 'VENDEDOR'))).toBe('INDISPONIVEL');
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(indisponivel, 'APROVAR', 'ATENDENTE'))).toBe('INDISPONIVEL');
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(indisponivel, null, null))).toBe('INDISPONIVEL');
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela({ ...comCaucao, caucaoModoSimulacao: false }, null, 'ADMINISTRADOR'))).toBe('INDISPONIVEL');
  });
  it.each([...CARGOS_SEM_PERMISSAO, null])('SEM_PERMISSAO: a caução pode ser tentada e o cargo %s não inicia', (cargo) => {
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(comCaucao, null, cargo))).toBe('SEM_PERMISSAO');
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(comCaucao, 'APROVAR', cargo))).toBe('SEM_PERMISSAO');
  });
  it.each(['ATENDENTE', 'ADMINISTRADOR'] as const)('PAINEL: a caução pode ser tentada e o cargo %s inicia', (cargo) => {
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(comCaucao, null, cargo))).toBe('PAINEL');
    expect(situacaoDaPreAutorizacao(estadoCaucaoNaTela(comCaucao, 'RECUSAR', cargo))).toBe('PAINEL');
  });
});

describe('memória da tentativa e da escolha (fica na página, zera quando a reserva da rota muda)', () => {
  const tentativa: TentativaCaucao = { escolha: 'APROVAR', token: 'FAKE-0123456789abcdef0123456789abcdef', status: 0 };
  const daReserva8: MemoriaCaucaoRetirada = { idReserva: 8, escolha: 'APROVAR', tentativaAnterior: tentativa };
  const ZERADA_8: MemoriaCaucaoRetirada = { idReserva: 8, escolha: null, tentativaAnterior: null };

  it('mantém a memória da mesma reserva (a escolha e a tentativa), sem trocar o objeto', () => {
    expect(memoriaDaRetirada(daReserva8, 8)).toBe(daReserva8);
    expect(memoriaDaRetirada(daReserva8, 8)).toEqual({ idReserva: 8, escolha: 'APROVAR', tentativaAnterior: tentativa });
  });
  it('zera com outra reserva', () => {
    expect(memoriaDaRetirada(daReserva8, 9)).toEqual({ idReserva: 9, escolha: null, tentativaAnterior: null });
  });
  it('zera com a reserva nula (id inválido na rota)', () => {
    expect(memoriaDaRetirada(daReserva8, null)).toEqual({ idReserva: null, escolha: null, tentativaAnterior: null });
  });
  it('zera sem memória', () => {
    expect(memoriaDaRetirada(null, 8)).toEqual(ZERADA_8);
    expect(memoriaDaRetirada(null, null)).toEqual({ idReserva: null, escolha: null, tentativaAnterior: null });
  });
  it('grava a escolha e a tentativa só na memória da mesma reserva', () => {
    expect(memoriaComEscolha(ZERADA_8, 8, 'RECUSAR')).toEqual({ idReserva: 8, escolha: 'RECUSAR', tentativaAnterior: null });
    expect(memoriaComTentativa(ZERADA_8, 8, tentativa)).toEqual({ idReserva: 8, escolha: null, tentativaAnterior: tentativa });
    expect(memoriaComTentativa(daReserva8, 8, { ...tentativa, status: 409 }).escolha).toBe('APROVAR');
  });
  it('a resposta que chega depois de a rota trocar de reserva não mexe na memória da reserva nova', () => {
    expect(memoriaComTentativa(daReserva8, 7, { ...tentativa, token: 'FAKE-RECUSADO', escolha: 'RECUSAR' })).toBe(daReserva8);
    expect(memoriaComEscolha(daReserva8, 7, 'ANALISE')).toBe(daReserva8);
  });
  it('com a memória que sobreviveu, a tentativa seguinte repete o MESMO token (sem resposta) e a escolha continua', () => {
    const lembrada = memoriaDaRetirada(daReserva8, 8);
    expect(lembrada.escolha).toBe('APROVAR');
    expect(tokenDaTentativa('APROVAR', lembrada.tentativaAnterior, () => 'novo')).toBe(tentativa.token);
    const zerada = memoriaDaRetirada(daReserva8, 9);
    expect(tokenDaTentativa('APROVAR', zerada.tentativaAnterior, () => 'novo')).toBe('FAKE-novo');
  });
});

describe('opções do painel de simulação', () => {
  it('três saídas, na ordem aprovar, recusar e deixar em análise', () => {
    expect(OPCOES_SIMULACAO_CAUCAO.map((opcao) => opcao.valor)).toEqual(['APROVAR', 'RECUSAR', 'ANALISE']);
    expect(OPCOES_SIMULACAO_CAUCAO.map((opcao) => opcao.rotulo)).toEqual(['Aprovar', 'Recusar', 'Deixar em análise']);
  });
});

describe('token da tentativa', () => {
  afterEach(() => vi.restoreAllMocks());

  it('"Aprovar" gera "FAKE-" + o valor do gerador injetado, no formato que o servidor aceita', () => {
    const token = tokenDaTentativa('APROVAR', null, () => '0123456789abcdef0123456789abcdef');
    expect(token).toBe('FAKE-0123456789abcdef0123456789abcdef');
    expect(token).toMatch(FORMATO_TOKEN);
  });
  it('"Recusar" manda FAKE-RECUSADO e "Deixar em análise" manda FAKE-ANALISE', () => {
    expect(tokenDaTentativa('RECUSAR', null, geradorSequencial())).toBe('FAKE-RECUSADO');
    expect(tokenDaTentativa('ANALISE', null, geradorSequencial())).toBe('FAKE-ANALISE');
    expect('FAKE-RECUSADO').toMatch(FORMATO_TOKEN);
    expect('FAKE-ANALISE').toMatch(FORMATO_TOKEN);
  });
  it.each([0, 408, 502, 503, 504])('repete o MESMO token depois do status %s com a mesma saída', (status) => {
    const gerar = geradorSequencial();
    const primeiro = tokenDaTentativa('APROVAR', null, gerar);
    expect(tokenDaTentativa('APROVAR', anterior('APROVAR', primeiro, status), gerar)).toBe(primeiro);
    expect(tokenDaTentativa('RECUSAR', anterior('RECUSAR', 'FAKE-RECUSADO', status), gerar)).toBe('FAKE-RECUSADO');
  });
  it.each([400, 401, 403, 404, 409, 429, 500])('gera token NOVO depois do status %s (resposta definitiva)', (status) => {
    const gerar = geradorSequencial();
    const primeiro = tokenDaTentativa('APROVAR', null, gerar);
    const segundo = tokenDaTentativa('APROVAR', anterior('APROVAR', primeiro, status), gerar);
    expect(segundo).not.toBe(primeiro);
    expect(segundo).toBe('FAKE-aleatorio2');
  });
  it.each([0, 408, 502, 503, 504])('com outra saída, o token é o da saída nova mesmo depois do status %s', (status) => {
    const gerar = geradorSequencial();
    expect(tokenDaTentativa('RECUSAR', anterior('APROVAR', 'FAKE-aleatorio1', status), gerar)).toBe('FAKE-RECUSADO');
    expect(tokenDaTentativa('ANALISE', anterior('RECUSAR', 'FAKE-RECUSADO', status), gerar)).toBe('FAKE-ANALISE');
    const aprovado = tokenDaTentativa('APROVAR', anterior('ANALISE', 'FAKE-ANALISE', status), gerar);
    expect(aprovado).toBe('FAKE-aleatorio1');
  });
  it('nenhum caminho devolve token fixo para "Aprovar": cada tentativa nova chama o gerador', () => {
    const gerar = vi.fn(geradorSequencial());
    const tokens = [
      tokenDaTentativa('APROVAR', null, gerar),
      tokenDaTentativa('APROVAR', anterior('APROVAR', 'FAKE-x', 409), gerar),
      tokenDaTentativa('APROVAR', anterior('RECUSAR', 'FAKE-RECUSADO', 0), gerar),
      tokenDaTentativa('APROVAR', anterior('ANALISE', 'FAKE-ANALISE', 409), gerar),
    ];
    expect(gerar).toHaveBeenCalledTimes(4);
    expect(new Set(tokens).size).toBe(4);
    for (const token of tokens) {
      expect(token.startsWith('FAKE-')).toBe(true);
      expect(['FAKE-RECUSADO', 'FAKE-ANALISE']).not.toContain(token);
    }
  });
  it('sem gerador injetado, "Aprovar" usa o gerador padrão: FAKE- + 32 hexadecimais, que não parece número de cartão', () => {
    const token = tokenDaTentativa('APROVAR', null);
    expect(token).toMatch(/^FAKE-[0-9a-f]{32}$/);
    expect(token).toHaveLength(37);
    expect(token).toMatch(FORMATO_TOKEN);
    expect(pareceNumeroDeCartao(token)).toBe(false);
  });
  it('o gerador padrão devolve 32 caracteres [0-9a-f], e duas chamadas seguidas dão valores diferentes', () => {
    const primeiro = gerarAleatorioHex();
    const segundo = gerarAleatorioHex();
    expect(primeiro).toMatch(/^[0-9a-f]{32}$/);
    expect(segundo).toMatch(/^[0-9a-f]{32}$/);
    expect(primeiro).not.toBe(segundo);
  });
  it('o gerador padrão não depende de crypto.randomUUID (que só existe em contexto seguro)', () => {
    const randomUUID = vi.spyOn(globalThis.crypto, 'randomUUID').mockImplementation(() => {
      throw new Error('crypto.randomUUID indisponível fora de contexto seguro');
    });
    expect(gerarAleatorioHex()).toMatch(/^[0-9a-f]{32}$/);
    expect(tokenDaTentativa('APROVAR', null)).toMatch(/^FAKE-[0-9a-f]{32}$/);
    expect(randomUUID).not.toHaveBeenCalled();
  });
  it('o gerador padrão usa crypto.getRandomValues com 16 bytes', () => {
    const getRandomValues = vi.spyOn(globalThis.crypto, 'getRandomValues');
    gerarAleatorioHex();
    expect(getRandomValues).toHaveBeenCalledTimes(1);
    expect((getRandomValues.mock.calls[0][0] as Uint8Array).length).toBe(16);
  });
});

describe('repete o token', () => {
  it.each([0, 408, 502, 503, 504])('sim, depois do status %s (sem resposta definitiva)', (status) => {
    expect(repeteToken(status)).toBe(true);
  });
  it.each([200, 201, 400, 401, 403, 404, 409, 422, 429, 500, 501])('não, depois do status %s', (status) => {
    expect(repeteToken(status)).toBe(false);
  });
});

describe('dados da caução para abrir', () => {
  it('manda o token, a bandeira "visa" e emissor e dispositivo nulos (nada de valor, e-mail nem CPF)', () => {
    expect(BANDEIRA_SIMULACAO).toBe('visa');
    expect(BANDEIRA_SIMULACAO).toMatch(/^[a-z0-9_]{2,30}$/);
    expect(dadosDaCaucaoParaAbrir('FAKE-RECUSADO')).toEqual({
      caucaoTokenCartao: 'FAKE-RECUSADO',
      caucaoMetodoPagamentoId: 'visa',
      caucaoEmissorId: null,
      caucaoIdentificadorDispositivo: null,
    });
  });
});

describe('status do erro da abertura', () => {
  it('usa o status do ErroApi; erro que não veio da API conta como sem resposta (0)', () => {
    expect(statusDoErro(new ErroApi({ status: 409, mensagem: 'Conflito.' }))).toBe(409);
    expect(statusDoErro(new ErroApi({ status: 0, mensagem: 'Sem conexão.' }))).toBe(0);
    expect(statusDoErro(new Error('qualquer'))).toBe(0);
    expect(statusDoErro(undefined)).toBe(0);
  });
});
