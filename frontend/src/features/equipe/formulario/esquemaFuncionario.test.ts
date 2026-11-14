import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_EQUIPE_ADMISSAO_FORMATO,
  MENSAGEM_EQUIPE_ADMISSAO_FUTURA,
  MENSAGEM_EQUIPE_ADMISSAO_OBRIGATORIA,
  MENSAGEM_EQUIPE_CARGO_OBRIGATORIO,
  MENSAGEM_EQUIPE_CONFIRMACAO_SENHA,
  MENSAGEM_EQUIPE_CPF_INVALIDO,
  MENSAGEM_EQUIPE_CPF_OBRIGATORIO,
  MENSAGEM_EQUIPE_EMAIL_INVALIDO,
  MENSAGEM_EQUIPE_EMAIL_OBRIGATORIO,
  MENSAGEM_EQUIPE_MATRICULA_OBRIGATORIA,
  MENSAGEM_EQUIPE_NOME_OBRIGATORIO,
  MENSAGEM_EQUIPE_SENHA_FRACA,
  MENSAGEM_EQUIPE_SENHA_OBRIGATORIA,
  MENSAGEM_EQUIPE_SENHA_TAMANHO,
  mensagemEquipeCampoTamanho,
} from '@/lib/mensagens';
import {
  CAMPOS_FORMULARIO_FUNCIONARIO,
  ehCampoFormularioFuncionario,
  ESQUEMA_CADASTRO_FUNCIONARIO,
  ESQUEMA_EDICAO_FUNCIONARIO,
  type FormularioFuncionario,
} from './esquemaFuncionario';

/** Data de hoje no formato DD/MM/AAAA, calculada na hora do teste. */
function hojeBr(): string {
  const agora = new Date();
  const dia = String(agora.getDate()).padStart(2, '0');
  const mes = String(agora.getMonth() + 1).padStart(2, '0');
  return `${dia}/${mes}/${agora.getFullYear()}`;
}

