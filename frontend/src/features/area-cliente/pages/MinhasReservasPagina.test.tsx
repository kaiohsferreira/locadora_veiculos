import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useCancelarReserva } from '@/features/area-cliente/hooks/useCancelarReserva';
import { useReservas, useTotalReservas } from '@/features/area-cliente/hooks/useMinhasReservas';
import {
  RESERVA_ATIVA,
  RESERVA_CONFIRMADA,
  semTextosAprovadosDoDesenho,
  TERMOS_SEM_LASTRO,
} from '@/features/area-cliente/reservas/fixturasReservas';
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
  usuario: { nome: 'Marina Costa', perfil: 'cliente' },
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

  it('a raiz da página corta os círculos decorativos com overflow-clip (não rola de lado ao focar uma aba)', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html.startsWith('<div class="relative overflow-clip">')).toBe(true);
    expect(html).not.toContain('relative overflow-hidden');
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
    expect(html).toMatch(/Histórico de Locações<\/span><span[^>]*>1<\/span>/);
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
    expect(html).toMatch(/Histórico de Locações<\/span><\/button>/);
    expect(html).not.toMatch(/Histórico de Locações<\/span><span/);
    expect(html).toContain('Bem-vindo de volta, Marina Costa');
  });

  it('no celular a segunda aba tem o rótulo curto "Histórico"; o rótulo longo só aparece no computador', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toMatch(/<span class="md:hidden">Histórico<\/span><span class="hidden md:inline">Histórico de Locações<\/span>/);
    // A primeira aba não tem rótulo curto: o mesmo texto vale em todas as larguras.
    expect(html).toMatch(/<span>Ativas e Próximas<\/span>/);
  });

  it('as abas ficam lado a lado no celular: o trilho ocupa a largura e cada aba divide o espaço', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toMatch(/role="tablist"[^>]*class="flex w-full[^"]*"/);
    expect(html.match(/role="tab"[^>]*class="[^"]*flex-1/g)?.length).toBe(2);
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
    const limpo = semTextosAprovadosDoDesenho(html).toLowerCase();
    for (const termo of TERMOS_SEM_LASTRO) {
      expect(limpo, `termo proibido: ${termo}`).not.toContain(termo.toLowerCase());
    }
  });

  it('a aba Ativas é a inicial: a lista de ativas e não o histórico', () => {
    const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
    expect(html).toMatch(/role="tab"[^>]*aria-selected="true"[^>]*>/);
    expect(html).not.toContain('Filtrar por código ou modelo');
  });

  describe('foto de capa nos cartões (contrato reserva v2)', () => {
    const URL_CAPA = 'https://armazenamento.exemplo/bucket/veiculos/7/fotos/abc.jpg?assinatura=teste';
    const MENSAGEM_FALHA = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';
    const CAPA = { url: URL_CAPA, urlMiniatura: null, expiraEm: '2026-11-16T21:15:00.0000000+00:00' };

    it('com foto de capa: a imagem aparece no cartão, sem aviso de erro', () => {
      const html = renderizar({
        lista: consultaCom({ data: pagina([{ ...RESERVA_ATIVA, fotoCapa: CAPA, erroFotoCapa: null }], 1) }),
      });
      expect(html).toContain(`src="${URL_CAPA}"`);
      expect(html).toContain('alt="Jeep Renegade Longitude"');
      expect(html).not.toContain('<output');
    });

    it('sem foto cadastrada (nulo e nulo): a caixa com o ícone de carro e nenhum aviso', () => {
      const html = renderizar({
        lista: consultaCom({ data: pagina([{ ...RESERVA_ATIVA, fotoCapa: null, erroFotoCapa: null }], 1) }),
      });
      expect(html).toContain('directions_car');
      expect(html).not.toContain(URL_CAPA);
      expect(html).not.toContain('<output');
    });

    it('falha do armazenamento: a lista sai sem a foto e o aviso aparece UMA vez, com a mensagem do servidor', () => {
      const html = renderizar({
        lista: consultaCom({
          data: pagina(
            [
              { ...RESERVA_ATIVA, fotoCapa: null, erroFotoCapa: MENSAGEM_FALHA },
              { ...RESERVA_CONFIRMADA, fotoCapa: null, erroFotoCapa: MENSAGEM_FALHA },
            ],
            2,
          ),
        }),
      });
      expect(html.split('<output').length - 1).toBe(1);
      expect(html.split(MENSAGEM_FALHA).length - 1).toBe(1);
      expect(html).toContain('AV-00042');
      expect(html).toContain('AV-00043');
      expect(html).not.toContain('<img');
    });

    it('sem erro de foto em nenhum item: o aviso não aparece', () => {
      const html = renderizar({
        lista: consultaCom({ data: pagina([{ ...RESERVA_ATIVA, fotoCapa: CAPA, erroFotoCapa: null }], 1) }),
      });
      expect(html).not.toContain('<output');
      expect(html).not.toContain(MENSAGEM_FALHA);
    });

    it('resposta antiga, sem os campos de capa: a tela funciona com o ícone de carro e sem aviso', () => {
      const html = renderizar({ lista: consultaCom({ data: pagina([RESERVA_ATIVA], 1) }) });
      expect(html).toContain('AV-00042');
      expect(html).toContain('directions_car');
      expect(html).not.toContain('<output');
    });

    it('a URL assinada nunca vai para atributo de dados da página', () => {
      const html = renderizar({
        lista: consultaCom({ data: pagina([{ ...RESERVA_ATIVA, fotoCapa: CAPA, erroFotoCapa: null }], 1) }),
      });
      expect(html).not.toMatch(/data-[\w-]*="[^"]*https?:/);
    });
  });
});
