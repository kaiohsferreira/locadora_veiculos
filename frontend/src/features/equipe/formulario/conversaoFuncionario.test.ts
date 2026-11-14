import { describe, expect, it } from 'vitest';
import type { FuncionarioEquipe } from '@/types/equipe';
import {
  montarRequisicaoCadastro,
  montarRequisicaoEdicao,
  valoresIniciaisCadastro,
  valoresIniciaisEdicao,
} from './conversaoFuncionario';
import type { FormularioFuncionario } from './esquemaFuncionario';

const FORMULARIO: FormularioFuncionario = {
  nome: '  Carlos Eduardo Silveira  ',
  cpf: '529.982.247-25',
  matricula: ' MAT-0200 ',
  email: ' carlos@rotasul.com.br ',
  dataAdmissao: '23/02/2025',
  cargo: 'GESTOR_FROTA',
  senhaInicial: ' Senha1234 ',
  confirmarSenha: ' Senha1234 ',
};

const FUNCIONARIO: FuncionarioEquipe = {
  idFuncionario: 7,
  nome: 'Juliana Mendes',
  cpfMascarado: '512.***.***-98',
  matricula: 'MAT-0103',
  email: null,
  cargo: 'FINANCEIRO',
  ativo: true,
  dataAdmissao: '2024-11-04',
  dataAdmissaoStr: '04/11/2024',
  ehVoce: false,
};

describe('montarRequisicaoCadastro', () => {
  it('leva CPF só com dígitos, data em AAAA-MM-DD e os textos aparados', () => {
    expect(montarRequisicaoCadastro(FORMULARIO)).toEqual({
      nome: 'Carlos Eduardo Silveira',
      cpf: '52998224725',
      matricula: 'MAT-0200',
      email: 'carlos@rotasul.com.br',
      dataAdmissao: '2025-02-23',
      cargo: 'GESTOR_FROTA',
      senhaInicial: ' Senha1234 ',
    });
  });

  it('não leva a confirmação de senha nem empresa ou id', () => {
    const requisicao = montarRequisicaoCadastro(FORMULARIO);
    expect(requisicao).not.toHaveProperty('confirmarSenha');
    expect(requisicao).not.toHaveProperty('idEmpresa');
    expect(requisicao).not.toHaveProperty('empresa');
  });

  it('mantém a senha como digitada (sem aparar)', () => {
    expect(montarRequisicaoCadastro(FORMULARIO).senhaInicial).toBe(' Senha1234 ');
  });
});

describe('montarRequisicaoEdicao', () => {
  it('leva só nome, matrícula, e-mail, admissão e cargo', () => {
    const requisicao = montarRequisicaoEdicao(FORMULARIO);
    expect(Object.keys(requisicao).sort()).toEqual(['cargo', 'dataAdmissao', 'email', 'matricula', 'nome']);
    expect(requisicao).toEqual({
      nome: 'Carlos Eduardo Silveira',
      matricula: 'MAT-0200',
      email: 'carlos@rotasul.com.br',
      dataAdmissao: '2025-02-23',
      cargo: 'GESTOR_FROTA',
    });
  });

  it('nunca leva CPF nem senha', () => {
    const requisicao = montarRequisicaoEdicao(FORMULARIO);
    expect(requisicao).not.toHaveProperty('cpf');
    expect(requisicao).not.toHaveProperty('senhaInicial');
  });
});

describe('valoresIniciaisEdicao', () => {
  it('converte a data ISO para DD/MM/AAAA e deixa e-mail nulo como texto vazio', () => {
    const valores = valoresIniciaisEdicao(FUNCIONARIO);
    expect(valores).toMatchObject({
      nome: 'Juliana Mendes',
      matricula: 'MAT-0103',
      email: '',
      dataAdmissao: '04/11/2024',
      cargo: 'FINANCEIRO',
    });
  });

  it('não preenche CPF nem senhas (não são editados aqui)', () => {
    const valores = valoresIniciaisEdicao(FUNCIONARIO);
    expect(valores.cpf).toBe('');
    expect(valores.senhaInicial).toBe('');
    expect(valores.confirmarSenha).toBe('');
  });
});

describe('valoresIniciaisCadastro', () => {
  it('começa com cargo Atendente e tudo vazio', () => {
    expect(valoresIniciaisCadastro()).toEqual({
      nome: '',
      cpf: '',
      matricula: '',
      email: '',
      dataAdmissao: '',
      cargo: 'ATENDENTE',
      senhaInicial: '',
      confirmarSenha: '',
    });
  });
});
