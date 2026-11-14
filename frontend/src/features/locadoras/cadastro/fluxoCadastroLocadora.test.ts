import { describe, expect, it, vi } from 'vitest';
import type { RequisicaoLogin } from '@/types/autenticacao';
import type { RequisicaoCadastroLocadora, RespostaEmpresa } from '@/types/empresas';
import { executarCadastroLocadora } from './fluxoCadastroLocadora';

const REQUISICAO: RequisicaoCadastroLocadora = {
  razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
  nomeFantasia: 'Rota Sul Locadora',
  cnpj: '11222333000181',
  telefone: '5133429000',
  email: 'contato@rotasul.com.br',
  cep: '90200310',
  logradouro: 'Av. Severo Dullius',
  numero: '1240',
  complemento: null,
  bairro: 'São João',
  cidade: 'Porto Alegre',
  uf: 'RS',
  responsavelNome: 'Marina Costa',
  responsavelCpf: '52998224725',
  responsavelEmail: 'marina@rotasul.com.br',
  senha: 'Rotasul@2025!',
  aceiteTermos: true,
};

const EMPRESA: RespostaEmpresa = {
  idEmpresa: 7,
  razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
  nomeFantasia: 'Rota Sul Locadora',
  cnpj: '11222333000181',
  cnpjStr: '11.222.333/0001-81',
  telefone: '5133429000',
  email: 'contato@rotasul.com.br',
  cep: '90200310',
  logradouro: 'Av. Severo Dullius',
  numero: '1240',
  complemento: null,
  bairro: 'São João',
  cidade: 'Porto Alegre',
  uf: 'RS',
  ativo: true,
  idFuncionarioResponsavel: 21,
  responsavelNome: 'Marina Costa',
  matriculaResponsavel: 'MAT-0001',
  dataCadastroStr: '13/11/2026 10:00',
};

describe('executarCadastroLocadora: um POST e, se criou, o login automático', () => {
  it('cria a conta e abre a sessão com perfil FUNCIONARIO, o CPF e a senha digitada', async () => {
    const cadastrar = vi.fn(async () => EMPRESA);
    const entrar = vi.fn(async (_requisicao: RequisicaoLogin) => ({ token: 't' }));

    const resultado = await executarCadastroLocadora({ cadastrar, entrar }, REQUISICAO);

    expect(cadastrar).toHaveBeenCalledTimes(1);
    expect(cadastrar).toHaveBeenCalledWith(REQUISICAO);
    expect(entrar).toHaveBeenCalledTimes(1);
    expect(entrar).toHaveBeenCalledWith({
      perfil: 'FUNCIONARIO',
      cpf: '52998224725',
      senha: 'Rotasul@2025!',
    });
    expect(resultado).toEqual({ tipo: 'criado', empresa: EMPRESA, loginAutomatico: true });
  });

  it('se o login falha, a conta fica criada e o resultado diz que não há sessão (sem refazer o POST)', async () => {
    const cadastrar = vi.fn(async () => EMPRESA);
    const entrar = vi.fn(async () => {
      throw new Error('empresa inativa');
    });

    const resultado = await executarCadastroLocadora({ cadastrar, entrar }, REQUISICAO);

    expect(cadastrar).toHaveBeenCalledTimes(1);
    expect(resultado).toEqual({ tipo: 'criado', empresa: EMPRESA, loginAutomatico: false });
  });

  it('se o POST falha, o login não é tentado e o erro volta para a tela', async () => {
    const erro = new Error('409');
    const cadastrar = vi.fn(async () => {
      throw erro;
    });
    const entrar = vi.fn(async () => ({}));

    const resultado = await executarCadastroLocadora({ cadastrar, entrar }, REQUISICAO);

    expect(entrar).not.toHaveBeenCalled();
    expect(resultado).toEqual({ tipo: 'erro', erro });
  });

  it('o resultado nunca carrega a senha, nem como campo nem como texto', async () => {
    const resultado = await executarCadastroLocadora(
      { cadastrar: async () => EMPRESA, entrar: async () => ({}) },
      REQUISICAO,
    );

    expect(JSON.stringify(resultado)).not.toContain('Rotasul@2025!');
    expect(resultado).not.toHaveProperty('senha');
  });
});
