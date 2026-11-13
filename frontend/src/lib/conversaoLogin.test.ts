import { describe, expect, it } from 'vitest';
import { converterRespostaLogin, lerVencimentoDoToken, perfilUsuarioDaApi } from '@/lib/conversaoLogin';
import type { RespostaLogin } from '@/types/autenticacao';

const CABECALHO = '{"alg":"HS256","typ":"JWT"}';

/** base64url sem padding, como o JWT. Só ASCII nos payloads de teste. */
function base64Url(texto: string): string {
  return btoa(texto).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/** Token montado com o payload informado (a assinatura é um texto qualquer: o front não a verifica). */
function tokenCom(payload: string): string {
  return `${base64Url(CABECALHO)}.${base64Url(payload)}.assinatura-de-teste`;
}

/** Instante fixo (UTC) usado nos testes: 12/11/2026 13:00 UTC. */
const EXP_TESTE = Date.UTC(2026, 10, 12, 13, 0, 0) / 1000;
const ISO_TESTE = '2026-11-12T13:00:00.000Z';

const RESPOSTA_BASE: RespostaLogin = {
  token: 'token-de-teste',
  tipoToken: 'Bearer',
  expiraEm: '2026-11-12T12:00:00',
  expiraEmStr: '12/11/2026 12:00',
  perfil: 'CLIENTE',
  idUsuario: 7,
  nome: 'Ana Souza',
};

describe('perfilUsuarioDaApi', () => {
  it('converte CLIENTE e FUNCIONARIO para cliente e funcionario', () => {
    expect(perfilUsuarioDaApi('CLIENTE')).toBe('cliente');
    expect(perfilUsuarioDaApi('FUNCIONARIO')).toBe('funcionario');
  });
});

describe('lerVencimentoDoToken', () => {
  it('devolve o exp do token como ISO UTC com Z', () => {
    expect(lerVencimentoDoToken(tokenCom(`{"exp":${EXP_TESTE},"nome":"Ana"}`))).toBe(ISO_TESTE);
  });

  it('devolve null quando o payload não tem exp', () => {
    expect(lerVencimentoDoToken(tokenCom('{"nome":"Ana"}'))).toBeNull();
  });

  it('devolve null quando exp não é número finito', () => {
    expect(lerVencimentoDoToken(tokenCom('{"exp":"2026-11-12"}'))).toBeNull();
    expect(lerVencimentoDoToken(tokenCom('{"exp":null}'))).toBeNull();
  });

  it('devolve null para token malformado (partes diferentes de três)', () => {
    expect(lerVencimentoDoToken('abc')).toBeNull();
    expect(lerVencimentoDoToken('a.b')).toBeNull();
    expect(lerVencimentoDoToken('a.b.c.d')).toBeNull();
    expect(lerVencimentoDoToken('')).toBeNull();
  });

  it('devolve null quando o payload não é JSON', () => {
    expect(lerVencimentoDoToken(tokenCom('texto-sem-json'))).toBeNull();
  });

  it('devolve null quando o payload é JSON mas não é um objeto', () => {
    expect(lerVencimentoDoToken(tokenCom('123'))).toBeNull();
    expect(lerVencimentoDoToken(tokenCom('null'))).toBeNull();
  });

  it('decodifica base64url com "-" e "_" no payload', () => {
    const comMenos = `{"exp":${EXP_TESTE},"nome":"Ana ??>"}`;
    const comUnderline = `{"exp":${EXP_TESTE},"nome":"Ana ?>?"}`;

    expect(tokenCom(comMenos).split('.')[1]).toMatch(/-/);
    expect(tokenCom(comUnderline).split('.')[1]).toMatch(/_/);
    expect(lerVencimentoDoToken(tokenCom(comMenos))).toBe(ISO_TESTE);
    expect(lerVencimentoDoToken(tokenCom(comUnderline))).toBe(ISO_TESTE);
  });

  it('decodifica payload sem padding (base64url do JWT não tem "=")', () => {
    const token = tokenCom(`{"exp":${EXP_TESTE},"nome":"Ana Souza"}`);

    expect(token.split('.')[1]).not.toContain('=');
    expect(lerVencimentoDoToken(token)).toBe(ISO_TESTE);
  });
});

describe('converterRespostaLogin', () => {
  it('converte o perfil CLIENTE para cliente e copia nome, id e validade', () => {
    const usuario = converterRespostaLogin(RESPOSTA_BASE);

    expect(usuario).toEqual({
      nome: 'Ana Souza',
      perfil: 'cliente',
      idUsuario: 7,
      expiraEm: '2026-11-12T12:00:00',
    });
  });

  it('usa o exp do token (UTC) como validade quando o token é legível', () => {
    const usuario = converterRespostaLogin({ ...RESPOSTA_BASE, token: tokenCom(`{"exp":${EXP_TESTE}}`) });

    expect(usuario.expiraEm).toBe(ISO_TESTE);
  });

  it('converte o perfil FUNCIONARIO e leva o cargo', () => {
    const usuario = converterRespostaLogin({
      ...RESPOSTA_BASE,
      perfil: 'FUNCIONARIO',
      cargo: 'GESTOR_FROTA',
    });

    expect(usuario.perfil).toBe('funcionario');
    expect(usuario.cargo).toBe('GESTOR_FROTA');
    expect(usuario).not.toHaveProperty('statusCadastro');
  });

  it('leva o status do cadastro do cliente quando vier preenchido', () => {
    const usuario = converterRespostaLogin({ ...RESPOSTA_BASE, statusCadastro: 'PENDENTE' });

    expect(usuario.statusCadastro).toBe('PENDENTE');
    expect(usuario).not.toHaveProperty('cargo');
  });

  it('omite cargo e statusCadastro quando vêm null ou ausentes', () => {
    const comNull = converterRespostaLogin({
      ...RESPOSTA_BASE,
      cargo: null,
      statusCadastro: null,
    });
    const semCampos = converterRespostaLogin(RESPOSTA_BASE);

    for (const usuario of [comNull, semCampos]) {
      expect(usuario).not.toHaveProperty('cargo');
      expect(usuario).not.toHaveProperty('statusCadastro');
    }
  });
});
