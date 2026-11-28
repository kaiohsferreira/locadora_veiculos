import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AvisoProvider } from '@/components/ui';
import {
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_ERRO_CARREGAMENTO,
  MENSAGEM_VERIFICACAO_VAZIA_DESCRICAO,
  MENSAGEM_VERIFICACAO_VAZIA_TITULO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import {
  useDecidirVerificacao,
  useDocumentosVerificacao,
  useFilaVerificacao,
  useTotalPendentesVerificacao,
  useVerificacaoCliente,
} from '@/features/clientes/hooks/useVerificacaoCadastros';
import VerificacaoCadastrosPagina from './VerificacaoCadastrosPagina';

// Nenhum teste chama a API: as consultas viram dublês, como nas outras telas de página.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
vi.mock('@/features/clientes/hooks/useVerificacaoCadastros', () => ({
  CHAVE_VERIFICACAO: 'verificacao-cadastros',
  useFilaVerificacao: vi.fn(),
  useTotalPendentesVerificacao: vi.fn(),
  useVerificacaoCliente: vi.fn(),
  useDocumentosVerificacao: vi.fn(),
  useDecidirVerificacao: vi.fn(),
}));

type ConsultaFila = ReturnType<typeof useFilaVerificacao>;
type ConsultaTotal = ReturnType<typeof useTotalPendentesVerificacao>;
type ConsultaCliente = ReturnType<typeof useVerificacaoCliente>;
type ConsultaDocumentos = ReturnType<typeof useDocumentosVerificacao>;
type ConsultaDecisao = ReturnType<typeof useDecidirVerificacao>;

/** Consulta sem dados, em repouso (nada carregando, nada com erro). */
function semDados<T>(parcial: Record<string, unknown> = {}): T {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    isPlaceholderData: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as T;
}

/** Renderiza a tela 18 com a fila dada. As outras consultas da tela ficam em repouso. */
function renderizar(fila: Record<string, unknown>): string {
  vi.mocked(useFilaVerificacao).mockReturnValue(semDados<ConsultaFila>(fila));
  vi.mocked(useTotalPendentesVerificacao).mockReturnValue(semDados<ConsultaTotal>());
  vi.mocked(useVerificacaoCliente).mockReturnValue(semDados<ConsultaCliente>());
  vi.mocked(useDocumentosVerificacao).mockReturnValue(semDados<ConsultaDocumentos>());
  vi.mocked(useDecidirVerificacao).mockReturnValue({ mutate: vi.fn(), isPending: false } as unknown as ConsultaDecisao);

  return renderToStaticMarkup(
    <QueryClientProvider client={new QueryClient()}>
      <AvisoProvider>
        <VerificacaoCadastrosPagina />
      </AvisoProvider>
    </QueryClientProvider>,
  );
}

describe('VerificacaoCadastrosPagina (tela 18): estados da fila', () => {
  beforeEach(() => {
    vi.mocked(useFilaVerificacao).mockReset();
  });

  it('fila carregando: tabela com aria-busy, sem a frase de fila vazia e sem aviso de erro', () => {
    const html = renderizar({ data: undefined, isPending: true, isFetching: true });

    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain(MENSAGEM_VERIFICACAO_VAZIA_TITULO);
    expect(html).not.toContain('role="alert"');
    expect(html).not.toContain('Tentar novamente');
  });

  it('fila vazia sem filtro: mostra a frase de lista vazia, sem carregamento e sem erro', () => {
    const html = renderizar({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });

    expect(html).toContain(MENSAGEM_VERIFICACAO_VAZIA_TITULO);
    expect(html).toContain(MENSAGEM_VERIFICACAO_VAZIA_DESCRICAO);
    expect(html).not.toContain('aria-busy="true"');
    expect(html).not.toContain('role="alert"');
  });

  it('erro da fila: aviso com a mensagem amigável e o botão Tentar novamente, sem a tabela', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Erro técnico interno do servidor' });
    const html = renderizar({ data: undefined, isError: true, error: erro });

    expect(html).toContain(MENSAGEM_ERRO_CARREGAMENTO);
    expect(html).toContain(MENSAGEM_ERRO_API_PADRAO);
    expect(html).not.toContain('Erro técnico interno do servidor');
    expect(html).not.toContain(MENSAGEM_VERIFICACAO_VAZIA_TITULO);
    expect(html).not.toContain('aria-busy="true"');
    expect(html).toMatch(/role="alert"[^>]*>(?:(?!<\/div>)[\s\S])*<button[^>]*>(?:(?!<\/button>)[\s\S])*Tentar novamente/);
  });
});
