import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import {
  LOCACAO_ATIVA,
  LOCACAO_ATRASADA,
  LOCACAO_FINALIZADA,
  respostaComItens,
} from '@/features/area-cliente/locacoes/fixturasLocacoes';
import { useLocacoesCliente, useTotalLocacoesCliente } from '@/features/area-cliente/locacoes/hooks/useMinhasLocacoes';
import { mensagemReservasDiarias } from '@/lib/mensagensReservas';
import { ErroApi } from '@/types/api';
import type { FiltroLocacoes, LocacaoCliente, SituacaoLocacao } from '@/types/locacoesCliente';
import MinhasLocacoesPagina from './MinhasLocacoesPagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê, e os hooks das consultas são substituídos.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn() },
}));
vi.mock('@/features/area-cliente/locacoes/hooks/useMinhasLocacoes', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/features/area-cliente/locacoes/hooks/useMinhasLocacoes')>()),
  useLocacoesCliente: vi.fn(),
  useTotalLocacoesCliente: vi.fn(),
}));

type ConsultaLocacoes = ReturnType<typeof useLocacoesCliente>;

/** Consulta falsa com os campos que a tela lê. Os demais campos do TanStack Query não são usados aqui. */
function consultaCom(parcial: Record<string, unknown>): ConsultaLocacoes {
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
  } as unknown as ConsultaLocacoes;
}

interface Cenario {
  /** Lista da situação em andamento (a aba inicial). */
  andamento?: ConsultaLocacoes;
  /** Lista do histórico (seção "Últimas locações" e aba Histórico). */
  historico?: ConsultaLocacoes;
  totalAndamento?: number;
  totalHistorico?: number;
}

/** Renderiza a página com as consultas por situação, como a tela as chama. */
function renderizar({ andamento, historico, totalAndamento = 1, totalHistorico = 0 }: Cenario = {}): string {
  const listaAndamento = andamento ?? consultaCom({ data: respostaComItens([LOCACAO_ATIVA], 1) });
  const listaHistorico = historico ?? consultaCom({ data: respostaComItens([], 0) });

  vi.mocked(useLocacoesCliente).mockImplementation((filtro: FiltroLocacoes) =>
    filtro.situacao === 'HISTORICO' ? listaHistorico : listaAndamento,
  );
  vi.mocked(useTotalLocacoesCliente).mockImplementation((situacao: SituacaoLocacao) =>
    situacao === 'EM_ANDAMENTO'
      ? consultaCom({ data: respostaComItens([], totalAndamento) })
      : consultaCom({ data: respostaComItens([], totalHistorico) }),
  );

  return renderToStaticMarkup(
    <MemoryRouter>
      <MinhasLocacoesPagina />
    </MemoryRouter>,
  );
}

/** Classes do contador (o span com o número) logo depois do rótulo da aba. */
function classesDoContador(html: string, rotulo: string): string[] {
  const achado = html.match(new RegExp(`>${rotulo}</span><span class="([^"]*)">`));
  return achado ? achado[1].split(' ') : [];
}

function comFoto(locacao: LocacaoCliente, erro: string | null): LocacaoCliente {
  return { ...locacao, fotoCapa: null, erroFotoCapa: erro };
}

