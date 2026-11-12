import { describe, expect, it } from 'vitest';
import { MENSAGEM_CADASTRO_SENHA_FRACA, MENSAGEM_ERRO_API_PADRAO, MENSAGEM_FALHA_CONEXAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import { decidirErroRedefinicao, montarRequisicaoRedefinicao } from './fluxoRedefinicao';

describe('montarRequisicaoRedefinicao', () => {
  it('envia token, nova senha e confirmação (os três campos do contrato)', () => {
    expect(
      montarRequisicaoRedefinicao('token-do-link', { novaSenha: 'abcdefg1', confirmacaoSenha: 'abcdefg1' }),
    ).toEqual({ token: 'token-do-link', novaSenha: 'abcdefg1', confirmacaoSenha: 'abcdefg1' });
  });
});

describe('decidirErroRedefinicao', () => {
  it('409: link inválido (mesma decisão para expirado, usado, adulterado etc.)', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'O link de redefinição é inválido ou expirou. Solicite um novo.' });
    expect(decidirErroRedefinicao(erro)).toEqual({ tipo: 'link-invalido' });
  });

  it('400 apontando o token: link inválido', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { token: ['O link de redefinição está incompleto. Solicite um novo.'] },
    });
    expect(decidirErroRedefinicao(erro)).toEqual({ tipo: 'link-invalido' });
  });

  it('400 com o token junto de um campo de senha: link inválido (o token tem prioridade)', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { token: ['Token inválido.'], novaSenha: [MENSAGEM_CADASTRO_SENHA_FRACA] },
    });
    expect(decidirErroRedefinicao(erro)).toEqual({ tipo: 'link-invalido' });
  });

  it('400 com campo de senha: a mensagem vai para o campo', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { novaSenha: [MENSAGEM_CADASTRO_SENHA_FRACA] },
    });
    expect(decidirErroRedefinicao(erro)).toEqual({
      tipo: 'campos',
      campos: { novaSenha: MENSAGEM_CADASTRO_SENHA_FRACA },
    });
  });

  it('400 com confirmação: a mensagem vai para confirmacaoSenha', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'x',
      erros: { confirmacaoSenha: ['A confirmação não é igual à nova senha.'] },
    });
    expect(decidirErroRedefinicao(erro)).toEqual({
      tipo: 'campos',
      campos: { confirmacaoSenha: 'A confirmação não é igual à nova senha.' },
    });
  });

  it('400 sem campo conhecido: alerta geral', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'Requisição inválida.' });
    expect(decidirErroRedefinicao(erro)).toEqual({ tipo: 'alerta', mensagem: 'Requisição inválida.' });
  });

  it('429: alerta geral (o formulário e o token seguem para nova tentativa)', () => {
    const erro = new ErroApi({ status: 429, mensagem: 'Muitas tentativas. Aguarde um minuto.' });
    expect(decidirErroRedefinicao(erro)).toEqual({
      tipo: 'alerta',
      mensagem: 'Muitas tentativas. Aguarde um minuto.',
    });
  });

  it('falha de rede: alerta geral com a mensagem de conexão', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_FALHA_CONEXAO });
    expect(decidirErroRedefinicao(erro)).toEqual({ tipo: 'alerta', mensagem: MENSAGEM_FALHA_CONEXAO });
  });

  it('erro que não é da API: alerta com a mensagem padrão', () => {
    expect(decidirErroRedefinicao('falha qualquer')).toEqual({
      tipo: 'alerta',
      mensagem: MENSAGEM_ERRO_API_PADRAO,
    });
  });
});
