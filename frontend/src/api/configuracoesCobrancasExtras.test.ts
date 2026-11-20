import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { criarConfiguracaoCobrancasExtras, desativarConfiguracaoCobrancasExtras, editarConfiguracaoCobrancasExtras, listarConfiguracoesCobrancasExtras } from './configuracoesCobrancasExtras';
import type { DadosConfiguracaoCobrancasExtras } from '@/types/configuracoesCobrancasExtras';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
const dados: DadosConfiguracaoCobrancasExtras = { idCategoria: 1, baseFranquia: 'TOTAL', franquiaKm: 0, valorKmExcedente: 2, valorTanqueCompleto: 300, toleranciaAtrasoMinutos: 0, unidadeAtraso: 'HORA', fracionamentoAtraso: 'INICIADA', valorUnidadeAtraso: 20, baseAvarias: 'ESTIMATIVA_VISTORIA' };
describe('API de políticas', () => {
  beforeEach(() => vi.clearAllMocks());
  it('lista paginada e filtrada sem empresa no payload', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: { itens: [], total: 0 } });
    const filtro = { pagina: 2, tamanhoPagina: 10, idCategoria: 1 };
    await listarConfiguracoesCobrancasExtras(filtro);
    expect(api.get).toHaveBeenCalledExactlyOnceWith('/api/configuracoes-cobrancas-extras', { params: filtro, signal: undefined });
  });
  it('cria e edita com versão esperada flat; desativa por DELETE contratado', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { ...dados, idConfiguracao: 3, versao: 1 } });
    await criarConfiguracaoCobrancasExtras(dados);
    expect(api.post).toHaveBeenCalledExactlyOnceWith('/api/configuracoes-cobrancas-extras', dados);
    await editarConfiguracaoCobrancasExtras(3, { ...dados, versaoEsperada: 7 });
    expect(api.put).toHaveBeenCalledExactlyOnceWith('/api/configuracoes-cobrancas-extras/3', { ...dados, versaoEsperada: 7 });
    await desativarConfiguracaoCobrancasExtras(3);
    expect(api.delete).toHaveBeenCalledExactlyOnceWith('/api/configuracoes-cobrancas-extras/3');
  });
});
