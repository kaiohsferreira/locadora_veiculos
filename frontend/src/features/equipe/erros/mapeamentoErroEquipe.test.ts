import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_EQUIPE_ACESSO_NEGADO,
  MENSAGEM_EQUIPE_CPF_JA_CADASTRADO,
  MENSAGEM_EQUIPE_FUNCIONARIO_NAO_ENCONTRADO,
  MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA,
  MENSAGEM_EQUIPE_PROPRIO_ACESSO,
  MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR,
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_FALHA_CONEXAO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import { interpretarErroFuncionario, primeiroCampoComErro } from './mapeamentoErroEquipe';

const OUTRO_USUARIO = { ehPropriaConta: false };
const PROPRIA_CONTA = { ehPropriaConta: true };

/**
 * Textos reais dos 409 da equipe (ConstantsMessage.Equipe.cs), fixados como literais: uma mudança de texto no back
 * quebra este teste, não a tela.
 */
describe('interpretarErroFuncionario: textos reais dos 409 do backend, fixados', () => {
  it('ErroEquipeCpfDuplicado vai para o campo CPF', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Já existe um funcionário cadastrado com este CPF.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO).campos).toEqual({
      cpf: 'Já existe um funcionário cadastrado com este CPF.',
    });
  });

  it('ErroEquipeMatriculaDuplicada vai para o campo matrícula', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Já existe um funcionário com esta matrícula nesta locadora.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO).campos).toEqual({
      matricula: 'Já existe um funcionário com esta matrícula nesta locadora.',
    });
  });

  it('ErroEquipeUltimoAdministrador vira aviso geral', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'A locadora precisa manter pelo menos um administrador ativo.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({
      campos: {},
      aviso: 'A locadora precisa manter pelo menos um administrador ativo.',
    });
  });

  it('ErroEquipePropriaConta, na própria conta, vira o aviso de próprio acesso', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Você não pode alterar o próprio acesso. Peça a outro administrador.' });
    expect(interpretarErroFuncionario(erro, PROPRIA_CONTA)).toEqual({
      campos: {},
      aviso: MENSAGEM_EQUIPE_PROPRIO_ACESSO,
    });
  });
});

describe('primeiroCampoComErro', () => {
  it('devolve o primeiro campo com erro na ordem da tela, não na ordem do objeto', () => {
    expect(primeiroCampoComErro({ email: 'x', cpf: 'y' })).toBe('cpf');
  });

  it('sem erro de campo, devolve undefined', () => {
    expect(primeiroCampoComErro({})).toBeUndefined();
  });
});

describe('interpretarErroFuncionario: 400', () => {
  it('leva cada erro de campo ao campo do formulário, sem diferença de maiúsculas', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'Alguns dados estão inválidos. Confira os campos destacados.',
      erros: { Nome: ['Informe o nome do funcionário.'], dataAdmissao: ['A data de admissão não pode ser futura.'] },
    });

    const resultado = interpretarErroFuncionario(erro, OUTRO_USUARIO);

    expect(resultado.campos).toEqual({
      nome: 'Informe o nome do funcionário.',
      dataAdmissao: 'A data de admissão não pode ser futura.',
    });
    expect(resultado.aviso).toBeNull();
  });

  it('ignora chaves que não são campos do formulário', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'Alguns dados estão inválidos. Confira os campos destacados.',
      erros: { idEmpresa: ['técnico'] },
    });

    const resultado = interpretarErroFuncionario(erro, OUTRO_USUARIO);

    expect(resultado.campos).toEqual({});
    expect(resultado.aviso).toBe('Alguns dados estão inválidos. Confira os campos destacados.');
  });

  it('sem campos, mostra a mensagem já convertida pelo cliente', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'O VO enviado não é válido.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({ campos: {}, aviso: 'O VO enviado não é válido.' });
  });
});

describe('interpretarErroFuncionario: 409', () => {
  it('CPF já cadastrado vai para o campo CPF', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_EQUIPE_CPF_JA_CADASTRADO });

    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({
      campos: { cpf: MENSAGEM_EQUIPE_CPF_JA_CADASTRADO },
      aviso: null,
    });
  });

  it('matrícula já usada na locadora vai para o campo matrícula', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA });

    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({
      campos: { matricula: MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA },
      aviso: null,
    });
  });

  it('último administrador ativo vira aviso, mesmo na própria conta', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR });

    expect(interpretarErroFuncionario(erro, PROPRIA_CONTA)).toEqual({
      campos: {},
      aviso: MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR,
    });
  });

  it('409 desconhecido na própria conta mostra o texto do servidor, não o aviso de próprio acesso', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Operação recusada pelo servidor.' });

    expect(interpretarErroFuncionario(erro, PROPRIA_CONTA)).toEqual({
      campos: {},
      aviso: 'Operação recusada pelo servidor.',
    });
  });

  it('409 de sistema (ex.: restrição do banco) não vira o aviso de próprio acesso, mesmo na própria conta', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Registro em uso por outro processo.' });

    expect(interpretarErroFuncionario(erro, PROPRIA_CONTA).aviso).toBe('Registro em uso por outro processo.');
  });

  it('o aviso próprio cita administrador sem ser confundido com o último administrador', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_EQUIPE_PROPRIO_ACESSO });

    expect(interpretarErroFuncionario(erro, PROPRIA_CONTA).aviso).toBe(MENSAGEM_EQUIPE_PROPRIO_ACESSO);
  });

  it('409 sem relação com a própria conta mostra a mensagem do servidor já convertida', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Os dados mudaram. Atualize a página.' });

    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO).aviso).toBe('Os dados mudaram. Atualize a página.');
  });
});

describe('interpretarErroFuncionario: 403, 404, rede e demais', () => {
  it('403 tem mensagem própria da equipe', () => {
    const erro = new ErroApi({ status: 403, mensagem: 'Você não tem permissão para realizar esta ação.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({ campos: {}, aviso: MENSAGEM_EQUIPE_ACESSO_NEGADO });
  });

  it('404 tem mensagem própria do funcionário não encontrado', () => {
    const erro = new ErroApi({ status: 404, mensagem: 'O registro solicitado não foi encontrado.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO)).toEqual({
      campos: {},
      aviso: MENSAGEM_EQUIPE_FUNCIONARIO_NAO_ENCONTRADO,
    });
  });

  it('falha de rede usa a mensagem de conexão do cliente', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_FALHA_CONEXAO });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO).aviso).toBe(MENSAGEM_FALHA_CONEXAO);
  });

  it('erro 500 usa a mensagem genérica do cliente', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' });
    expect(interpretarErroFuncionario(erro, OUTRO_USUARIO).aviso).toBe(
      'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.',
    );
  });

  it('o que não é ErroApi vira a mensagem padrão, nunca o texto técnico', () => {
    const resultado = interpretarErroFuncionario(new Error('TypeError: Cannot read properties of undefined'), OUTRO_USUARIO);
    expect(resultado).toEqual({ campos: {}, aviso: MENSAGEM_ERRO_API_PADRAO });
  });
});
