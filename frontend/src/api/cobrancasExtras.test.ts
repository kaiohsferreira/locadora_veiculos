import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { buscarApuracaoCobrancasExtras, registrarApuracaoCobrancasExtras, simularCobrancasExtras } from './cobrancasExtras';
import { ErroApi } from '@/types/api';
vi.mock('./client', () => ({ api: { post: vi.fn(), get: vi.fn() } }));
describe('API RF23 cadastrada', () => {
  beforeEach(() => vi.clearAllMocks());
  it('simula com corpo vazio sem enviar tarifas', async () => {
    const resposta = { simulacao: true, total: 0 };
    vi.mocked(api.post).mockResolvedValueOnce({ data: resposta });
    expect(await simularCobrancasExtras(42)).toBe(resposta);
    expect(api.post).toHaveBeenCalledExactlyOnceWith('/api/locacoes/42/cobrancas-extras/simulacao', {});
  });
  it('registra somente identidade e versão conferidas', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { idApuracao: 8 } });
    await registrarApuracaoCobrancasExtras(42, { idConfiguracao: 3, versaoEsperada: 7 });
    expect(api.post).toHaveBeenCalledExactlyOnceWith('/api/locacoes/42/cobrancas-extras/apuracao', { idConfiguracao: 3, versaoEsperada: 7 });
  });
  it('404 na consulta significa apuração ainda não criada', async () => {
    vi.mocked(api.get).mockRejectedValueOnce(new ErroApi({ status: 404, mensagem: 'Ausente' }));
    expect(await buscarApuracaoCobrancasExtras(42)).toBeNull();
  });
  it.each([403, 409, 0])('não oculta falha %s como ausência', async status => {
    const erro = new ErroApi({ status, mensagem: 'Falha.' });
    vi.mocked(api.get).mockRejectedValueOnce(erro);
    await expect(buscarApuracaoCobrancasExtras(42)).rejects.toBe(erro);
    expect(api.get).toHaveBeenCalledTimes(1);
  });
});
