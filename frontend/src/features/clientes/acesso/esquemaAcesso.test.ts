import { describe, expect, it } from 'vitest';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_LEMBRAR_INVALIDO,
  ACESSO_ERRO_PERFIL_OBRIGATORIO,
  ACESSO_ERRO_SENHA_OBRIGATORIA,
  ACESSO_ERRO_SENHA_TAMANHO,
} from '@/lib/mensagens';
import { esquemaAcesso } from './esquemaAcesso';

const VALORES_VALIDOS = { perfil: 'CLIENTE', cpf: '123.456.789-09', senha: 'senha-de-teste' };

/** Mensagem do primeiro erro do campo, ou undefined quando o campo passou. */
function mensagemDoCampo(valores: unknown, campo: string): string | undefined {
  const resultado = esquemaAcesso.safeParse(valores);
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('esquemaAcesso', () => {
  it('aceita CPF com máscara e perfil e senha preenchidos', () => {
    expect(esquemaAcesso.safeParse(VALORES_VALIDOS).success).toBe(true);
  });

  it('aceita CPF só com dígitos', () => {
    expect(esquemaAcesso.safeParse({ ...VALORES_VALIDOS, cpf: '12345678909' }).success).toBe(true);
  });

  it('não confere o dígito verificador do CPF (o backend também não confere no login)', () => {
    expect(esquemaAcesso.safeParse({ ...VALORES_VALIDOS, cpf: '111.111.111-11' }).success).toBe(true);
  });

  it('exige o CPF quando está vazio', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '' }, 'cpf')).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '.-' }, 'cpf')).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
  });

  it('rejeita CPF com menos de 11 dígitos, mesmo com máscara', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123.456.789-0' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('rejeita CPF com mais de 11 dígitos', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123456789012' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('exige a senha quando está vazia', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, senha: '' }, 'senha')).toBe(ACESSO_ERRO_SENHA_OBRIGATORIA);
  });

  it('aceita a senha com 128 caracteres', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, senha: 'a'.repeat(128) }, 'senha')).toBeUndefined();
  });

  it('rejeita a senha com 129 caracteres', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, senha: 'a'.repeat(129) }, 'senha')).toBe(
      ACESSO_ERRO_SENHA_TAMANHO,
    );
  });

  it('exige o perfil quando ele está ausente', () => {
    const semPerfil: Record<string, string> = { cpf: VALORES_VALIDOS.cpf, senha: VALORES_VALIDOS.senha };
    expect(mensagemDoCampo(semPerfil, 'perfil')).toBe(ACESSO_ERRO_PERFIL_OBRIGATORIO);
  });

  it('rejeita perfil fora de CLIENTE e FUNCIONARIO', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, perfil: 'ADMIN' }, 'perfil')).toBe(
      ACESSO_ERRO_PERFIL_OBRIGATORIO,
    );
  });

  it('aceita o perfil FUNCIONARIO', () => {
    expect(esquemaAcesso.safeParse({ ...VALORES_VALIDOS, perfil: 'FUNCIONARIO' }).success).toBe(true);
  });

  it('aceita senha só com espaços (o backend aceita de 1 a 128 caracteres)', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, senha: '   ' }, 'senha')).toBeUndefined();
  });

  it('CPF com letras e poucos dígitos conta só os dígitos', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123abc45678' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('CPF só com letras é tratado como vazio', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: 'abc.def.ghi-jk' }, 'cpf')).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
  });

  it('perfil em minúsculas é rejeitado (o contrato usa CLIENTE e FUNCIONARIO)', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, perfil: 'cliente' }, 'perfil')).toBe(
      ACESSO_ERRO_PERFIL_OBRIGATORIO,
    );
  });

  describe('lembrar de mim', () => {
    it('ausente vale false', () => {
      const resultado = esquemaAcesso.safeParse(VALORES_VALIDOS);
      expect(resultado.success).toBe(true);
      if (resultado.success) expect(resultado.data.lembrar).toBe(false);
    });

    it('true é preservado', () => {
      const resultado = esquemaAcesso.safeParse({ ...VALORES_VALIDOS, lembrar: true });
      expect(resultado.success).toBe(true);
      if (resultado.success) expect(resultado.data.lembrar).toBe(true);
    });

    it('false explícito é preservado', () => {
      const resultado = esquemaAcesso.safeParse({ ...VALORES_VALIDOS, lembrar: false });
      expect(resultado.success).toBe(true);
      if (resultado.success) expect(resultado.data.lembrar).toBe(false);
    });

    it('valor que não é booleano é rejeitado', () => {
      expect(mensagemDoCampo({ ...VALORES_VALIDOS, lembrar: 'sim' }, 'lembrar')).toBe(ACESSO_ERRO_LEMBRAR_INVALIDO);
      expect(mensagemDoCampo({ ...VALORES_VALIDOS, lembrar: 1 }, 'lembrar')).toBe(ACESSO_ERRO_LEMBRAR_INVALIDO);
      expect(mensagemDoCampo({ ...VALORES_VALIDOS, lembrar: null }, 'lembrar')).toBe(ACESSO_ERRO_LEMBRAR_INVALIDO);
    });
  });
});
