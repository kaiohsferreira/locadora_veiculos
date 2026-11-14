import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_SENHA_OBRIGATORIA,
  MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
  mensagemCadastroCampoTamanho,
  mensagemCadastroSenhaTamanho,
  MENSAGEM_LOCADORA_ACEITE_TERMOS_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_EMAIL_OBRIGATORIO,
  MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO,
  MENSAGEM_LOCADORA_SENHA_FRACA,
} from '@/lib/mensagens';
import {
  CAMPOS_RESPONSAVEL,
  DADOS_RESPONSAVEL_INICIAL,
  ehCampoResponsavel,
  ESQUEMA_RESPONSAVEL,
  extrairDadosResponsavel,
  type FormularioResponsavel,
} from './esquemaResponsavel';

/** CPF com dígitos verificadores corretos (529.982.247-25), conferido à mão. */
const CPF_VALIDO = '529.982.247-25';

const VALORES_VALIDOS: FormularioResponsavel = {
  nome: 'Marina Costa',
  cpf: CPF_VALIDO,
  email: 'marina@rotasul.com.br',
  senha: 'Rotasul@2025!',
  confirmarSenha: 'Rotasul@2025!',
  aceiteTermos: true,
};

/** Primeira mensagem de erro do campo (ou do objeto, quando o campo é a confirmação). */
function mensagemDo(campo: keyof FormularioResponsavel, valores: Partial<FormularioResponsavel>): string | undefined {
  const resultado = ESQUEMA_RESPONSAVEL.safeParse({ ...VALORES_VALIDOS, ...valores });
  if (resultado.success) return undefined;
  return resultado.error.issues.find((problema) => problema.path[0] === campo)?.message;
}

describe('ESQUEMA_RESPONSAVEL: campos da etapa 2 (tela 37)', () => {
  it('aceita um responsável completo com CPF válido, senha forte e aceite', () => {
    expect(ESQUEMA_RESPONSAVEL.safeParse(VALORES_VALIDOS).success).toBe(true);
  });

  it('nome: obrigatório e no máximo 120 caracteres', () => {
    expect(mensagemDo('nome', { nome: '' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO);
    expect(mensagemDo('nome', { nome: '   ' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO);
    expect(mensagemDo('nome', { nome: 'a'.repeat(121) })).toBe(mensagemCadastroCampoTamanho('nome', 120));
    expect(mensagemDo('nome', { nome: 'a'.repeat(120) })).toBeUndefined();
  });

  it('CPF: obrigatório, com dígitos verificadores certos (aceita máscara)', () => {
    expect(mensagemDo('cpf', { cpf: '' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_CPF_OBRIGATORIO);
    expect(mensagemDo('cpf', { cpf: '111.111.111-11' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO);
    expect(mensagemDo('cpf', { cpf: '341.892.409-12' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO);
    expect(mensagemDo('cpf', { cpf: '52998224725' })).toBeUndefined();
  });

  it('e-mail: obrigatório, formato válido e no máximo 120 caracteres', () => {
    expect(mensagemDo('email', { email: '' })).toBe(MENSAGEM_LOCADORA_RESPONSAVEL_EMAIL_OBRIGATORIO);
    expect(mensagemDo('email', { email: 'marina@' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
    expect(mensagemDo('email', { email: 'marina com@rotasul.com.br' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
    const longo = `${'a'.repeat(115)}@x.com`;
    expect(longo).toHaveLength(121);
    expect(mensagemDo('email', { email: longo })).toBe(mensagemCadastroCampoTamanho('e-mail', 120));
  });

  it('senha: vazia, com 7 caracteres e com 65 caracteres são recusadas', () => {
    expect(mensagemDo('senha', { senha: '', confirmarSenha: '' })).toBe(MENSAGEM_CADASTRO_SENHA_OBRIGATORIA);
    expect(mensagemDo('senha', { senha: 'Abcdef1', confirmarSenha: 'Abcdef1' })).toBe(
      mensagemCadastroSenhaTamanho(8, 64),
    );
    const longa = `A1${'a'.repeat(63)}`;
    expect(longa).toHaveLength(65);
    expect(mensagemDo('senha', { senha: longa, confirmarSenha: longa })).toBe(mensagemCadastroSenhaTamanho(8, 64));
  });

  it('senha: com 64 caracteres (com maiúscula e número) é aceita', () => {
    const limite = `A1${'a'.repeat(62)}`;
    expect(limite).toHaveLength(64);
    expect(mensagemDo('senha', { senha: limite, confirmarSenha: limite })).toBeUndefined();
  });

  it('senha: exige uma letra MAIÚSCULA e um número (oito caracteres sem maiúscula ou sem número falham)', () => {
    expect(mensagemDo('senha', { senha: 'abcdefg1', confirmarSenha: 'abcdefg1' })).toBe(MENSAGEM_LOCADORA_SENHA_FRACA);
    expect(mensagemDo('senha', { senha: 'Abcdefgh', confirmarSenha: 'Abcdefgh' })).toBe(MENSAGEM_LOCADORA_SENHA_FRACA);
    expect(mensagemDo('senha', { senha: 'ABCDEFG1', confirmarSenha: 'ABCDEFG1' })).toBeUndefined();
  });

  it('confirmação diferente da senha é recusada no campo de confirmação', () => {
    expect(mensagemDo('confirmarSenha', { confirmarSenha: 'Rotasul@2026!' })).toBe(
      MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
    );
  });

  it('aceite dos termos desmarcado é recusado', () => {
    expect(mensagemDo('aceiteTermos', { aceiteTermos: false })).toBe(MENSAGEM_LOCADORA_ACEITE_TERMOS_OBRIGATORIO);
  });

  it('o aceite começa desmarcado, como escolha explícita da pessoa', () => {
    expect(DADOS_RESPONSAVEL_INICIAL.aceiteTermos).toBe(false);
  });
});

describe('extrairDadosResponsavel: nada da senha vai para a memória da tela', () => {
  it('devolve só nome, CPF, e-mail e aceite', () => {
    expect(Object.keys(extrairDadosResponsavel(VALORES_VALIDOS)).sort()).toEqual(
      ['aceiteTermos', 'cpf', 'email', 'nome'].sort(),
    );
  });

  it('o resultado não traz a senha nem a confirmação, nem como texto', () => {
    const dados = extrairDadosResponsavel(VALORES_VALIDOS);
    expect(JSON.stringify(dados)).not.toContain('Rotasul@2025!');
    expect(dados).not.toHaveProperty('senha');
    expect(dados).not.toHaveProperty('confirmarSenha');
  });
});

describe('campos da etapa 2', () => {
  it('a lista de campos é a da tela, sem o cargo nem o celular que o contrato não tem', () => {
    expect(CAMPOS_RESPONSAVEL).toEqual(['nome', 'cpf', 'email', 'senha', 'confirmarSenha', 'aceiteTermos']);
  });

  it('ehCampoResponsavel reconhece só os campos da etapa', () => {
    expect(ehCampoResponsavel('cpf')).toBe(true);
    expect(ehCampoResponsavel('cnpj')).toBe(false);
    expect(ehCampoResponsavel('cargo')).toBe(false);
  });
});
