import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { CAMINHOS } from '@/routes/caminhos';
import type { UsuarioSessao } from '@/types/autenticacao';
import TipoContaPagina from './TipoContaPagina';

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

/** Renderiza a tela na rota /cadastro, como a tabela de rotas faz (a tabela em si não é importável em teste). */
function renderizarTipoConta(usuario: UsuarioSessao | null = null): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={contextoCom(usuario)}>
      <MemoryRouter initialEntries={[CAMINHOS.cadastro]}>
        <Routes>
          <Route path={CAMINHOS.cadastro} element={<TipoContaPagina />} />
        </Routes>
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

/** Endereços (href) de todos os links e botões da tela. */
function hrefs(marcado: string): string[] {
  return [...marcado.matchAll(/href="([^"]+)"/g)].map((casamento) => casamento[1]);
}

describe('TipoContaPagina (tela 35): conteúdo', () => {
  it('mostra a pergunta, os dois cartões e os textos do desenho que o sistema cumpre', () => {
    const marcado = renderizarTipoConta();

    expect(marcado).toContain('Como você quer usar a AutoVia?');
    expect(marcado).toContain('Quero alugar ou comprar');
    expect(marcado).toContain('Sou uma locadora');
    expect(marcado).toContain('Criar conta de cliente');
    expect(marcado).toContain('Cadastrar minha locadora');
    expect(marcado).toContain('Reserve e alugue veículos de várias locadoras');
    expect(marcado).toContain('Anuncie a sua frota no catálogo integrado');
  });

  it('tem a nota de "Pessoa física" e "Pessoa jurídica" como etiquetas', () => {
    const marcado = renderizarTipoConta();

    expect(marcado).toContain('Pessoa física');
    expect(marcado).toContain('Pessoa jurídica');
  });

  it('não traz as promessas do desenho que o sistema não cumpre (tarifas, laudo, biometria, 24 horas, telefone e LGPD)', () => {
    const marcado = renderizarTipoConta();

    for (const termo of [
      'tarifas corporativas',
      'laudo',
      'verificados',
      'em tempo real',
      'biométric',
      '24 horas',
      '0800',
      'Criptografia',
      'LGPD',
      'Central 24/7',
    ]) {
      expect(marcado).not.toContain(termo);
    }
  });
});

describe('TipoContaPagina (tela 35): os três destinos', () => {
  it('"Criar conta de cliente" abre o cadastro do cliente (sem mudança)', () => {
    expect(hrefs(renderizarTipoConta())).toContain(CAMINHOS.cadastroDadosPessoais);
  });

  it('"Cadastrar minha locadora" abre a etapa 1 da locadora', () => {
    expect(hrefs(renderizarTipoConta())).toContain(CAMINHOS.cadastroLocadoraEmpresa);
  });

  it('"Fazer login" abre a tela de entrada', () => {
    expect(hrefs(renderizarTipoConta())).toContain(CAMINHOS.entrar);
  });

  it('a volta da trilha aponta para o início', () => {
    expect(hrefs(renderizarTipoConta())).toContain(CAMINHOS.inicio);
  });
});

describe('TipoContaPagina (tela 35): sessão ativa', () => {
  it('quem já tem sessão não vê a escolha do tipo de conta', () => {
    const marcado = renderizarTipoConta({ nome: 'Ana', perfil: 'cliente' });

    expect(marcado).not.toContain('Como você quer usar a AutoVia?');
  });
});

describe('TipoContaPagina (tela 35): círculos de fundo', () => {
  it('a raiz corta os círculos decorativos com overflow-clip (sem criar rolagem lateral no celular)', () => {
    const marcado = renderizarTipoConta();

    expect(marcado.startsWith('<div class="relative w-full overflow-clip py-8">')).toBe(true);
    expect(marcado).not.toContain('relative w-full overflow-hidden py-8');
  });
});
