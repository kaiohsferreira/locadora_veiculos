import { describe, expect, it } from 'vitest';
import { ACESSO_ERRO_CPF_DIGITOS, MENSAGEM_ERRO_API_PADRAO, MENSAGEM_FALHA_CONEXAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import {
  decidirErroSolicitacao,
  montarRequisicaoSolicitacao,
  perfilInicialRecuperacao,
} from './fluxoRecuperacao';

describe('montarRequisicaoSolicitacao', () => {
  it('envia o perfil e o CPF só com dígitos (com máscara na entrada)', () => {
    expect(montarRequisicaoSolicitacao({ perfil: 'CLIENTE', cpf: '529.982.247-25' })).toEqual({
      perfil: 'CLIENTE',
      cpf: '52998224725',
    });
  });

  it('mantém o perfil FUNCIONARIO', () => {
    expect(montarRequisicaoSolicitacao({ perfil: 'FUNCIONARIO', cpf: '52998224725' })).toEqual({
      perfil: 'FUNCIONARIO',
      cpf: '52998224725',
    });
  });
});

describe('perfilInicialRecuperacao', () => {
  it('usa o perfil recebido pelo link (state)', () => {
    expect(perfilInicialRecuperacao({ perfil: 'FUNCIONARIO' })).toBe('FUNCIONARIO');
    expect(perfilInicialRecuperacao({ perfil: 'CLIENTE' })).toBe('CLIENTE');
  });

  it('sem state, vale Cliente', () => {
    expect(perfilInicialRecuperacao(null)).toBe('CLIENTE');
    expect(perfilInicialRecuperacao(undefined)).toBe('CLIENTE');
    expect(perfilInicialRecuperacao('CLIENTE')).toBe('CLIENTE');
    expect(perfilInicialRecuperacao({})).toBe('CLIENTE');
  });

  it('ignora valor de perfil inválido', () => {
    expect(perfilInicialRecuperacao({ perfil: 'ADMIN' })).toBe('CLIENTE');
    expect(perfilInicialRecuperacao({ perfil: 'cliente' })).toBe('CLIENTE');
    expect(perfilInicialRecuperacao({ perfil: 42 })).toBe('CLIENTE');
  });
});

describe('decidirErroSolicitacao', () => {
  it('400 com campo de CPF: a mensagem vai para o campo', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { cpf: [ACESSO_ERRO_CPF_DIGITOS] },
    });
    expect(decidirErroSolicitacao(erro)).toEqual({
      tipo: 'campos',
      campos: { cpf: ACESSO_ERRO_CPF_DIGITOS },
    });
  });

  it('400 com campo de perfil, em qualquer caixa do nome', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { Perfil: ['Escolha o perfil.'] },
    });
    expect(decidirErroSolicitacao(erro)).toEqual({
      tipo: 'campos',
      campos: { perfil: 'Escolha o perfil.' },
    });
  });

  it('400 sem campo conhecido: alerta geral com a mensagem da API', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'Dados inválidos.' });
    expect(decidirErroSolicitacao(erro)).toEqual({ tipo: 'alerta', mensagem: 'Dados inválidos.' });
  });

  it('429: alerta geral com a mensagem da API', () => {
    const erro = new ErroApi({ status: 429, mensagem: 'Muitas tentativas. Aguarde um minuto.' });
    expect(decidirErroSolicitacao(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Muitas tentativas. Aguarde um minuto.',
    });
  });

  it('falha de rede (sem resposta): alerta geral com a mensagem de conexão', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_FALHA_CONEXAO });
    expect(decidirErroSolicitacao(erro)).toEqual({ tipo: 'alerta', mensagem: MENSAGEM_FALHA_CONEXAO });
  });

  it('erro que não é da API vira a mensagem padrão, nunca o texto técnico', () => {
    expect(decidirErroSolicitacao(new Error('stack técnica'))).toEqual({
      tipo: 'alerta',
      mensagem: MENSAGEM_ERRO_API_PADRAO,
    });
  });
});
