import { describe, expect, it } from 'vitest';
import {
  GOOGLE_VINCULAR_PROVA_EXPIRADA,
  MENSAGEM_CREDENCIAIS_INVALIDAS,
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_FALHA_CONEXAO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import { decidirErroVinculo, montarRequisicaoVinculo } from './vinculoGoogle';

describe('montarRequisicaoVinculo', () => {
  it('manda o CPF só com dígitos, a credencial, a senha e o lembrar booleano', () => {
    expect(
      montarRequisicaoVinculo({ cpf: '529.982.247-25', senha: 'Senha#1' }, 'jwt-de-teste', true),
    ).toEqual({
      credencial: 'jwt-de-teste',
      cpf: '52998224725',
      senha: 'Senha#1',
      lembrar: true,
    });
  });

  it('lembrar falso vai como false', () => {
    expect(montarRequisicaoVinculo({ cpf: '52998224725', senha: 'x' }, 'c', false).lembrar).toBe(false);
  });
});

describe('decidirErroVinculo', () => {
  it('400 com campo conhecido aponta o campo', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'CPF inválido.', erros: { Cpf: ['CPF inválido.'] } });
    expect(decidirErroVinculo(erro)).toEqual({ tipo: 'campos', campos: { cpf: 'CPF inválido.' } });
  });

  it('400 sem campo conhecido vira alerta geral', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'Dados inválidos.', erros: { outro: ['x'] } });
    expect(decidirErroVinculo(erro)).toEqual({ tipo: 'alerta', mensagem: 'Dados inválidos.', voltarAoLogin: false });
  });

  it('400 que aponta a credencial: volta ao login com o texto do front (sem "tente novamente")', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'Informe a credencial do Google.',
      erros: { credencial: ['Informe a credencial do Google.'] },
    });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'A confirmação com o Google expirou. Volte ao login e escolha Continuar com o Google de novo.',
      voltarAoLogin: true,
    });
  });

  it('401 de CPF ou senha: senha recusada, sem voltar ao login', () => {
    const erro = new ErroApi({ status: 401, mensagem: MENSAGEM_CREDENCIAIS_INVALIDAS });
    expect(decidirErroVinculo(erro)).toEqual({ tipo: 'senha-recusada', mensagem: 'CPF ou senha inválidos.' });
  });

  it('401 de prova do Google: volta ao login com o texto do front, não o da API', () => {
    const erro = new ErroApi({
      status: 401,
      mensagem: 'Não foi possível validar sua conta Google. Tente novamente.',
    });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'A confirmação com o Google expirou. Volte ao login e escolha Continuar com o Google de novo.',
      voltarAoLogin: true,
    });
  });

  it('409 mostra a mensagem da API e oferece voltar ao login', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Este cadastro já está vinculado a outra conta Google.' });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Este cadastro já está vinculado a outra conta Google.',
      voltarAoLogin: true,
    });
  });

  it('429 mostra a mensagem da API (Muitas tentativas) e não oferece voltar', () => {
    const erro = new ErroApi({
      status: 429,
      mensagem: 'Muitas tentativas. Aguarde alguns instantes e tente novamente.',
    });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Muitas tentativas. Aguarde alguns instantes e tente novamente.',
      voltarAoLogin: false,
    });
  });

  it('502 mostra a mensagem da API e não oferece voltar', () => {
    const erro = new ErroApi({ status: 502, mensagem: 'Não foi possível entrar com o Google agora.' });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Não foi possível entrar com o Google agora.',
      voltarAoLogin: false,
    });
  });

  it('falha de rede mostra a mensagem de conexão e não oferece voltar', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_FALHA_CONEXAO });
    expect(decidirErroVinculo(erro)).toEqual({
      tipo: 'alerta',
      mensagem: MENSAGEM_FALHA_CONEXAO,
      voltarAoLogin: false,
    });
  });

  it('erro que não é da API vira a mensagem padrão', () => {
    expect(decidirErroVinculo(new Error('x'))).toEqual({
      tipo: 'alerta',
      mensagem: MENSAGEM_ERRO_API_PADRAO,
      voltarAoLogin: false,
    });
  });

  it('o texto da prova expirada não diz "tente novamente" (reenviar a mesma prova não adianta)', () => {
    expect(GOOGLE_VINCULAR_PROVA_EXPIRADA).not.toContain('tente novamente');
  });
});
