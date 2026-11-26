import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  listarMinhasLocacoes,
  montarParametrosListagemLocacoes,
  normalizarRespostaLocacoes,
} from '@/api/locacoesCliente';
import { LOCACAO_ATIVA } from '@/features/area-cliente/locacoes/fixturasLocacoes';
import type { FiltroLocacoes, LocacaoCliente } from '@/types/locacoesCliente';

// Nenhum teste chama a rede: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn() },
}));

const FILTRO: FiltroLocacoes = { pagina: 2, tamanhoPagina: 5, situacao: 'HISTORICO' };
const FILTRO_ANDAMENTO: FiltroLocacoes = { pagina: 1, tamanhoPagina: 5, situacao: 'EM_ANDAMENTO' };

/** Todos os campos anuláveis do contrato locacao-cliente v3 em null (o item mais vazio que a API pode mandar). */
const ITEM_COM_ANULAVEIS_NULOS: LocacaoCliente = {
  ...LOCACAO_ATIVA,
  codigoReserva: null,
  dataDevolucaoEfetiva: null,
  dataDevolucaoEfetivaStr: null,
  valorTotal: null,
  valorTotalStr: null,
  cor: null,
  fotoCapa: null,
  erroFotoCapa: null,
  toleranciaAtrasoMinutos: null,
  combustivelRetirada: null,
  combustivelRetiradaStr: null,
};

describe('listarMinhasLocacoes', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset();
  });

  it('faz GET em /api/locacoes/minhas, com página, tamanho e situação na query, e nada no corpo', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: { itens: [], total: 0, pagina: 2, tamanhoPagina: 5 },
    } as unknown as Awaited<ReturnType<typeof api.get>>);

    await listarMinhasLocacoes(FILTRO);

    expect(api.get).toHaveBeenCalledTimes(1);
    expect(api.get).toHaveBeenCalledWith('/api/locacoes/minhas', {
      params: { pagina: 2, tamanhoPagina: 5, situacao: 'HISTORICO' },
    });
  });

  it('item com todos os campos anuláveis em null volta inteiro, sem trocar nenhum null por outro valor', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: { itens: [ITEM_COM_ANULAVEIS_NULOS], total: 1, pagina: 1, tamanhoPagina: 5 },
    } as unknown as Awaited<ReturnType<typeof api.get>>);

    const resposta = await listarMinhasLocacoes(FILTRO_ANDAMENTO);

    expect(resposta.itens).toHaveLength(1);
    expect(resposta.itens[0]).toEqual(ITEM_COM_ANULAVEIS_NULOS);
    expect(resposta.itens[0].toleranciaAtrasoMinutos).toBeNull();
    expect(resposta.itens[0].combustivelRetirada).toBeNull();
    expect(resposta.itens[0].fotoCapa).toBeNull();
  });
});



describe('montarParametrosListagemLocacoes', () => {
  it('manda página, tamanho e situação (sem busca nem id de cliente: o servidor usa a sessão)', () => {
    expect(montarParametrosListagemLocacoes(FILTRO)).toEqual({ pagina: 2, tamanhoPagina: 5, situacao: 'HISTORICO' });
  });
});

describe('normalizarRespostaLocacoes', () => {
  it('resposta vazia ou sem lista vira página vazia, para a tela não quebrar', () => {
    expect(normalizarRespostaLocacoes(null, FILTRO)).toEqual({ itens: [], total: 0, pagina: 2, tamanhoPagina: 5 });
    expect(normalizarRespostaLocacoes({ total: 3 }, FILTRO)).toEqual({ itens: [], total: 0, pagina: 2, tamanhoPagina: 5 });
  });

  it('total ausente conta os itens recebidos', () => {
    const resposta = normalizarRespostaLocacoes({ itens: [{ idLocacao: 1 }, { idLocacao: 2 }] }, FILTRO);
    expect(resposta.total).toBe(2);
    expect(resposta.pagina).toBe(2);
  });

  it('resposta completa passa como veio', () => {
    const corpo = { itens: [{ idLocacao: 9 }], total: 7, pagina: 1, tamanhoPagina: 5 };
    expect(normalizarRespostaLocacoes(corpo, FILTRO)).toEqual(corpo);
  });
});
