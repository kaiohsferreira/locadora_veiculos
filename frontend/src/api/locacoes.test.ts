import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { abrirLocacao, buscarContextoRetirada } from './locacoes';
import { ErroApi } from '@/types/api';
import { contextoRetiradaTeste } from '@/features/locacoes/retirada/contextoRetirada.fixture';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));
describe('contrato da retirada', () => {
  beforeEach(() => vi.clearAllMocks());
  it('consulta somente o contexto contratado e preserva máscaras', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: contextoRetiradaTeste });
    expect(await buscarContextoRetirada(8)).toEqual(contextoRetiradaTeste);
    expect(api.get).toHaveBeenCalledWith('/api/locacoes/retirada/8');
  });
  it('envia apenas idReserva e usa idLocacao retornado', async () => {
    const retorno = { idLocacao: 42, kmSaida: 12000, status: 'EM_ANDAMENTO' };
    vi.mocked(api.post).mockResolvedValueOnce({ data: retorno });
    expect(await abrirLocacao({ idReserva: 8, cpfConferido: true } as { idReserva: number })).toEqual(retorno);
    expect(api.post).toHaveBeenCalledWith('/api/locacoes', { idReserva: 8 });
  });
  it.each([0, 400, 401, 403, 404, 409, 500])('propaga falha %s sem fabricar dados', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Não foi possível continuar.' });
    vi.mocked(api.get).mockRejectedValueOnce(erro);
    await expect(buscarContextoRetirada(8)).rejects.toBe(erro);
    vi.mocked(api.post).mockRejectedValueOnce(erro);
    await expect(abrirLocacao({ idReserva: 8 })).rejects.toBe(erro);
  });
});
