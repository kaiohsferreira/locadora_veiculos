import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { UsuarioSessao } from '@/types/autenticacao';
import { CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './AutenticacaoContexto';
import { calcularPermissoes, cargoPermitido, usePermissoes } from './usePermissoes';

function funcionario(cargo?: CargoFuncionario, extras: Partial<UsuarioSessao> = {}): UsuarioSessao {
  return { nome: 'Teste', perfil: 'funcionario', cargo, ...extras };
}

function contextoCom(usuario: UsuarioSessao | null): AutenticacaoContextoValor {
  return {
    usuario,
    autenticado: usuario !== null,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: (perfis) => usuario !== null && perfis.includes(usuario.perfil),
  };
}

describe('calcularPermissoes por cargo', () => {
  it.each(CARGO_FUNCIONARIO)('cargo %s: devolve o próprio cargo', (cargo) => {
    expect(calcularPermissoes(funcionario(cargo)).cargo).toBe(cargo);
  });

  it('só ADMINISTRADOR é administrador', () => {
    for (const cargo of CARGO_FUNCIONARIO) {
      expect(calcularPermissoes(funcionario(cargo)).ehAdministrador).toBe(cargo === 'ADMINISTRADOR');
    }
  });

  it('podeVerValores vale para ADMINISTRADOR e FINANCEIRO, e para mais nenhum cargo', () => {
    for (const cargo of CARGO_FUNCIONARIO) {
      const esperado = cargo === 'ADMINISTRADOR' || cargo === 'FINANCEIRO';
      expect(calcularPermissoes(funcionario(cargo)).podeVerValores).toBe(esperado);
    }
  });

  it('funcionário sem cargo não tem nenhuma permissão de cargo', () => {
    expect(calcularPermissoes(funcionario())).toEqual({
      cargo: null,
      ehAdministrador: false,
      podeVerValores: false,
      ehOperadora: false,
    });
  });
});

describe('calcularPermissoes: locadora e perfis', () => {
  it('ehOperadora só vale para funcionário de empresa operadora', () => {
    expect(calcularPermissoes(funcionario('ADMINISTRADOR', { empresaOperadora: true })).ehOperadora).toBe(true);
    expect(calcularPermissoes(funcionario('ADMINISTRADOR', { empresaOperadora: false })).ehOperadora).toBe(false);
    expect(calcularPermissoes(funcionario('ADMINISTRADOR')).ehOperadora).toBe(false);
  });

  it('cliente não tem cargo nem permissão, mesmo com dado de locadora', () => {
    const cliente: UsuarioSessao = { nome: 'Ana', perfil: 'cliente', empresaOperadora: true };

    expect(calcularPermissoes(cliente)).toEqual({
      cargo: null,
      ehAdministrador: false,
      podeVerValores: false,
      ehOperadora: false,
    });
  });

  it('sem sessão, nada é permitido', () => {
    expect(calcularPermissoes(null)).toEqual({
      cargo: null,
      ehAdministrador: false,
      podeVerValores: false,
      ehOperadora: false,
    });
  });
});

describe('cargoPermitido', () => {
  it('confere o cargo na lista e nega quando não há cargo', () => {
    expect(cargoPermitido('ADMINISTRADOR', ['ADMINISTRADOR'])).toBe(true);
    expect(cargoPermitido('ATENDENTE', ['ADMINISTRADOR'])).toBe(false);
    expect(cargoPermitido(null, ['ADMINISTRADOR', 'ATENDENTE'])).toBe(false);
  });
});

describe('usePermissoes', () => {
  /** Componente que mostra as permissões lidas pelo hook, para renderizar no servidor. */
  function Sonda() {
    const permissoes = usePermissoes();
    return (
      <span>
        {permissoes.cargo ?? 'sem-cargo'}|{String(permissoes.ehAdministrador)}|{String(permissoes.podeVerValores)}
      </span>
    );
  }

  it('lê o cargo da sessão do contexto de autenticação', () => {
    const marcado = renderToStaticMarkup(
      <AutenticacaoContexto.Provider value={contextoCom(funcionario('ADMINISTRADOR'))}>
        <Sonda />
      </AutenticacaoContexto.Provider>,
    );

    expect(marcado).toContain('ADMINISTRADOR|true|true');
  });

  it('sem cargo, devolve as permissões negadas', () => {
    const marcado = renderToStaticMarkup(
      <AutenticacaoContexto.Provider value={contextoCom(funcionario('ATENDENTE'))}>
        <Sonda />
      </AutenticacaoContexto.Provider>,
    );

    expect(marcado).toContain('ATENDENTE|false|false');
  });
});
