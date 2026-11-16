import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useCancelarReserva } from '@/features/area-cliente/hooks/useCancelarReserva';
import { useReservas, useTotalReservas } from '@/features/area-cliente/hooks/useMinhasReservas';
import { RESERVA_ATIVA, TERMOS_SEM_LASTRO } from '@/features/area-cliente/reservas/fixturasReservas';
import { ErroApi } from '@/types/api';
import type { ReservaCliente, RespostaReservas, SituacaoReserva } from '@/types/reservas';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import MinhasReservasPagina from './MinhasReservasPagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), delete: vi.fn() },
}));
vi.mock('@/features/area-cliente/hooks/useMinhasReservas', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/features/area-cliente/hooks/useMinhasReservas')>()),
  useReservas: vi.fn(),
  useTotalReservas: vi.fn(),
}));
vi.mock('@/features/area-cliente/hooks/useCancelarReserva', () => ({
  useCancelarReserva: vi.fn(),
}));

type ConsultaReservas = ReturnType<typeof useReservas>;

const AUTENTICACAO: AutenticacaoContextoValor = {
  usuario: { nome: 'Marina Costa', perfil: 'cliente', statusCadastro: 'APROVADO' },
  autenticado: true,
  autenticacaoIgnorada: false,
  entrar: () => undefined,
  sair: () => undefined,
  possuiPerfil: () => true,
};

function consultaCom(parcial: Record<string, unknown>): ConsultaReservas {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    isPlaceholderData: false,
    dataUpdatedAt: 0,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaReservas;
}

function pagina(itens: ReservaCliente[], total: number): RespostaReservas {
  return { itens, total, pagina: 1, tamanhoPagina: 5 };
}

interface Cenario {
  lista: ConsultaReservas;
  ativas?: ConsultaReservas;
  historico?: ConsultaReservas;
}

/** Renderiza a página. As consultas de contagem são separadas por situação, como na tela. */
function renderizar({ lista, ativas, historico }: Cenario): string {
  vi.mocked(useReservas).mockReturnValue(lista);
  vi.mocked(useTotalReservas).mockImplementation((situacao: SituacaoReserva) => {
    if (situacao === 'ATIVAS') return ativas ?? consultaCom({ data: pagina([], 2) });
    return historico ?? consultaCom({ data: pagina([], 1) });
  });
  vi.mocked(useCancelarReserva).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
  } as unknown as ReturnType<typeof useCancelarReserva>);

  return renderToStaticMarkup(
    <MemoryRouter>
      <AutenticacaoContexto.Provider value={AUTENTICACAO}>
        <AvisoProvider>
          <MinhasReservasPagina />
        </AvisoProvider>
      </AutenticacaoContexto.Provider>
    </MemoryRouter>,
  );
}

describe('MinhasReservasPagina (T-FE-03)', () => {
  beforeEach(() => {
    vi.mocked(useReservas).mockReset();
    vi.mocked(useTotalReservas).mockReset();
    vi.mocked(useCancelarReserva).mockReset();
  });

  it('cumprimenta pelo nome da sessão e mostra o texto verdadeiro da descrição', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toContain('Bem-vindo de volta, Marina Costa');
    expect(html).toContain('Acompanhe suas reservas ativas e consulte o histórico.');
  });

  it('as duas abas mostram o total de cada situação no contador', () => {
    const html = renderizar({
      lista: consultaCom({ data: pagina([RESERVA_ATIVA], 2) }),
      ativas: consultaCom({ data: pagina([RESERVA_ATIVA], 2) }),
      historico: consultaCom({ data: pagina([], 1) }),
    });
    expect(html).toMatch(/Ativas e Próximas<\/span><span[^>]*>2<\/span>/);
    expect(html).toMatch(/Histórico<\/span><span[^>]*>1<\/span>/);
  });

  it('a aba Faturas e Notas Fiscais não aparece', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).not.toContain('Faturas');
    expect(html).not.toContain('Notas Fiscais');
  });

  it('contador que falhou some da aba, sem quebrar a tela', () => {
    const html = renderizar({
      lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }),
      historico: consultaCom({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Falha.' }) }),
    });
    // A aba Histórico sai sem número (o rótulo fica, o contador não).
    expect(html).toMatch(/<span>Histórico<\/span><\/button>/);
    expect(html).not.toMatch(/Histórico<\/span><span/);
    expect(html).toContain('Bem-vindo de volta, Marina Costa');
  });

  it('carregando a primeira lista: o cabeçalho e as abas aparecem na hora, com a área ocupada', () => {
    const html = renderizar({ lista: consultaCom({ isPending: true }) });
    expect(html).toContain('Bem-vindo de volta, Marina Costa');
    expect(html).toContain('Ativas e Próximas');
    expect(html).toContain('aria-busy="true"');
  });

  it('"Atualizado" só aparece depois da primeira consulta (dataUpdatedAt diferente de zero)', () => {
    const semConsulta = renderizar({ lista: consultaCom({ dataUpdatedAt: 0 }) });
    expect(semConsulta).not.toContain('Atualizado');

    const comConsulta = renderizar({
      lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1), dataUpdatedAt: Date.now() }),
    });
    expect(comConsulta).toContain('Atualizado');
  });

  it('com reservas ativas: o cartão aparece com o botão Cancelar reserva e nenhum termo sem lastro', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toContain('AV-00042');
    expect(html).toContain('Cancelar reserva');
    for (const termo of TERMOS_SEM_LASTRO) {
      expect(html.toLowerCase(), `termo proibido: ${termo}`).not.toContain(termo.toLowerCase());
    }
  });

  it('a aba Ativas é a inicial: a lista de ativas e não o histórico', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toMatch(/role="tab"[^>]*aria-selected="true"[^>]*>/);
    expect(html).not.toContain('Filtrar por código ou modelo');
  });
});
