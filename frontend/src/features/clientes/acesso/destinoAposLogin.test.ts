import { describe, expect, it } from 'vitest';
import { CAMINHOS } from '@/routes/caminhos';
import { perfilInicialPara, resolverDestinoAposLogin } from './destinoAposLogin';

describe('resolverDestinoAposLogin', () => {
  it('leva o cliente sem retorno para o início', () => {
    expect(resolverDestinoAposLogin('cliente', undefined)).toBe(CAMINHOS.inicio);
  });

  it('leva o funcionário sem retorno para o back office', () => {
    expect(resolverDestinoAposLogin('funcionario', undefined)).toBe(CAMINHOS.backOffice);
  });

  it('volta para a página pedida pelo cliente', () => {
    const estado = { de: CAMINHOS.minhasReservas };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.minhasReservas);
  });

  it('volta para a página pedida pelo funcionário dentro do back office', () => {
    const estado = { de: CAMINHOS.verificacaoCadastros };
    expect(resolverDestinoAposLogin('funcionario', estado)).toBe(CAMINHOS.verificacaoCadastros);
  });

  it('manda o cliente que veio do back office para o início, evitando acesso negado', () => {
    const estado = { de: CAMINHOS.frota };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.inicio);
  });

  it('manda o cliente para o início também quando o retorno é "/back-office-x"', () => {
    const estado = { de: '/back-office-x' };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.inicio);
  });

  it('rejeita retorno externo "//site" e usa o padrão do perfil', () => {
    const estado = { de: '//site-externo.com' };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.inicio);
    expect(resolverDestinoAposLogin('funcionario', estado)).toBe(CAMINHOS.backOffice);
  });

  it('rejeita URL absoluta como retorno', () => {
    const estado = { de: 'https://site-externo.com/pagina' };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.inicio);
  });

  it('rejeita "/entrar" como retorno e usa o padrão do perfil (não volta ao login)', () => {
    const estado = { de: CAMINHOS.entrar };
    expect(resolverDestinoAposLogin('cliente', estado)).toBe(CAMINHOS.inicio);
    expect(resolverDestinoAposLogin('funcionario', estado)).toBe(CAMINHOS.backOffice);
  });

  it('ignora um estado sem o campo de retorno', () => {
    expect(resolverDestinoAposLogin('funcionario', { outra: '/x' })).toBe(CAMINHOS.backOffice);
  });
});

describe('perfilInicialPara', () => {
  it('marca Cliente quando não há retorno', () => {
    expect(perfilInicialPara(undefined)).toBe('CLIENTE');
  });

  it('marca Equipe quando o retorno é do back office', () => {
    expect(perfilInicialPara({ de: CAMINHOS.backOffice })).toBe('FUNCIONARIO');
    expect(perfilInicialPara({ de: CAMINHOS.locacoes })).toBe('FUNCIONARIO');
  });

  it('mantém Cliente quando o retorno é da área pública, externo ou a própria tela de login', () => {
    expect(perfilInicialPara({ de: CAMINHOS.minhasLocacoes })).toBe('CLIENTE');
    expect(perfilInicialPara({ de: '//site-externo.com' })).toBe('CLIENTE');
    expect(perfilInicialPara({ de: CAMINHOS.entrar })).toBe('CLIENTE');
  });
});
