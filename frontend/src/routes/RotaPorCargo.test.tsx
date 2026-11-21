import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type { UsuarioSessao } from '@/types/autenticacao';
import type { CargoFuncionario } from '@/types/enums';
import { CARGOS_VER_PAINEL_FINANCEIRO } from '@/features/financeiro/permissoes';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './autenticacao/AutenticacaoContexto';
import { RotaPorCargo } from './RotaPorCargo';

const MENSAGEM_CONTEUDO = 'Conteúdo protegido';
const TITULO_NEGADO = 'Acesso não permitido';

function contextoCom(usuario: UsuarioSessao | null, autenticacaoIgnorada = false): AutenticacaoContextoValor {
  return {
    usuario,
    autenticado: usuario !== null,
    autenticacaoIgnorada,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: (perfis) => usuario !== null && perfis.includes(usuario.perfil),
  };
}

function renderizarRota(
  usuario: UsuarioSessao | null,
  cargosPermitidos: readonly CargoFuncionario[],
  autenticacaoIgnorada = false,
): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={contextoCom(usuario, autenticacaoIgnorada)}>
      <MemoryRouter initialEntries={['/back-office/equipe']}>
        <Routes>
          <Route element={<RotaPorCargo cargosPermitidos={cargosPermitidos} />}>
            <Route path="/back-office/equipe" element={<p>{MENSAGEM_CONTEUDO}</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

describe('RotaPorCargo: permite', () => {
  it('abre as rotas filhas para o cargo permitido', () => {
    const marcado = renderizarRota({ nome: 'Marina', perfil: 'funcionario', cargo: 'ADMINISTRADOR' }, [
      'ADMINISTRADOR',
    ]);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
  });

  it('abre para qualquer cargo que esteja na lista', () => {
    const marcado = renderizarRota({ nome: 'Bia', perfil: 'funcionario', cargo: 'FINANCEIRO' }, [
      'ADMINISTRADOR',
      'FINANCEIRO',
    ]);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
  });
});

describe('RotaPorCargo: nega', () => {
  it('mostra acesso não permitido para cargo fora da lista', () => {
    const marcado = renderizarRota({ nome: 'Caio', perfil: 'funcionario', cargo: 'ATENDENTE' }, ['ADMINISTRADOR']);

    expect(marcado).toContain(TITULO_NEGADO);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
  });

  it('nega funcionário sem cargo', () => {
    const marcado = renderizarRota({ nome: 'Dani', perfil: 'funcionario' }, ['ADMINISTRADOR']);

    expect(marcado).toContain(TITULO_NEGADO);
  });

  it('nega sem sessão', () => {
    const marcado = renderizarRota(null, ['ADMINISTRADOR']);

    expect(marcado).toContain(TITULO_NEGADO);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
  });
});

describe('RotaPorCargo: modo de desenvolvimento sem autenticação (VITE_IGNORAR_AUTENTICACAO)', () => {
  it('libera a rota sem sessão, como o RotaProtegida', () => {
    const marcado = renderizarRota(null, ['ADMINISTRADOR'], true);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
  });

  it('libera a rota para funcionário sem cargo quando a autenticação está ignorada', () => {
    const marcado = renderizarRota({ nome: 'Dani', perfil: 'funcionario' }, ['ADMINISTRADOR'], true);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
  });

  it('com a autenticação ativa, o cargo continua valendo', () => {
    const marcado = renderizarRota({ nome: 'Caio', perfil: 'funcionario', cargo: 'ATENDENTE' }, ['ADMINISTRADOR'], false);

    expect(marcado).toContain(TITULO_NEGADO);
  });
});

describe('RotaPorCargo: painel financeiro (tela 09)', () => {
  it('ATENDENTE não abre a tela 09', () => {
    const marcado = renderizarRota(
      { nome: 'Caio', perfil: 'funcionario', cargo: 'ATENDENTE' },
      CARGOS_VER_PAINEL_FINANCEIRO,
    );

    expect(marcado).toContain(TITULO_NEGADO);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
  });

  it('FINANCEIRO e ADMINISTRADOR abrem a tela 09', () => {
    expect(
      renderizarRota({ nome: 'Bia', perfil: 'funcionario', cargo: 'FINANCEIRO' }, CARGOS_VER_PAINEL_FINANCEIRO),
    ).toContain(MENSAGEM_CONTEUDO);
    expect(
      renderizarRota({ nome: 'Marina', perfil: 'funcionario', cargo: 'ADMINISTRADOR' }, CARGOS_VER_PAINEL_FINANCEIRO),
    ).toContain(MENSAGEM_CONTEUDO);
  });
});
