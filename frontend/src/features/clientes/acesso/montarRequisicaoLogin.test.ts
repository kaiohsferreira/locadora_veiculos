import { describe, expect, it } from 'vitest';
import { montarRequisicaoLogin } from './montarRequisicaoLogin';
import type { ValoresAcesso } from './esquemaAcesso';

const VALORES: ValoresAcesso = {
  perfil: 'CLIENTE',
  cpf: '123.456.789-09',
  senha: 'senha-de-teste',
  lembrar: false,
};

describe('montarRequisicaoLogin', () => {
  it('monta o corpo com perfil, CPF só com dígitos, senha e lembrar', () => {
    expect(montarRequisicaoLogin(VALORES)).toEqual({
      perfil: 'CLIENTE',
      cpf: '12345678909',
      senha: 'senha-de-teste',
      lembrar: false,
    });
  });

  it('leva lembrar true quando marcado', () => {
    expect(montarRequisicaoLogin({ ...VALORES, lembrar: true }).lembrar).toBe(true);
  });

  it('sempre envia lembrar como booleano, mesmo sem o campo', () => {
    const semLembrar = { perfil: 'FUNCIONARIO', cpf: '12345678909', senha: 'x' } as unknown as ValoresAcesso;
    const corpo = montarRequisicaoLogin(semLembrar);
    expect(corpo.lembrar).toBe(false);
    expect(typeof corpo.lembrar).toBe('boolean');
  });

  it('não leva outros campos do formulário', () => {
    expect(Object.keys(montarRequisicaoLogin(VALORES)).sort()).toEqual(['cpf', 'lembrar', 'perfil', 'senha']);
  });
});
