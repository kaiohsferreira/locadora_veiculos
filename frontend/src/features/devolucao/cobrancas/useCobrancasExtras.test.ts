import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { UseMutationOptions } from '@tanstack/react-query';
import type { ApuracaoCobrancasExtras, ConfirmarApuracaoCobrancasExtras } from '@/types/cobrancasExtras';
import { ErroApi } from '@/types/api';
import { buscarApuracaoCobrancasExtras, registrarApuracaoCobrancasExtras } from '@/api/cobrancasExtras';
import { useRegistrarApuracaoCobrancasExtras } from './useCobrancasExtras';
const capturado = vi.hoisted(() => ({ opcoes: {} as UseMutationOptions<ApuracaoCobrancasExtras, Error, ConfirmarApuracaoCobrancasExtras> }));
vi.mock('@tanstack/react-query', () => ({ useMutation: vi.fn(opcoes => { capturado.opcoes = opcoes; return {}; }), useQuery: vi.fn(), useQueryClient: () => ({ cancelQueries: vi.fn(), setQueryData: vi.fn(), invalidateQueries: vi.fn() }) }));
vi.mock('@/api/cobrancasExtras', () => ({ buscarApuracaoCobrancasExtras: vi.fn(), registrarApuracaoCobrancasExtras: vi.fn() }));
describe('registro e recuperação da apuração', () => {
  beforeEach(() => { vi.clearAllMocks(); useRegistrarApuracaoCobrancasExtras(42); });
  async function executar() { return capturado.opcoes.mutationFn!({ idConfiguracao: 2, versaoEsperada: 3 }, {} as never); }
  it.each([0, 409])('recupera resposta incerta %s por GET sem repetir POST, incluindo total zero', async status => {
    vi.mocked(registrarApuracaoCobrancasExtras).mockRejectedValueOnce(new ErroApi({ status, mensagem: 'Falha' }));
    const persistida = { idApuracao: 8, total: 0, cobrancas: [] } as unknown as ApuracaoCobrancasExtras;
    vi.mocked(buscarApuracaoCobrancasExtras).mockResolvedValueOnce(persistida);
    expect(await executar()).toBe(persistida);
    expect(registrarApuracaoCobrancasExtras).toHaveBeenCalledTimes(1);
    expect(buscarApuracaoCobrancasExtras).toHaveBeenCalledExactlyOnceWith(42);
    expect(capturado.opcoes.retry).toBe(false);
  });
  it('conflito sem apuração preservada propaga erro para exigir nova simulação', async () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Versão mudou' });
    vi.mocked(registrarApuracaoCobrancasExtras).mockRejectedValueOnce(erro);
    vi.mocked(buscarApuracaoCobrancasExtras).mockResolvedValueOnce(null);
    await expect(executar()).rejects.toBe(erro);
    expect(registrarApuracaoCobrancasExtras).toHaveBeenCalledTimes(1);
  });
  it('não tenta recuperação para solicitação inválida', async () => {
    vi.mocked(registrarApuracaoCobrancasExtras).mockRejectedValueOnce(new ErroApi({ status: 400, mensagem: 'Inválida' }));
    await expect(executar()).rejects.toBeInstanceOf(ErroApi);
    expect(buscarApuracaoCobrancasExtras).not.toHaveBeenCalled();
  });
});
