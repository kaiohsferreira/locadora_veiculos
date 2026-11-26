import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { SecaoUltimasLocacoes } from '@/features/area-cliente/locacoes/componentes/SecaoUltimasLocacoes';
import { useLocacoesCliente } from '@/features/area-cliente/locacoes/hooks/useMinhasLocacoes';
import { MENSAGEM_LOCACOES_ERRO_LISTA, MENSAGEM_LOCACOES_ERRO_TITULO } from '@/lib/mensagensLocacoesCliente';
import { ErroApi } from '@/types/api';

vi.mock('@/features/area-cliente/locacoes/hooks/useMinhasLocacoes', () => ({
  useLocacoesCliente: vi.fn(),
}));

const TEXTO_TECNICO_DO_SERVIDOR = 'Texto técnico do servidor.';

/** Marcação estática da seção com a consulta em erro: a mesma mensagem dos painéis (ERRO-1 em falha de rede ou 5xx). */
function renderizarComErro(erro: unknown): string {
  vi.mocked(useLocacoesCliente).mockReturnValue({
    data: undefined,
    error: erro,
    isError: true,
    isPending: false,
    isFetching: false,
    refetch: vi.fn(),
  } as unknown as ReturnType<typeof useLocacoesCliente>);
  return renderToStaticMarkup(
    <MemoryRouter>
      <SecaoUltimasLocacoes aoVerTodas={() => undefined} />
    </MemoryRouter>,
  );
}

describe('SecaoUltimasLocacoes (erro da consulta)', () => {
  it('sem resposta do servidor (status 0): mostra o título e o texto ERRO-1 aprovados, igual aos painéis', () => {
    const html = renderizarComErro(new ErroApi({ status: 0, mensagem: TEXTO_TECNICO_DO_SERVIDOR }));
    expect(html).toContain(MENSAGEM_LOCACOES_ERRO_TITULO);
    expect(html).toContain(MENSAGEM_LOCACOES_ERRO_LISTA);
    expect(html).not.toContain(TEXTO_TECNICO_DO_SERVIDOR);
  });

  it('falha 500: usa o mesmo texto ERRO-1, sem a mensagem técnica do servidor', () => {
    const html = renderizarComErro(new ErroApi({ status: 500, mensagem: TEXTO_TECNICO_DO_SERVIDOR }));
    expect(html).toContain(MENSAGEM_LOCACOES_ERRO_LISTA);
    expect(html).not.toContain(TEXTO_TECNICO_DO_SERVIDOR);
  });
});
