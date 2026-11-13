import { describe, expect, it } from 'vitest';
import { converterFormularioParaRequisicao } from '@/features/clientes/cadastro/conversaoCadastro';
import type { FormularioCadastro } from '@/features/clientes/cadastro/esquemaCadastro';

/** Formulário preenchido, com máscaras e espaços como o usuário digita. */
const FORMULARIO: FormularioCadastro = {
  nome: '  Mariana Ribeiro de Souza  ',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: '  Mariana.Souza@Email.COM ',
  telefone: '(11) 98765-4321',
  cnhNumero: '123 456 789 01',
  cnhCategoria: 'AB',
  cnhValidade: '2028-05-10',
  cep: '01310-100',
  logradouro: '  Avenida Paulista  ',
  numero: '1000',
  complemento: 'Apto 12',
  bairro: 'Bela Vista',
  cidade: 'São Paulo',
  uf: 'sp',
  senha: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

describe('converterFormularioParaRequisicao: máscaras e textos', () => {
  it('remove a máscara de CPF, telefone, CNH e CEP', () => {
    const requisicao = converterFormularioParaRequisicao(FORMULARIO);
    expect(requisicao.cpf).toBe('52998224725');
    expect(requisicao.telefone).toBe('11987654321');
    expect(requisicao.cnhNumero).toBe('12345678901');
    expect(requisicao.cep).toBe('01310100');
  });

  it('tira os espaços das bordas do nome e dos textos do endereço', () => {
    const requisicao = converterFormularioParaRequisicao(FORMULARIO);
    expect(requisicao.nome).toBe('Mariana Ribeiro de Souza');
    expect(requisicao.logradouro).toBe('Avenida Paulista');
  });

  it('grava o e-mail com espaços removidos e em minúsculas', () => {
    expect(converterFormularioParaRequisicao(FORMULARIO).email).toBe('mariana.souza@email.com');
  });

  it('grava a UF em maiúsculas', () => {
    expect(converterFormularioParaRequisicao(FORMULARIO).uf).toBe('SP');
  });

  it('mantém as datas em AAAA-MM-DD e a categoria da CNH', () => {
    const requisicao = converterFormularioParaRequisicao(FORMULARIO);
    expect(requisicao.dataNascimento).toBe('1992-08-14');
    expect(requisicao.cnhValidade).toBe('2028-05-10');
    expect(requisicao.cnhCategoria).toBe('AB');
  });

  it('não altera a senha (nem espaços nas bordas)', () => {
    const requisicao = converterFormularioParaRequisicao({ ...FORMULARIO, senha: ' Senha 1234 ' });
    expect(requisicao.senha).toBe(' Senha 1234 ');
  });
});

describe('converterFormularioParaRequisicao: opcionais vazios', () => {
  it('vira null em cada campo de endereço vazio ou só com espaços', () => {
    const requisicao = converterFormularioParaRequisicao({
      ...FORMULARIO,
      cep: '',
      logradouro: '   ',
      numero: '',
      complemento: '',
      bairro: '',
      cidade: '',
      uf: '',
    });
    expect(requisicao.cep).toBeNull();
    expect(requisicao.logradouro).toBeNull();
    expect(requisicao.numero).toBeNull();
    expect(requisicao.complemento).toBeNull();
    expect(requisicao.bairro).toBeNull();
    expect(requisicao.cidade).toBeNull();
    expect(requisicao.uf).toBeNull();
  });
});

describe('converterFormularioParaRequisicao: categoria vazia', () => {
  it('lança erro quando a categoria da CNH está vazia (a validação impede esse caso)', () => {
    expect(() => converterFormularioParaRequisicao({ ...FORMULARIO, cnhCategoria: '' })).toThrow();
  });
});

describe('converterFormularioParaRequisicao: confirmação de senha', () => {
  it('não envia confirmarSenha no payload', () => {
    const requisicao = converterFormularioParaRequisicao(FORMULARIO);
    expect(Object.keys(requisicao)).not.toContain('confirmarSenha');
    expect(JSON.stringify(requisicao)).not.toContain('confirmarSenha');
  });

  it('envia só os campos do ClienteVO', () => {
    const chaves = Object.keys(converterFormularioParaRequisicao(FORMULARIO)).sort();
    expect(chaves).toEqual(
      [
        'bairro',
        'cep',
        'cidade',
        'cnhCategoria',
        'cnhNumero',
        'cnhValidade',
        'complemento',
        'cpf',
        'dataNascimento',
        'email',
        'logradouro',
        'nome',
        'numero',
        'senha',
        'telefone',
        'uf',
      ].sort(),
    );
  });
});
