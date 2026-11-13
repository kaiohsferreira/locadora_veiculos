import { describe, expect, it } from 'vitest';
import {
  ACESSO_ERRO_CPF_DIGITOS,
  ACESSO_ERRO_CPF_OBRIGATORIO,
  ACESSO_ERRO_SENHA_OBRIGATORIA,
  ACESSO_ERRO_SENHA_TAMANHO,
} from '@/lib/mensagens';
import { esquemaVinculo } from './esquemaVinculo';

/** Primeira mensagem de erro de cada campo, para comparar sem depender da estrutura do Zod. */
function errosPorCampo(valores: unknown): Record<string, string> {
  const resultado = esquemaVinculo.safeParse(valores);
  if (resultado.success) return {};
  const erros: Record<string, string> = {};
  for (const problema of resultado.error.issues) {
    const campo = String(problema.path[0]);
    if (erros[campo] === undefined) erros[campo] = problema.message;
  }
  return erros;
}

describe('esquemaVinculo', () => {
  it('aceita CPF com máscara de 11 dígitos e senha dentro do limite', () => {
    expect(esquemaVinculo.safeParse({ cpf: '529.982.247-25', senha: 'x' }).success).toBe(true);
  });

  it('exige o CPF', () => {
    expect(errosPorCampo({ cpf: '', senha: 'x' }).cpf).toBe(ACESSO_ERRO_CPF_OBRIGATORIO);
  });

  it('exige 11 dígitos no CPF', () => {
    expect(errosPorCampo({ cpf: '529.982.247', senha: 'x' }).cpf).toBe(ACESSO_ERRO_CPF_DIGITOS);
  });

  it('exige a senha', () => {
    expect(errosPorCampo({ cpf: '52998224725', senha: '' }).senha).toBe(ACESSO_ERRO_SENHA_OBRIGATORIA);
  });

  it('aceita senha de 128 caracteres e recusa 129', () => {
    expect(esquemaVinculo.safeParse({ cpf: '52998224725', senha: 'a'.repeat(128) }).success).toBe(true);
    expect(errosPorCampo({ cpf: '52998224725', senha: 'a'.repeat(129) }).senha).toBe(ACESSO_ERRO_SENHA_TAMANHO);
  });
});
