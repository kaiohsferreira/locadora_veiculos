import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './client';
import { abrirLocacao, buscarContextoRetirada, TEMPO_LIMITE_ABERTURA_COM_CAUCAO_MS } from './locacoes';
import { ErroApi } from '@/types/api';
import type { DadosCaucaoAbertura } from '@/types/retirada';
import { contextoRetiradaComCaucaoTeste, contextoRetiradaTeste } from '@/features/locacoes/retirada/contextoRetirada.fixture';
vi.mock('./client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));

const CAUCAO: DadosCaucaoAbertura = {
  caucaoTokenCartao: 'FAKE-0123456789abcdef0123456789abcdef',
  caucaoMetodoPagamentoId: 'visa',
  caucaoEmissorId: null,
  caucaoIdentificadorDispositivo: null,
};

describe('contrato da retirada', () => {
  beforeEach(() => vi.clearAllMocks());
  it('consulta somente o contexto contratado e preserva máscaras', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: contextoRetiradaTeste });
    expect(await buscarContextoRetirada(8)).toEqual(contextoRetiradaTeste);
    expect(api.get).toHaveBeenCalledWith('/api/locacoes/retirada/8');
  });
  it('devolve os cinco campos da caução como vieram do servidor', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: contextoRetiradaComCaucaoTeste });
    const contexto = await buscarContextoRetirada(8);
    expect(contexto.caucaoExigida).toBe(true);
    expect(contexto.valorCaucao).toBe(1200);
    expect(contexto.valorCaucaoStr).toBe('R$ 1.200,00');
    expect(contexto.caucaoDisponivel).toBe(true);
    expect(contexto.caucaoModoSimulacao).toBe(true);
  });
  it('envia apenas idReserva e usa idLocacao retornado', async () => {
    const retorno = { idLocacao: 42, kmSaida: 12000, status: 'EM_ANDAMENTO' };
    vi.mocked(api.post).mockResolvedValueOnce({ data: retorno });
    expect(await abrirLocacao({ idReserva: 8, cpfConferido: true } as { idReserva: number })).toEqual(retorno);
    expect(api.post).toHaveBeenCalledWith('/api/locacoes', { idReserva: 8 });
  });
  it('sem caução: o POST fica como antes, sem tempo limite próprio (vale o padrão do cliente)', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { idLocacao: 42 } });
    await abrirLocacao({ idReserva: 8 });
    expect(vi.mocked(api.post).mock.calls[0]).toEqual(['/api/locacoes', { idReserva: 8 }]);
  });
  it('com caução: manda a reserva e os quatro campos do cartão, com tempo limite de 120 s', async () => {
    const retorno = { idLocacao: 42, idReserva: 8, kmSaida: 12000, status: 'EM_ANDAMENTO' };
    vi.mocked(api.post).mockResolvedValueOnce({ data: retorno });
    expect(await abrirLocacao({ idReserva: 8, caucao: CAUCAO })).toEqual(retorno);
    expect(TEMPO_LIMITE_ABERTURA_COM_CAUCAO_MS).toBe(120000);
    expect(api.post).toHaveBeenCalledWith(
      '/api/locacoes',
      {
        idReserva: 8,
        caucaoTokenCartao: 'FAKE-0123456789abcdef0123456789abcdef',
        caucaoMetodoPagamentoId: 'visa',
        caucaoEmissorId: null,
        caucaoIdentificadorDispositivo: null,
      },
      { timeout: 120000 },
    );
  });
  it('com caução: o corpo nunca leva valor, e-mail, CPF nem campo que não é do contrato', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { idLocacao: 42 } });
    const caucaoComSobras = { ...CAUCAO, valorCaucao: 1, email: 'outro@exemplo.com', cpf: '11122233344' } as DadosCaucaoAbertura;
    await abrirLocacao({ idReserva: 8, caucao: caucaoComSobras });
    const corpo = vi.mocked(api.post).mock.calls[0][1] as Record<string, unknown>;
    expect(Object.keys(corpo).sort()).toEqual([
      'caucaoEmissorId', 'caucaoIdentificadorDispositivo', 'caucaoMetodoPagamentoId', 'caucaoTokenCartao', 'idReserva',
    ]);
    for (const proibido of ['valorCaucao', 'valor', 'email', 'cpf', 'idEmpresa']) expect(corpo).not.toHaveProperty(proibido);
  });
  it.each([0, 400, 401, 403, 404, 409, 500])('propaga falha %s sem fabricar dados', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Não foi possível continuar.' });
    vi.mocked(api.get).mockRejectedValueOnce(erro);
    await expect(buscarContextoRetirada(8)).rejects.toBe(erro);
    vi.mocked(api.post).mockRejectedValueOnce(erro);
    await expect(abrirLocacao({ idReserva: 8 })).rejects.toBe(erro);
  });
  it.each([0, 403, 409, 429, 502])('com caução, propaga falha %s sem fabricar dados', async (status) => {
    const erro = new ErroApi({ status, mensagem: 'Não foi possível continuar.' });
    vi.mocked(api.post).mockRejectedValueOnce(erro);
    await expect(abrirLocacao({ idReserva: 8, caucao: CAUCAO })).rejects.toBe(erro);
  });
});
