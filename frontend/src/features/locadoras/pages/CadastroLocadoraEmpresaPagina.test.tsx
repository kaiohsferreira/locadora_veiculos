import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ContextoCadastroLocadora, type ContextoCadastroLocadoraValor } from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import type { FormularioEmpresa } from '@/features/locadoras/cadastro/esquemaLocadora';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { CAMINHOS } from '@/routes/caminhos';
import CadastroLocadoraEmpresaPagina from './CadastroLocadoraEmpresaPagina';

// A tela importa o hook de verificação (e, com ele, o cliente HTTP, que lê o navegador ao carregar).
// Nenhum teste aqui chama a API: o cliente é trocado por um dublê, como em api/clientes.test.ts.
vi.mock('@/api/client', () => ({
  api: { post: vi.fn() },
}));

function autenticacaoSemSessao(): AutenticacaoContextoValor {
  return {
    usuario: null,
    autenticado: false,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => false,
  };
}

/** Renderiza a etapa 1 com o provedor de dados e o cliente de consultas, como a rota-pai faz. */
function renderizarEmpresa(dadosEmpresa: FormularioEmpresa | null = null): string {
  const contexto: ContextoCadastroLocadoraValor = {
    dadosEmpresa,
    salvarDadosEmpresa: () => undefined,
    dadosResponsavel: null,
    salvarDadosResponsavel: () => undefined,
    errosServidor: [],
    definirErrosServidor: () => undefined,
    removerErrosServidor: () => undefined,
    conflitos: {},
    registrarConflitos: () => undefined,
    resultado: null,
    salvarResultado: () => undefined,
  };
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={autenticacaoSemSessao()}>
      <QueryClientProvider client={new QueryClient()}>
        <ContextoCadastroLocadora.Provider value={contexto}>
          <MemoryRouter initialEntries={[CAMINHOS.cadastroLocadoraEmpresa]}>
            <Routes>
              <Route path={CAMINHOS.cadastroLocadoraEmpresa} element={<CadastroLocadoraEmpresaPagina />} />
            </Routes>
          </MemoryRouter>
        </ContextoCadastroLocadora.Provider>
      </QueryClientProvider>
    </AutenticacaoContexto.Provider>,
  );
}

describe('CadastroLocadoraEmpresaPagina (tela 36): conteúdo', () => {
  it('mostra o título, o subtítulo e a etapa 1 do indicador', () => {
    const marcado = renderizarEmpresa();

    expect(marcado).toContain('Cadastre a sua locadora');
    expect(marcado).toContain('Informe os dados da empresa. Eles aparecem para os clientes no catálogo.');
    expect(marcado).toContain('aria-current="step"');
    expect(marcado).toContain('Em preenchimento');
    expect(marcado).toContain('ETAPA 1 DE 3');
  });

  it('tem os campos do contrato com os rótulos do desenho', () => {
    const marcado = renderizarEmpresa();

    for (const rotulo of [
      'Razão social',
      'Nome fantasia',
      'CNPJ',
      'Telefone comercial',
      'E-mail comercial',
      'Endereço da sede operacional',
      'CEP',
      'Endereço',
      'Número',
      'Complemento',
      'Bairro',
      'Cidade',
      'UF',
    ]) {
      expect(marcado).toContain(rotulo);
    }
  });

  it('o nome fantasia traz a dica do desenho ("Nome exibido no catálogo")', () => {
    expect(renderizarEmpresa()).toContain('Nome exibido no catálogo');
  });

  it('o botão de continuar e o link de login estão na tela', () => {
    const marcado = renderizarEmpresa();

    expect(marcado).toContain('Continuar para Responsável');
    expect(marcado).toContain('Fazer login');
    expect(marcado).toContain(`href="${CAMINHOS.entrar}"`);
  });

  it('não pede a senha nesta etapa e avisa que ela é criada na próxima', () => {
    const marcado = renderizarEmpresa();

    expect(marcado).toContain('A senha é criada na próxima etapa.');
    expect(marcado).not.toContain('type="password"');
  });

  it('não traz a promessa de validação na Receita nem a de biometria do desenho', () => {
    const marcado = renderizarEmpresa();

    expect(marcado).not.toContain('Receita');
    expect(marcado).not.toContain('biométric');
    expect(marcado).not.toContain('criptografia de ponta a ponta');
  });
});

describe('CadastroLocadoraEmpresaPagina (tela 36): círculos de fundo', () => {
  it('a raiz corta os círculos decorativos com overflow-clip (sem criar rolagem lateral no celular)', () => {
    const marcado = renderizarEmpresa();

    expect(marcado.startsWith('<div class="relative w-full overflow-clip py-8">')).toBe(true);
    expect(marcado).not.toContain('relative w-full overflow-hidden py-8');
  });
});