const VALIDO: FormularioFuncionario = {
  nome: 'Carlos Eduardo Silveira',
  cpf: '529.982.247-25',
  matricula: 'MAT-0200',
  email: 'carlos@rotasul.com.br',
  dataAdmissao: '23/02/2025',
  cargo: 'ATENDENTE',
  senhaInicial: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

/** Mensagem do primeiro erro do campo (ou undefined quando o campo passou). */
function erroDoCampo(valores: FormularioFuncionario, campo: string): string | undefined {
  const resultado = ESQUEMA_CADASTRO_FUNCIONARIO.safeParse(valores);
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('esquema do cadastro', () => {
  it('aceita um funcionário completo', () => {
    expect(ESQUEMA_CADASTRO_FUNCIONARIO.safeParse(VALIDO).success).toBe(true);
  });

  it('nome: obrigatório e até 120 caracteres', () => {
    expect(erroDoCampo({ ...VALIDO, nome: '   ' }, 'nome')).toBe(MENSAGEM_EQUIPE_NOME_OBRIGATORIO);
    expect(erroDoCampo({ ...VALIDO, nome: 'a'.repeat(121) }, 'nome')).toBe(
      mensagemEquipeCampoTamanho('nome', 120),
    );
    expect(erroDoCampo({ ...VALIDO, nome: 'a'.repeat(120) }, 'nome')).toBeUndefined();
  });

  it('CPF: obrigatório e com dígitos verificadores válidos', () => {
    expect(erroDoCampo({ ...VALIDO, cpf: '' }, 'cpf')).toBe(MENSAGEM_EQUIPE_CPF_OBRIGATORIO);
    expect(erroDoCampo({ ...VALIDO, cpf: '111.111.111-11' }, 'cpf')).toBe(MENSAGEM_EQUIPE_CPF_INVALIDO);
    expect(erroDoCampo({ ...VALIDO, cpf: '529.982.247-26' }, 'cpf')).toBe(MENSAGEM_EQUIPE_CPF_INVALIDO);
    expect(erroDoCampo(VALIDO, 'cpf')).toBeUndefined();
  });

  it('matrícula: obrigatória e até 20 caracteres', () => {
    expect(erroDoCampo({ ...VALIDO, matricula: ' ' }, 'matricula')).toBe(MENSAGEM_EQUIPE_MATRICULA_OBRIGATORIA);
    expect(erroDoCampo({ ...VALIDO, matricula: 'M'.repeat(21) }, 'matricula')).toBe(
      mensagemEquipeCampoTamanho('matrícula', 20),
    );
  });

  it('e-mail: obrigatório, com formato simples e até 120 caracteres', () => {
    expect(erroDoCampo({ ...VALIDO, email: '' }, 'email')).toBe(MENSAGEM_EQUIPE_EMAIL_OBRIGATORIO);
    expect(erroDoCampo({ ...VALIDO, email: 'carlos.rotasul.com.br' }, 'email')).toBe(MENSAGEM_EQUIPE_EMAIL_INVALIDO);
    expect(erroDoCampo({ ...VALIDO, email: 'a b@rotasul.com.br' }, 'email')).toBe(MENSAGEM_EQUIPE_EMAIL_INVALIDO);
    expect(erroDoCampo({ ...VALIDO, email: `${'a'.repeat(115)}@x.com` }, 'email')).toBe(
      mensagemEquipeCampoTamanho('e-mail', 120),
    );
  });

  it('data de admissão: obrigatória, em DD/MM/AAAA real e não futura', () => {
    expect(erroDoCampo({ ...VALIDO, dataAdmissao: '' }, 'dataAdmissao')).toBe(MENSAGEM_EQUIPE_ADMISSAO_OBRIGATORIA);
    expect(erroDoCampo({ ...VALIDO, dataAdmissao: '2025-02-23' }, 'dataAdmissao')).toBe(
      MENSAGEM_EQUIPE_ADMISSAO_FORMATO,
    );
    expect(erroDoCampo({ ...VALIDO, dataAdmissao: '31/02/2025' }, 'dataAdmissao')).toBe(
      MENSAGEM_EQUIPE_ADMISSAO_FORMATO,
    );
    expect(erroDoCampo({ ...VALIDO, dataAdmissao: '01/01/2999' }, 'dataAdmissao')).toBe(
      MENSAGEM_EQUIPE_ADMISSAO_FUTURA,
    );
  });

  it('data de admissão de hoje não é futura', () => {
    expect(erroDoCampo({ ...VALIDO, dataAdmissao: hojeBr() }, 'dataAdmissao')).toBeUndefined();
  });

  it('cargo: um dos seis valores', () => {
    expect(erroDoCampo({ ...VALIDO, cargo: 'DIRETOR' as FormularioFuncionario['cargo'] }, 'cargo')).toBe(
      MENSAGEM_EQUIPE_CARGO_OBRIGATORIO,
    );
    expect(erroDoCampo({ ...VALIDO, cargo: 'ADMINISTRADOR' }, 'cargo')).toBeUndefined();
  });

  it('senha inicial: 8 a 64 caracteres, com maiúscula e número', () => {
    expect(erroDoCampo({ ...VALIDO, senhaInicial: '', confirmarSenha: '' }, 'senhaInicial')).toBe(
      MENSAGEM_EQUIPE_SENHA_OBRIGATORIA,
    );
    expect(erroDoCampo({ ...VALIDO, senhaInicial: 'Ab1' }, 'senhaInicial')).toBe(MENSAGEM_EQUIPE_SENHA_TAMANHO);
    expect(erroDoCampo({ ...VALIDO, senhaInicial: 'a'.repeat(65) }, 'senhaInicial')).toBe(
      MENSAGEM_EQUIPE_SENHA_TAMANHO,
    );
    expect(erroDoCampo({ ...VALIDO, senhaInicial: 'senha1234' }, 'senhaInicial')).toBe(MENSAGEM_EQUIPE_SENHA_FRACA);
    expect(erroDoCampo({ ...VALIDO, senhaInicial: 'Senhaaaaa' }, 'senhaInicial')).toBe(MENSAGEM_EQUIPE_SENHA_FRACA);
  });

  it('senha inicial nos limites exatos (8 e 64) é aceita', () => {
    const oito = { ...VALIDO, senhaInicial: 'Abcdefg1', confirmarSenha: 'Abcdefg1' };
    const sessentaQuatro = { ...VALIDO, senhaInicial: `A1${'b'.repeat(62)}`, confirmarSenha: `A1${'b'.repeat(62)}` };
    expect(ESQUEMA_CADASTRO_FUNCIONARIO.safeParse(oito).success).toBe(true);
    expect(ESQUEMA_CADASTRO_FUNCIONARIO.safeParse(sessentaQuatro).success).toBe(true);
  });

  it('confirmação precisa ser igual à senha', () => {
    expect(erroDoCampo({ ...VALIDO, confirmarSenha: 'Senha1235' }, 'confirmarSenha')).toBe(
      MENSAGEM_EQUIPE_CONFIRMACAO_SENHA,
    );
  });
});

describe('esquema da edição', () => {
  it('não exige CPF nem senha', () => {
    const semCpfNemSenha = { ...VALIDO, cpf: '', senhaInicial: '', confirmarSenha: '' };
    expect(ESQUEMA_EDICAO_FUNCIONARIO.safeParse(semCpfNemSenha).success).toBe(true);
  });

  it('continua exigindo os demais campos', () => {
    const semNome = { ...VALIDO, nome: '', cpf: '', senhaInicial: '', confirmarSenha: '' };
    const resultado = ESQUEMA_EDICAO_FUNCIONARIO.safeParse(semNome);
    expect(resultado.success).toBe(false);
  });
});

describe('campos do formulário', () => {
  it('reconhece os campos que podem receber erro do servidor', () => {
    expect(CAMPOS_FORMULARIO_FUNCIONARIO).toEqual([
      'nome',
      'cpf',
      'matricula',
      'email',
      'dataAdmissao',
      'cargo',
      'senhaInicial',
    ]);
    expect(ehCampoFormularioFuncionario('matricula')).toBe(true);
    expect(ehCampoFormularioFuncionario('idEmpresa')).toBe(false);
  });
});
