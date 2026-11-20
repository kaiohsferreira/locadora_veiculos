import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MutationObserver, QueryClient, QueryObserver } from '@tanstack/react-query';
import { abrirLocacao, buscarContextoRetirada } from '@/api/locacoes';
import { ErroApi } from '@/types/api';
import { chaveRetirada, opcoesAbrirLocacao, opcoesRetirada } from './useRetirada';
import { contextoRetiradaTeste as contexto } from '../retirada/contextoRetirada.fixture';
vi.mock('@/api/locacoes', () => ({ abrirLocacao: vi.fn(), buscarContextoRetirada: vi.fn() }));
describe('consultas da retirada', () => {
  beforeEach(() => vi.clearAllMocks());
  it('id inválido não consulta', () => {
    const cliente = new QueryClient();
    const consulta = new QueryObserver(cliente, opcoesRetirada(null));
    const parar = consulta.subscribe(() => {});
    expect(buscarContextoRetirada).not.toHaveBeenCalled();
    parar(); cliente.clear();
  });
  it('sucesso memoriza locação sem afetar cache da vistoria', async () => {
    const cliente = new QueryClient();
    cliente.setQueryData(chaveRetirada(8), contexto);
    cliente.setQueryData(['vistoria-entrada', 42], { preservado: true });
    vi.mocked(abrirLocacao).mockResolvedValueOnce({ idLocacao: 42, idReserva: 8, kmSaida: 12000, status: 'EM_ANDAMENTO' });
    await new MutationObserver(cliente, opcoesAbrirLocacao(cliente, 8)).mutate();
    expect(cliente.getQueryData(chaveRetirada(8))).toEqual({ ...contexto, idLocacaoExistente: 42 });
    expect(cliente.getQueryData(['vistoria-entrada', 42])).toEqual({ preservado: true });
    cliente.clear();
  });
  it.each([0, 409])('falha %s refaz contexto e recupera abertura existente sem repetir POST', async (status) => {
    const cliente = new QueryClient();
    vi.mocked(buscarContextoRetirada).mockResolvedValue(contexto);
    const consulta = new QueryObserver(cliente, opcoesRetirada(8));
    const parar = consulta.subscribe(() => {});
    await consulta.refetch();
    vi.mocked(buscarContextoRetirada).mockResolvedValue({ ...contexto, idLocacaoExistente: 42 });
    const erro = new ErroApi({ status, mensagem: 'Não foi possível confirmar.' });
    vi.mocked(abrirLocacao).mockRejectedValueOnce(erro);
    await expect(new MutationObserver(cliente, opcoesAbrirLocacao(cliente, 8)).mutate()).rejects.toBe(erro);
    expect(abrirLocacao).toHaveBeenCalledTimes(1);
    expect(cliente.getQueryData(chaveRetirada(8))).toEqual({ ...contexto, idLocacaoExistente: 42 });
    parar(); cliente.clear();
  });
});
