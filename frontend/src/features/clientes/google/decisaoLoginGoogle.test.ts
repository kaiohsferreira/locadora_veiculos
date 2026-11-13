import { describe, expect, it } from 'vitest';
import { GOOGLE_CONTA_AMBIGUA, MENSAGEM_ERRO_API_PADRAO, MENSAGEM_FALHA_CONEXAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { ResultadoLoginGoogle } from '@/types/autenticacao';
import { decidirAcaoErroGoogle, decidirAcaoResultadoGoogle } from './decisaoLoginGoogle';

describe('decidirAcaoResultadoGoogle', () => {
  it('AUTENTICADO: nada mais a fazer (a sessão já foi aberta)', () => {
    const resultado: ResultadoLoginGoogle = {
      situacao: 'AUTENTICADO',
      sessao: {
        token: 't',
        tipoToken: 'Bearer',
        expiraEm: '2026-11-12T20:00:00',
        expiraEmStr: '',
        perfil: 'CLIENTE',
        idUsuario: 1,
        nome: 'Ana',
      },
    };
    expect(decidirAcaoResultadoGoogle(resultado)).toEqual({ tipo: 'concluido' });
  });

  it('CONFIRMACAO_NECESSARIA: confirmar o vínculo com nome e e-mail', () => {
    expect(
      decidirAcaoResultadoGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', nome: 'Ana', email: 'ana@exemplo.com' }),
    ).toEqual({ tipo: 'confirmar-vinculo', nome: 'Ana', email: 'ana@exemplo.com' });
  });

  it('CADASTRO_NECESSARIO: cadastro com nome e e-mail, com nome nulo preservado', () => {
    expect(
      decidirAcaoResultadoGoogle({ situacao: 'CADASTRO_NECESSARIO', nome: null, email: 'bruno@exemplo.com' }),
    ).toEqual({ tipo: 'cadastrar', nome: null, email: 'bruno@exemplo.com' });
  });

  it('CONTA_AMBIGUA: alerta com a orientação de entrar com CPF e senha', () => {
    expect(decidirAcaoResultadoGoogle({ situacao: 'CONTA_AMBIGUA' })).toEqual({
      tipo: 'alerta',
      mensagem: GOOGLE_CONTA_AMBIGUA,
    });
    expect(GOOGLE_CONTA_AMBIGUA).toBe('Encontramos mais de um cadastro com este e-mail. Entre com CPF e senha.');
  });

  it('e-mail nulo numa situação que pede e-mail vira erro genérico (não vincula nem cadastra)', () => {
    expect(
      decidirAcaoResultadoGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', nome: null, email: null }),
    ).toEqual({ tipo: 'alerta', mensagem: MENSAGEM_ERRO_API_PADRAO });
  });
});

describe('decidirAcaoErroGoogle', () => {
  it('401 (prova do Google inválida) mostra a mensagem da API', () => {
    const erro = new ErroApi({
      status: 401,
      mensagem: 'Não foi possível validar sua conta Google. Tente novamente.',
    });
    expect(decidirAcaoErroGoogle(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Não foi possível validar sua conta Google. Tente novamente.',
    });
  });

  it('409 mostra a mensagem da API', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Esta conta já está vinculada.' });
    expect(decidirAcaoErroGoogle(erro)).toEqual({ tipo: 'alerta', mensagem: 'Esta conta já está vinculada.' });
  });

  it('502 mostra a mensagem da API de indisponibilidade', () => {
    const erro = new ErroApi({
      status: 502,
      mensagem: 'Não foi possível entrar com o Google agora. Entre com CPF e senha ou tente mais tarde.',
    });
    expect(decidirAcaoErroGoogle(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Não foi possível entrar com o Google agora. Entre com CPF e senha ou tente mais tarde.',
    });
  });

  it('429 mostra a mensagem da API (Muitas tentativas), como o login por CPF', () => {
    const erro = new ErroApi({
      status: 429,
      mensagem: 'Muitas tentativas. Aguarde alguns instantes e tente novamente.',
    });
    expect(decidirAcaoErroGoogle(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Muitas tentativas. Aguarde alguns instantes e tente novamente.',
    });
  });

  it('falha de rede (status 0) mostra a mensagem de conexão do front', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_FALHA_CONEXAO });
    expect(decidirAcaoErroGoogle(erro)).toEqual({ tipo: 'alerta', mensagem: MENSAGEM_FALHA_CONEXAO });
  });

  it('erro que não é da API vira a mensagem padrão', () => {
    expect(decidirAcaoErroGoogle(new TypeError('quebrou'))).toEqual({
      tipo: 'alerta',
      mensagem: MENSAGEM_ERRO_API_PADRAO,
    });
  });
});