describe('MinhasLocacoesPagina (T-FE-02, tela 27)', () => {
  beforeEach(() => {
    vi.mocked(useLocacoesCliente).mockReset();
    vi.mocked(useTotalLocacoesCliente).mockReset();
  });

  it('a raiz da página corta os círculos decorativos com overflow-clip (sem rolagem lateral em 360 e 375 px)', () => {
    const html = renderizar();
    expect(html.startsWith('<div class="relative overflow-clip">')).toBe(true);
    expect(html).not.toContain('relative overflow-hidden');
  });

  it('cabeçalho: título, descrição aprovada e o botão Nova Reserva', () => {
    const html = renderizar();
    expect(html).toContain('>Minhas locações</h1>');
    expect(html).toContain('Acompanhe suas locações em andamento, o prazo de devolução e consulte o histórico.');
    expect(html).toContain('Nova Reserva');
  });

  it('duas abas, Em andamento e Histórico, e nenhuma aba Próximas', () => {
    const html = renderizar();
    expect(html).toContain('role="tablist"');
    expect(html).toContain('aria-label="Navegação de locações"');
    expect(html).toContain('Em andamento');
    expect(html).toContain('Histórico');
    expect(html).not.toContain('Próximas');
  });

  it('contador de cada aba mostra o total da situação', () => {
    const html = renderizar({ totalAndamento: 1, totalHistorico: 4 });
    expect(html).toMatch(/Em andamento<\/span><span class="[^"]*">1<\/span>/);
    expect(html).toMatch(/Histórico<\/span><span class="[^"]*">4<\/span>/);
  });

  it('no celular, o contador da aba ativa é laranja (frota-oficina) e o da inativa é cinza-azulado (anexo, item 3)', () => {
    const html = renderizar({ totalAndamento: 1, totalHistorico: 4 });
    const ativa = classesDoContador(html, 'Em andamento');
    const inativa = classesDoContador(html, 'Histórico');
    expect(ativa).toEqual(expect.arrayContaining(['bg-frota-oficina', 'font-bold', 'text-branco']));
    expect(ativa).not.toContain('bg-steel-alto');
    expect(inativa).toEqual(expect.arrayContaining(['bg-steel-alto', 'font-medium', 'text-status-neutro']));
    expect(inativa).not.toContain('bg-frota-oficina');
  });

  it('o contador mantém o fundo steel-suave no computador (desenho 27) nas duas abas', () => {
    const html = renderizar({ totalAndamento: 1, totalHistorico: 4 });
    expect(classesDoContador(html, 'Em andamento')).toContain('md:bg-steel-suave');
    expect(classesDoContador(html, 'Histórico')).toContain('md:bg-steel-suave');
  });

  it('trilho das abas: steel-suave no celular e steel-fundo no computador', () => {
    const html = renderizar();
    const achado = html.match(/role="tablist"[^>]*class="([^"]*)"/);
    const classes = achado ? achado[1].split(' ') : [];
    expect(classes).toEqual(expect.arrayContaining(['bg-steel-suave', 'md:bg-steel-fundo']));
  });

  it('a locação ativa aparece com o selo, o número da locação, a reserva, a locadora e as especificações', () => {
    const html = renderizar();
    expect(html).toContain('Locação Ativa');
    expect(html).toContain('Locação nº 12');
    expect(html).toContain('Reserva AV-00042');
    expect(html).toContain('Retirado em Locadora Centro');
    expect(html).toContain('Jeep Renegade Longitude');
    expect(html).toContain('Quilometragem na retirada');
    expect(html).toContain('24.850 km');
    expect(html).toContain('Combustível na retirada');
    expect(html).toContain('50%');
    expect(html).toContain('Locação em andamento');
    expect(html).toContain('Como chegar');
  });

  it('o cronômetro mostra o selo com a tolerância da categoria e o rótulo do tempo restante', () => {
    const html = renderizar();
    expect(html).toContain('Tempo Restante para Devolução');
    expect(html).toContain('Dentro do prazo • Tolerância: 59 min');
  });

  it('locação marcada como atrasada pelo servidor: selo "Locação Atrasada" e aviso de cobrança', () => {
    const html = renderizar({ andamento: consultaCom({ data: respostaComItens([LOCACAO_ATRASADA], 1) }) });
    expect(html).toContain('Locação Atrasada');
    expect(html).toContain('Prazo de devolução vencido. O atraso pode gerar cobrança extra.');
  });

  it('"Últimas locações do histórico" aparece na aba Em andamento, com o link para as locações encerradas', () => {
    const html = renderizar({
      historico: consultaCom({ data: respostaComItens([comFoto(LOCACAO_FINALIZADA, null)], 4) }),
      totalHistorico: 4,
    });
    expect(html).toContain('Últimas locações do histórico');
    expect(html).toContain('Suas últimas locações encerradas ou canceladas.');
    expect(html).toContain('Ver todas as 4 locações');
    expect(html).toContain('Locação nº 9');
  });

  it('sem histórico, a seção "Últimas locações" não aparece', () => {
    const html = renderizar();
    expect(html).not.toContain('Últimas locações do histórico');
  });

  it('sem nenhuma locação em andamento: o vazio aprovado, com o botão para as reservas', () => {
    const html = renderizar({ andamento: consultaCom({ data: respostaComItens([], 0) }), totalAndamento: 0 });
    expect(html).toContain('Você não tem locações em andamento.');
    expect(html).toContain('Quando você retirar um veículo, a locação aparece aqui.');
    expect(html).toContain('Ver minhas reservas');
    expect(html).toContain('href="/minhas-reservas"');
  });

  it('carregando a primeira lista: a área fica ocupada, com o texto de carregando só para leitor de tela', () => {
    const html = renderizar({ andamento: consultaCom({ isPending: true }) });
    expect(html).toContain('aria-busy="true"');
    expect(html).toContain('Carregando...');
    expect(html).not.toContain('Locação nº 12');
  });

  it('erro sem dado: o título de erro aprovado e a tentativa de novo', () => {
    const html = renderizar({
      andamento: consultaCom({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Falha.' }) }),
    });
    expect(html).toContain('Não foi possível carregar suas locações.');
    expect(html).toContain('Tentar novamente');
  });

  it('as duas listas falham (403 para funcionário): um só cartão de erro e sem a seção de últimas locações', () => {
    const acessoNegado = new ErroApi({ status: 403, mensagem: 'Acesso negado.' });
    const html = renderizar({
      andamento: consultaCom({ isError: true, error: acessoNegado }),
      historico: consultaCom({ isError: true, error: acessoNegado }),
    });
    expect(html.split('Não foi possível carregar suas locações.').length - 1).toBe(1);
    expect(html.split('Tentar novamente').length - 1).toBe(1);
    expect(html).not.toContain('Últimas locações do histórico');
  });

  it('falha de foto de capa: o aviso aparece uma vez, com a mensagem do servidor, e a lista continua', () => {
    const MENSAGEM = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';
    const html = renderizar({
      andamento: consultaCom({ data: respostaComItens([comFoto(LOCACAO_ATIVA, MENSAGEM)], 1) }),
    });
    expect(html.split('<output').length - 1).toBe(1);
    expect(html.split(MENSAGEM).length - 1).toBe(1);
    expect(html).toContain('Locação nº 12');
  });

  it('sem foto cadastrada: a caixa com o ícone de carro e nenhuma imagem', () => {
    const html = renderizar();
    expect(html).toContain('directions_car');
    expect(html).not.toContain('<img');
  });

  it('celular (anexo, item 5): caixa com foto compacta ao lado do texto, nome em h2 e categoria e placa no texto', () => {
    const html = renderizar();
    expect(html).toContain('flex min-w-0 items-center gap-3 rounded bg-steel-fundo p-3 md:hidden');
    expect(html).toContain('tracking-tighter text-navy-profundo">Jeep Renegade Longitude</h2>');
    expect(html).toContain('bg-steel-suave md:bg-steel-fundo');
    expect(html).toContain(`>${LOCACAO_ATIVA.categoriaNome}</span>`);
  });

  it('computador: o título da locação e o bloco grande da foto só aparecem a partir de md', () => {
    const html = renderizar();
    expect(html).toContain('hidden break-words text-marca font-bold tracking-tight text-navy-profundo md:block');
    expect(html).toContain('relative hidden aspect-[16/10] w-full overflow-hidden rounded-xl bg-steel-fundo p-2 md:block');
  });

  it('celular (anexo, item 6): roteiro com linha vertical e o selo de diárias no topo, como na tela 28', () => {
    const html = renderizar();
    expect(html).toContain('flex flex-col gap-3 rounded-xl bg-steel-fundo p-4 md:hidden');
    expect(html).toContain('before:left-3 before:top-4');
    expect(html).toContain(mensagemReservasDiarias(LOCACAO_ATIVA.diarias));
  });

  it('nenhum termo sem lastro aparece na tela (sem prorrogar, recibo, WhatsApp, SAC, Gold, vaga ou LOC-)', () => {
    const html = renderizar({
      historico: consultaCom({ data: respostaComItens([LOCACAO_FINALIZADA], 1) }),
      totalHistorico: 1,
    }).toLowerCase();
    const proibidos = [
      'prorrogar',
      'recibo',
      'nota fiscal',
      'whatsapp',
      'sac',
      'gold',
      'vaga',
      'loc-',
      '0800',
      'franquia',
      'ilimitada',
      'ver contrato',
      'aeroporto',
      'pátio',
    ];
    for (const termo of proibidos) {
      expect(html, `termo proibido: ${termo}`).not.toContain(termo);
    }
  });
});
