import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { CadastroContexto, type CadastroContextoValor } from '@/features/clientes/cadastro/ContextoCadastro';
import CadastroDadosPessoaisPagina from '@/features/clientes/pages/CadastroDadosPessoaisPagina';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';

// O cliente HTTP lê o window ao carregar: a página é testada sem rede e sem navegador.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

/** Renderiza a etapa 1 sem sessão, com os contextos que a página usa (como o CadastroDocumentosPagina.test). */
function renderizarEtapa1(): string {
  const contextoAutenticacao: AutenticacaoContextoValor = {
    usuario: null,
    autenticado: false,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => false,
  };
  const contextoCadastro: CadastroContextoValor = {
    dadosPessoais: null,
    documentos: null,
    veioDoGoogle: false,
    arquivosDocumentos: {},
    errosServidor: [],
    salvarDadosPessoais: () => undefined,
    salvarDocumentos: () => undefined,
    salvarArquivosDocumentos: () => undefined,
    definirErrosServidor: () => undefined,
    removerErrosServidor: () => undefined,
  };
  return renderToStaticMarkup(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <AvisoProvider>
          <AutenticacaoContexto.Provider value={contextoAutenticacao}>
            <CadastroContexto.Provider value={contextoCadastro}>
              <CadastroDadosPessoaisPagina />
            </CadastroContexto.Provider>
          </AutenticacaoContexto.Provider>
        </AvisoProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CadastroDadosPessoaisPagina (etapa 1 do cliente): círculos de fundo', () => {
  it('a raiz corta os círculos decorativos com overflow-clip (sem criar rolagem lateral no celular)', () => {
    const marcado = renderizarEtapa1();

    expect(marcado.startsWith('<div class="relative w-full overflow-clip py-8">')).toBe(true);
    expect(marcado).not.toContain('relative w-full overflow-hidden py-8');
  });
});
