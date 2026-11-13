import { describe, expect, it, vi } from 'vitest';
import {
  escolherCampoDoConflito,
  executarCadastro,
  primeiroCampoNaTela,
  type DependenciasCadastro,
} from '@/features/clientes/cadastro/fluxoCadastro';
import type { FormularioCadastro } from '@/features/clientes/cadastro/esquemaCadastro';
import {
  MENSAGEM_CADASTRO_CNH_JA_CADASTRADA,
  MENSAGEM_CADASTRO_CPF_JA_CADASTRADO,
  MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO,
} from '@/lib/mensagens';
import type { RespostaCliente } from '@/types/clientes';

const FORMULARIO: FormularioCadastro = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cep: '',
  logradouro: '',
  numero: '',
  complemento: '',
  bairro: '',
  cidade: '',
  uf: '',
  senha: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

const RESPOSTA: RespostaCliente = {
  idCliente: 1,
  nome: 'Mariana Ribeiro de Souza',
  cpf: '52998224725',
  cpfStr: '529.982.247-25',
  dataNascimento: '1992-08-14',
  dataNascimentoStr: '14/08/1992',
  email: 'mariana@email.com',
  telefone: '11987654321',
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cnhValidadeStr: '10/05/2028',
  logradouro: null,
  numero: null,
  complemento: null,
  bairro: null,
  cidade: null,
  uf: null,
  cep: null,
  dataCadastro: '2026-11-12T10:00:00',
  dataCadastroStr: '12/11/2026 10:00',
  statusCadastro: 'PENDENTE',
};

/** Dublês: o POST e o login com resultado controlado por cada teste. */
function criarDublês(opcoes: { cadastroFalha?: unknown; loginFalha?: boolean }) {
  const cadastrar = vi.fn<DependenciasCadastro['cadastrar']>(async () => {
    if (opcoes.cadastroFalha !== undefined) throw opcoes.cadastroFalha;
    return RESPOSTA;
  });
  const entrar = vi.fn<DependenciasCadastro['entrar']>(async () => {
    if (opcoes.loginFalha) throw new Error('429');
    return {};
  });
  return { dependencias: { cadastrar, entrar }, cadastrar, entrar };
}

describe('escolherCampoDoConflito', () => {
  it('aponta CPF quando a mensagem cita CPF', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este CPF.')).toBe('cpf');
  });

  it('aponta cnhNumero quando a mensagem cita CNH', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este número de CNH.')).toBe('cnhNumero');
  });

  it('aponta email com a mensagem exata do backend', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este e-mail.')).toBe('email');
  });

  it('reconhece e-mail com ou sem hífen e em qualquer caixa', () => {
    expect(escolherCampoDoConflito('E-MAIL já cadastrado.')).toBe('email');
    expect(escolherCampoDoConflito('Este email já existe.')).toBe('email');
  });

  it('devolve null para outra mensagem (vai para o alerta geral)', () => {
    expect(escolherCampoDoConflito('Não foi possível concluir a operação.')).toBeNull();
  });

  it('cada mensagem de duplicidade do front mapeia de volta ao campo certo', () => {
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_CPF_JA_CADASTRADO)).toBe('cpf');
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO)).toBe('email');
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_CNH_JA_CADASTRADA)).toBe('cnhNumero');
  });

  it('as mensagens de duplicidade têm o texto exato do backend', () => {
    expect(MENSAGEM_CADASTRO_CPF_JA_CADASTRADO).toBe('Já existe um cliente ativo com este CPF.');
    expect(MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO).toBe('Já existe um cliente ativo com este e-mail.');
    expect(MENSAGEM_CADASTRO_CNH_JA_CADASTRADA).toBe('Já existe um cliente ativo com este número de CNH.');
  });
});

describe('primeiroCampoNaTela', () => {
  it('escolhe o campo mais alto na tela, mesmo que a lista venha em outra ordem', () => {
    expect(primeiroCampoNaTela(['senha', 'cep', 'nome'])).toBe('nome');
    expect(primeiroCampoNaTela(['uf', 'cpf'])).toBe('cpf');
  });

  it('devolve undefined sem campos', () => {
    expect(primeiroCampoNaTela([])).toBeUndefined();
  });
});

describe('executarCadastro', () => {
  it('POST falha: o login não é chamado e o erro volta para a página', async () => {
    const erro = new Error('409');
    const { dependencias, entrar } = criarDublês({ cadastroFalha: erro });

    const resultado = await executarCadastro(dependencias, FORMULARIO);

    expect(resultado).toEqual({ tipo: 'erro-cadastro', erro });
    expect(entrar).not.toHaveBeenCalled();
  });

  it('POST ok e login ok: devolve com-login', async () => {
    const { dependencias } = criarDublês({});
    expect(await executarCadastro(dependencias, FORMULARIO)).toEqual({ tipo: 'com-login' });
  });

  it('POST ok e login falha: devolve sem-login e o POST foi chamado UMA vez só', async () => {
    const { dependencias, cadastrar, entrar } = criarDublês({ loginFalha: true });

    const resultado = await executarCadastro(dependencias, FORMULARIO);

    expect(resultado).toEqual({ tipo: 'sem-login' });
    expect(cadastrar).toHaveBeenCalledTimes(1);
    expect(entrar).toHaveBeenCalledTimes(1);
  });

  it('login automático usa perfil CLIENTE e CPF só com dígitos', async () => {
    const { dependencias, entrar } = criarDublês({});

    await executarCadastro(dependencias, FORMULARIO);

    expect(entrar).toHaveBeenCalledWith({
      perfil: 'CLIENTE',
      cpf: '52998224725',
      senha: 'Senha1234',
    });
  });

  it('o POST recebe o payload sem a confirmação de senha', async () => {
    const { dependencias, cadastrar } = criarDublês({});

    await executarCadastro(dependencias, FORMULARIO);

    const [requisicao] = cadastrar.mock.calls[0];
    expect(Object.keys(requisicao)).not.toContain('confirmarSenha');
  });

  it('falha de conversão (categoria vazia) é tratada como erro do cadastro, sem POST', async () => {
    const { dependencias, cadastrar, entrar } = criarDublês({});

    const resultado = await executarCadastro(dependencias, { ...FORMULARIO, cnhCategoria: '' });

    expect(resultado.tipo).toBe('erro-cadastro');
    expect(cadastrar).not.toHaveBeenCalled();
    expect(entrar).not.toHaveBeenCalled();
  });
});
