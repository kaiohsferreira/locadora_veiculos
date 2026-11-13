import { describe, expect, it } from 'vitest';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_PERFIL_OBRIGATORIO,
} from '@/lib/mensagens';
import { esquemaSolicitacao } from './esquemaSolicitacao';

const VALORES_VALIDOS = { perfil: 'CLIENTE', cpf: '529.982.247-25' };

/** Mensagem do primeiro erro do campo, ou undefined quando o campo passou. */
function mensagemDoCampo(valores: unknown, campo: string): string | undefined {
  const resultado = esquemaSolicitacao.safeParse(valores);
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('esquemaSolicitacao', () => {
  it('aceita CPF com máscara', () => {
    expect(esquemaSolicitacao.safeParse(VALORES_VALIDOS).success).toBe(true);
  });

  it('aceita CPF só com dígitos', () => {
    expect(esquemaSolicitacao.safeParse({ ...VALORES_VALIDOS, cpf: '52998224725' }).success).toBe(true);
  });

  it('não confere o dígito verificador (igual ao login)', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '111.111.111-11' }, 'cpf')).toBeUndefined();
  });

  it('exige o CPF quando está vazio', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '' }, 'cpf')).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '.-' }, 'cpf')).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
  });

  it('rejeita CPF curto, com ou sem máscara', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '529.982.247-2' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('rejeita CPF com mais de 11 dígitos', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, cpf: '123456789012' }, 'cpf')).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('exige o perfil quando ele está ausente', () => {
    expect(mensagemDoCampo({ cpf: VALORES_VALIDOS.cpf }, 'perfil')).toBe(ACESSO_ERRO_PERFIL_OBRIGATORIO);
  });

  it('rejeita perfil fora de CLIENTE e FUNCIONARIO', () => {
    expect(mensagemDoCampo({ ...VALORES_VALIDOS, perfil: 'ADMIN' }, 'perfil')).toBe(
      ACESSO_ERRO_PERFIL_OBRIGATORIO,
    );
  });

  it('aceita o perfil FUNCIONARIO', () => {
    expect(esquemaSolicitacao.safeParse({ ...VALORES_VALIDOS, perfil: 'FUNCIONARIO' }).success).toBe(true);
  });
});
