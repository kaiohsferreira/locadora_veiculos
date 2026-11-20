import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type { PerfilUsuario, UsuarioSessao } from '@/types/autenticacao';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from './autenticacao/AutenticacaoContexto';
import { CAMINHOS } from './caminhos';
import { RotaProtegida } from './RotaProtegida';

const MENSAGEM_CONTEUDO = 'Conteúdo protegido';
const TITULO_NEGADO = 'Acesso não permitido';

// O renderToStaticMarkup não executa efeitos, e o Navigate redireciona dentro de um efeito.
// Por isso o Navigate é trocado por uma marca que expõe o destino, o estado e o replace enviados pelo componente.
vi.mock('react-router-dom', async (importOriginal) => {
  const original = await importOriginal<typeof import('react-router-dom')>();
  const { createElement } = await import('react');
  return {
    ...original,
    Navigate: ({ to, replace, state }: { to: string; replace?: boolean; state?: { de?: string } | null }) =>
      createElement('span', {
        'data-navegacao': to,
        'data-substituir': String(replace === true),
        'data-de': state?.de ?? '',
      }),
  };
});

const CLIENTE: UsuarioSessao = { nome: 'Ana', perfil: 'cliente' };
const FUNCIONARIO: UsuarioSessao = { nome: 'Marina', perfil: 'funcionario', cargo: 'ADMINISTRADOR' };

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

function renderizarRota(
  usuario: UsuarioSessao | null,
  perfisPermitidos: readonly PerfilUsuario[],
  caminhoAcessado: string,
  caminhoDaRota: string,
): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={contextoCom(usuario)}>
      <MemoryRouter initialEntries={[caminhoAcessado]}>
        <Routes>
          <Route element={<RotaProtegida perfisPermitidos={perfisPermitidos} />}>
            <Route path={caminhoDaRota} element={<p>{MENSAGEM_CONTEUDO}</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

describe('RotaProtegida: sem sessão', () => {
  it('manda para o login na rota de equipe, guardando a página de origem', () => {
    const marcado = renderizarRota(null, ['funcionario'], CAMINHOS.equipe, CAMINHOS.equipe);

    expect(marcado).toContain(`data-navegacao="${CAMINHOS.entrar}"`);
    expect(marcado).toContain(`data-de="${CAMINHOS.equipe}"`);
    expect(marcado).toContain('data-substituir="true"');
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
  });

  it('manda para o login na rota de cliente, guardando a página de origem', () => {
    const marcado = renderizarRota(null, ['cliente'], CAMINHOS.minhasReservas, CAMINHOS.minhasReservas);

    expect(marcado).toContain(`data-navegacao="${CAMINHOS.entrar}"`);
    expect(marcado).toContain(`data-de="${CAMINHOS.minhasReservas}"`);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
  });
});

describe('RotaProtegida: sessão de cliente em rota de equipe', () => {
  it('mostra acesso não permitido e não o conteúdo', () => {
    const marcado = renderizarRota(CLIENTE, ['funcionario'], CAMINHOS.equipe, CAMINHOS.equipe);

    expect(marcado).toContain(TITULO_NEGADO);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain('data-navegacao');
  });
});

describe('RotaProtegida: sessão de funcionário em rota de equipe', () => {
  it('abre a rota de equipe para o funcionário', () => {
    const marcado = renderizarRota(FUNCIONARIO, ['funcionario'], CAMINHOS.equipe, CAMINHOS.equipe);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
    expect(marcado).not.toContain('data-navegacao');
  });
});

describe('RotaProtegida: sessão de funcionário em rota de cliente', () => {
  it('mostra acesso não permitido e não o conteúdo', () => {
    const marcado = renderizarRota(FUNCIONARIO, ['cliente'], CAMINHOS.minhasReservas, CAMINHOS.minhasReservas);

    expect(marcado).toContain(TITULO_NEGADO);
    expect(marcado).not.toContain(MENSAGEM_CONTEUDO);
  });
});

describe('RotaProtegida: sessão de cliente em rota de cliente', () => {
  it('abre a rota de cliente para o cliente', () => {
    const marcado = renderizarRota(CLIENTE, ['cliente'], CAMINHOS.minhasReservas, CAMINHOS.minhasReservas);

    expect(marcado).toContain(MENSAGEM_CONTEUDO);
    expect(marcado).not.toContain(TITULO_NEGADO);
  });
});
