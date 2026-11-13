import { beforeEach, describe, expect, it } from 'vitest';
import {
  guardarCredencialGoogle,
  limparCredencialGoogle,
  obterCredencialGoogle,
  type CredencialGuardada,
} from './credencialGoogle';

const DADOS: CredencialGuardada = {
  credencial: 'jwt-de-teste',
  lembrar: true,
  nome: 'Ana Souza',
  email: 'ana@exemplo.com',
};

describe('credencialGoogle', () => {
  beforeEach(() => {
    limparCredencialGoogle();
  });

  it('começa sem credencial', () => {
    expect(obterCredencialGoogle()).toBeNull();
  });

  it('guarda e devolve os dados', () => {
    guardarCredencialGoogle(DADOS);
    expect(obterCredencialGoogle()).toEqual(DADOS);
  });

  it('devolve uma cópia: alterar o retorno não muda a credencial guardada', () => {
    guardarCredencialGoogle(DADOS);
    const obtida = obterCredencialGoogle();
    if (obtida !== null) obtida.credencial = 'outra';
    expect(obterCredencialGoogle()?.credencial).toBe('jwt-de-teste');
  });

  it('não é afetada por mudança no objeto que foi guardado', () => {
    const dados = { ...DADOS };
    guardarCredencialGoogle(dados);
    dados.email = 'outro@exemplo.com';
    expect(obterCredencialGoogle()?.email).toBe('ana@exemplo.com');
  });

  it('substitui a credencial anterior ao guardar de novo', () => {
    guardarCredencialGoogle(DADOS);
    guardarCredencialGoogle({ ...DADOS, credencial: 'segunda', lembrar: false });
    expect(obterCredencialGoogle()).toEqual({ ...DADOS, credencial: 'segunda', lembrar: false });
  });

  it('limpa a credencial', () => {
    guardarCredencialGoogle(DADOS);
    limparCredencialGoogle();
    expect(obterCredencialGoogle()).toBeNull();
  });

  it('limpar sem credencial não dá erro', () => {
    expect(() => limparCredencialGoogle()).not.toThrow();
    expect(obterCredencialGoogle()).toBeNull();
  });

  it('aceita nome e e-mail nulos', () => {
    guardarCredencialGoogle({ ...DADOS, nome: null, email: null });
    expect(obterCredencialGoogle()).toEqual({ ...DADOS, nome: null, email: null });
  });
});
