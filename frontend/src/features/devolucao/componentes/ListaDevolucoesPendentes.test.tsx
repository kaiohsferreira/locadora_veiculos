import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { ErroApi, type Pagina } from '@/types/api';
import type { DevolucaoPendente } from '@/types/devolucao';
import type { CargoFuncionario } from '@/types/enums';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { useDevolucoesPendentes } from '../hooks/useDevolucoesPendentes';
import { ListaDevolucoesPendentes } from './ListaDevolucoesPendentes';

vi.mock('../hooks/useDevolucoesPendentes', () => ({ useDevolucoesPendentes: vi.fn() }));

type Consulta = ReturnType<typeof useDevolucoesPendentes>;

const pendente: DevolucaoPendente = {
  idLocacao: 12, codigoReserva: 'AV-00012', nomeCliente: 'Ana Souza', placa: 'ABC1D23', marca: 'Fiat', modelo: 'Toro',
  status: 'EM_ANDAMENTO', atrasada: false, devolucaoPrevistaEfetiva: '2026-11-26T10:00:00', devolucaoPrevistaEfetivaStr: '26/11/2026 10:00',
};
const atrasada: DevolucaoPendente = { ...pendente, idLocacao: 13, nomeCliente: 'Bruno Lima', atrasada: true };
const pagina = (itens: DevolucaoPendente[]): Pagina<DevolucaoPendente> => ({ itens, total: itens.length, pagina: 1, tamanhoPagina: 10 });

const SESSAO = (cargo: CargoFuncionario): AutenticacaoContextoValor => ({
  usuario: { nome: 'Marina Costa', perfil: 'funcionario', cargo, empresaNomeFantasia: 'Rota Sul Locadora' },
  autenticado: true, autenticacaoIgnorada: false, entrar: () => undefined, sair: () => undefined, possuiPerfil: () => true,
});

function renderizar(parcial: Partial<Consulta>, cargo: 'ATENDENTE' | 'VENDEDOR' = 'ATENDENTE'): string {
  vi.mocked(useDevolucoesPendentes).mockReturnValue({
    data: undefined, isPending: false, isError: false, isFetching: false, refetch: vi.fn(), ...parcial,
  } as unknown as Consulta);
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={SESSAO(cargo)}>
      <MemoryRouter>
        <ListaDevolucoesPendentes />
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

describe('lista "Devoluções pendentes" (página de entrada da retirada)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('mostra o título, a locação, o cliente, o veículo, a placa e a devolução prevista', () => {
    const html = renderizar({ data: pagina([pendente]) });
    expect(html).toContain(M.pendentes.titulo);
    expect(html).toContain('Locação nº 12');
    expect(html).toContain('Ana Souza');
    expect(html).toContain('Fiat Toro');
    expect(html).toContain('26/11/2026 10:00');
    expect(html).toContain(M.pendentes.colunas.devolucaoPrevista);
  });

  it('o botão leva à tela 06 da locação, com o nome da linha para o leitor de tela', () => {
    const html = renderizar({ data: pagina([pendente]) });
    expect(html).toContain('href="/back-office/locacoes/12/devolucao"');
    expect(html).toContain(`aria-label="${M.pendentes.botao} Locação nº 12"`);
    expect(html).toContain(`>${M.pendentes.botao}</a>`);
  });

  it('atrasada ganha o selo "Atrasada"; a em andamento não', () => {
    const html = renderizar({ data: pagina([pendente, atrasada]) });
    expect(html).toContain(M.pendentes.atrasada);
    expect(html.match(new RegExp(M.pendentes.atrasada, 'g'))).toHaveLength(1);
  });

  it('lista vazia traz o texto próprio e nenhum botão', () => {
    const html = renderizar({ data: pagina([]) });
    expect(html).toContain(M.pendentes.vazio);
    expect(html).not.toContain(M.pendentes.botao);
  });

  it('carregando mostra o estado ocupado, sem o texto de vazio', () => {
    const html = renderizar({ isPending: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain(M.pendentes.vazio);
  });

  it('erro mostra a mensagem do servidor e Tentar novamente, sem a tabela', () => {
    const html = renderizar({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Não foi possível listar.' }) });
    expect(html).toContain('Não foi possível listar.');
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain(M.pendentes.botao);
  });

  it('cargo que não encerra vê a lista, com o botão desligado e sem link para a tela 06', () => {
    const html = renderizar({ data: pagina([pendente]) }, 'VENDEDOR');
    expect(html).toContain('Ana Souza');
    expect(html).toContain(M.pendentes.botao);
    expect(html).not.toContain('href="/back-office/locacoes/12/devolucao"');
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>[^<]*Registrar devolução/);
  });

  it('consulta só as em andamento (que já incluem as atrasadas) na primeira página', () => {
    renderizar({ data: pagina([pendente]) });
    expect(useDevolucoesPendentes).toHaveBeenCalledWith({ status: 'EM_ANDAMENTO', pagina: 1, tamanhoPagina: 10 });
  });
});
