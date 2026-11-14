import { describe, expect, it } from 'vitest';
import { converterDadosEmpresa, montarRequisicaoCadastroLocadora } from './conversaoLocadora';
import type { FormularioEmpresa } from './esquemaLocadora';
import type { FormularioResponsavel } from './esquemaResponsavel';

const FORMULARIO: FormularioEmpresa = {
  razaoSocial: '  Rota Sul Locadora de Veículos Ltda  ',
  nomeFantasia: ' Rota Sul Locadora ',
  cnpj: '11.222.333/0001-81',
  telefone: '(51) 3342-9000',
  email: '  Contato@RotaSul.com.BR ',
  cep: '90200-310',
  logradouro: '  Av. Severo Dullius ',
  numero: ' 1240 ',
  complemento: '',
  bairro: ' São João ',
  cidade: ' Porto Alegre ',
  uf: ' rs ',
};

describe('converterDadosEmpresa: normalização do corpo da API', () => {
  it('CNPJ, telefone e CEP vão só com dígitos', () => {
    const corpo = converterDadosEmpresa(FORMULARIO);

    expect(corpo.cnpj).toBe('11222333000181');
    expect(corpo.telefone).toBe('5133429000');
    expect(corpo.cep).toBe('90200310');
  });

  it('e-mail vai em minúsculas e sem espaços nas pontas', () => {
    expect(converterDadosEmpresa(FORMULARIO).email).toBe('contato@rotasul.com.br');
  });

  it('UF vai em maiúsculas', () => {
    expect(converterDadosEmpresa(FORMULARIO).uf).toBe('RS');
  });

  it('textos são aparados nas pontas', () => {
    const corpo = converterDadosEmpresa(FORMULARIO);

    expect(corpo.razaoSocial).toBe('Rota Sul Locadora de Veículos Ltda');
    expect(corpo.nomeFantasia).toBe('Rota Sul Locadora');
    expect(corpo.logradouro).toBe('Av. Severo Dullius');
    expect(corpo.numero).toBe('1240');
    expect(corpo.bairro).toBe('São João');
    expect(corpo.cidade).toBe('Porto Alegre');
  });

  it('complemento vazio ou só com espaços vai como null', () => {
    expect(converterDadosEmpresa(FORMULARIO).complemento).toBeNull();
    expect(converterDadosEmpresa({ ...FORMULARIO, complemento: '   ' }).complemento).toBeNull();
  });

  it('complemento preenchido vai aparado', () => {
    expect(converterDadosEmpresa({ ...FORMULARIO, complemento: ' Loja 02 ' }).complemento).toBe('Loja 02');
  });

  it('não leva senha, responsável nem campos que o backend define sozinho', () => {
    const corpo = converterDadosEmpresa(FORMULARIO);

    expect(Object.keys(corpo).sort()).toEqual(
      [
        'bairro',
        'cep',
        'cidade',
        'cnpj',
        'complemento',
        'email',
        'logradouro',
        'nomeFantasia',
        'numero',
        'razaoSocial',
        'telefone',
        'uf',
      ].sort(),
    );
  });
});

const RESPONSAVEL: FormularioResponsavel = {
  nome: '  Marina Costa ',
  cpf: '529.982.247-25',
  email: '  Marina@RotaSul.com.BR ',
  senha: ' Rotasul@2025! ',
  confirmarSenha: ' Rotasul@2025! ',
  aceiteTermos: true,
};

describe('montarRequisicaoCadastroLocadora: corpo completo do POST /api/empresas', () => {
  it('junta a empresa (etapa 1) com o responsável e a senha (etapa 2)', () => {
    const corpo = montarRequisicaoCadastroLocadora(FORMULARIO, RESPONSAVEL);

    expect(corpo.cnpj).toBe('11222333000181');
    expect(corpo.responsavelNome).toBe('Marina Costa');
    expect(corpo.responsavelCpf).toBe('52998224725');
    expect(corpo.responsavelEmail).toBe('marina@rotasul.com.br');
    expect(corpo.aceiteTermos).toBe(true);
  });

  it('a senha vai como foi digitada (sem aparar espaços)', () => {
    expect(montarRequisicaoCadastroLocadora(FORMULARIO, RESPONSAVEL).senha).toBe(' Rotasul@2025! ');
  });

  it('o corpo tem exatamente os campos do CadastroLocadoraVO (sem confirmação, cargo, ativo nem telefone do responsável)', () => {
    const chaves = Object.keys(montarRequisicaoCadastroLocadora(FORMULARIO, RESPONSAVEL)).sort();

    expect(chaves).toEqual(
      [
        'aceiteTermos',
        'bairro',
        'cep',
        'cidade',
        'cnpj',
        'complemento',
        'email',
        'logradouro',
        'nomeFantasia',
        'numero',
        'razaoSocial',
        'responsavelCpf',
        'responsavelEmail',
        'responsavelNome',
        'senha',
        'telefone',
        'uf',
      ].sort(),
    );
  });
});
