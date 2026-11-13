import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_CADASTRO_CPF_INVALIDO,
  MENSAGEM_CADASTRO_CPF_OBRIGATORIO,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO,
  MENSAGEM_CADASTRO_NOME_OBRIGATORIO,
  MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
  MENSAGEM_CADASTRO_TELEFONE_INVALIDO,
  MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO,
  mensagemCadastroMenorDeIdade,
} from '@/lib/mensagens';
import {
  criarEsquemaDadosPessoais,
  criarEsquemaDocumentos,
  DADOS_PESSOAIS_INICIAL,
  type DadosPessoaisCadastro,
  type FormularioDocumentos,
} from '@/features/clientes/cadastro/esquemaCadastro';

// Referência fixa: 12/11/2026 (hora local). Os esquemas recebem este "hoje" em vez do relógio.
const HOJE = new Date(2026, 10, 12, 10, 0);
const esquemaEtapa1 = criarEsquemaDadosPessoais(() => HOJE);
const esquemaEtapa2 = criarEsquemaDocumentos(() => HOJE);

const VALIDO_ETAPA_1: DadosPessoaisCadastro = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
};

const VALIDO_ETAPA_2: FormularioDocumentos = {
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

/** Primeira mensagem de erro do campo na etapa 1, ou undefined quando o campo está válido. */
function mensagemEtapa1(campo: keyof DadosPessoaisCadastro, alteracoes: Partial<DadosPessoaisCadastro>) {
  const resultado = esquemaEtapa1.safeParse({ ...VALIDO_ETAPA_1, ...alteracoes });
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('esquemaDadosPessoais (etapa 1)', () => {
  it('aceita os cinco campos válidos, com ou sem máscara', () => {
    expect(esquemaEtapa1.safeParse(VALIDO_ETAPA_1).success).toBe(true);
    expect(
      esquemaEtapa1.safeParse({ ...VALIDO_ETAPA_1, cpf: '52998224725', telefone: '11987654321' }).success,
    ).toBe(true);
  });

  it('só tem os cinco campos da etapa 1 (CNH, endereço e senha são da etapa 2)', () => {
    expect(Object.keys(esquemaEtapa1.shape)).toEqual(['nome', 'cpf', 'dataNascimento', 'email', 'telefone']);
  });

  it('o estado inicial da etapa 1 é recusado campo a campo', () => {
    const resultado = esquemaEtapa1.safeParse(DADOS_PESSOAIS_INICIAL);
    expect(resultado.success).toBe(false);
    if (resultado.success) return;
    // Um campo pode ter mais de um problema; o formulário mostra o primeiro de cada campo.
    const campos = [...new Set(resultado.error.issues.map((issue) => issue.path[0]))];
    expect(campos).toEqual(['nome', 'cpf', 'dataNascimento', 'email', 'telefone']);
  });

  it('nome: recusa vazio e só espaços', () => {
    expect(mensagemEtapa1('nome', { nome: '' })).toBe(MENSAGEM_CADASTRO_NOME_OBRIGATORIO);
    expect(mensagemEtapa1('nome', { nome: '   ' })).toBe(MENSAGEM_CADASTRO_NOME_OBRIGATORIO);
  });

  it('CPF: recusa vazio e dígito verificador errado', () => {
    expect(mensagemEtapa1('cpf', { cpf: '' })).toBe(MENSAGEM_CADASTRO_CPF_OBRIGATORIO);
    expect(mensagemEtapa1('cpf', { cpf: '529.982.247-26' })).toBe(MENSAGEM_CADASTRO_CPF_INVALIDO);
  });

  it('nascimento: recusa vazio e data futura', () => {
    expect(mensagemEtapa1('dataNascimento', { dataNascimento: '' })).toBe(
      MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
    );
    expect(mensagemEtapa1('dataNascimento', { dataNascimento: '2026-11-13' })).toBe(
      MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
    );
  });

  it('nascimento: idade mínima de 18 anos (completa 18 hoje passa; falta um dia, não)', () => {
    expect(mensagemEtapa1('dataNascimento', { dataNascimento: '2008-11-12' })).toBeUndefined();
    expect(mensagemEtapa1('dataNascimento', { dataNascimento: '2008-11-13' })).toBe(
      mensagemCadastroMenorDeIdade(18),
    );
  });

  it('e-mail: recusa vazio e formato inválido', () => {
    expect(mensagemEtapa1('email', { email: '' })).toBe(MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO);
    expect(mensagemEtapa1('email', { email: 'mariana@' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
  });

  it('telefone: recusa vazio e quantidade de dígitos fora de 10 a 11', () => {
    expect(mensagemEtapa1('telefone', { telefone: '' })).toBe(MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO);
    expect(mensagemEtapa1('telefone', { telefone: '(11) 9876-43' })).toBe(MENSAGEM_CADASTRO_TELEFONE_INVALIDO);
  });
});

describe('esquemaDocumentos (etapa 2)', () => {
  it('aceita CNH, endereço vazio e senha válidos', () => {
    expect(esquemaEtapa2.safeParse(VALIDO_ETAPA_2).success).toBe(true);
  });

  it('não tem os dados pessoais (são da etapa 1)', () => {
    expect(Object.keys(esquemaEtapa2.shape)).not.toContain('nome');
    expect(Object.keys(esquemaEtapa2.shape)).not.toContain('cpf');
  });

  it('recusa confirmação de senha diferente, no campo da confirmação', () => {
    const resultado = esquemaEtapa2.safeParse({ ...VALIDO_ETAPA_2, confirmarSenha: 'Outra1234' });
    expect(resultado.success).toBe(false);
    if (resultado.success) return;
    const issue = resultado.error.issues.find((item) => item.path[0] === 'confirmarSenha');
    expect(issue?.message).toBe(MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM);
  });
});
