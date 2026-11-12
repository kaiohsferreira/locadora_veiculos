import { describe, expect, it } from 'vitest';
import { LIMITE_PREENCHIMENTO_CADASTRO, lerPreenchimentoCadastro } from './preenchimentoCadastro';

describe('lerPreenchimentoCadastro', () => {
  it('lê nome e e-mail válidos', () => {
    expect(lerPreenchimentoCadastro({ nome: 'Ana Souza', email: 'ana@exemplo.com' })).toEqual({
      nome: 'Ana Souza',
      email: 'ana@exemplo.com',
    });
  });

  it('ignora campos extras do valor', () => {
    expect(lerPreenchimentoCadastro({ nome: 'Ana', email: 'a@e.com', cpf: '123', senha: 'x' })).toEqual({
      nome: 'Ana',
      email: 'a@e.com',
    });
  });

  it('ausente, nulo ou de outro tipo vira vazio', () => {
    expect(lerPreenchimentoCadastro(undefined)).toEqual({ nome: '', email: '' });
    expect(lerPreenchimentoCadastro(null)).toEqual({ nome: '', email: '' });
    expect(lerPreenchimentoCadastro('texto')).toEqual({ nome: '', email: '' });
    expect(lerPreenchimentoCadastro({})).toEqual({ nome: '', email: '' });
  });

  it('o formato antigo (preenchimento dentro de location.state) não é mais lido', () => {
    expect(lerPreenchimentoCadastro({ preenchimento: { nome: 'Ana', email: 'a@e.com' } })).toEqual({
      nome: '',
      email: '',
    });
  });

  it('só aceita strings: número, objeto ou nulo no campo viram vazio', () => {
    expect(lerPreenchimentoCadastro({ nome: 42, email: { endereco: 'a@e.com' } })).toEqual({ nome: '', email: '' });
    expect(lerPreenchimentoCadastro({ nome: null, email: 'a@e.com' })).toEqual({ nome: '', email: 'a@e.com' });
  });

  it('corta nome e e-mail longos no limite de 120 caracteres', () => {
    const longo = 'x'.repeat(200);
    const resultado = lerPreenchimentoCadastro({ nome: longo, email: longo });
    expect(LIMITE_PREENCHIMENTO_CADASTRO).toBe(120);
    expect(resultado.nome).toHaveLength(120);
    expect(resultado.email).toHaveLength(120);
  });

  it('mantém texto com exatamente 120 caracteres sem cortar', () => {
    const exato = 'y'.repeat(120);
    expect(lerPreenchimentoCadastro({ nome: exato }).nome).toBe(exato);
  });
});
