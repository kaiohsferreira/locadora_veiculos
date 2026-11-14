import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { buscarVisaoGeral, montarParametrosVisaoGeral } from '@/api/painel';
import type { RespostaVisaoGeral } from '@/types/painel';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);

const RESPOSTA: RespostaVisaoGeral = {
  periodo: 'SETE_DIAS',
  periodoInicioStr: '07/11/2026',
  periodoFimStr: '13/11/2026',
  primeiroAcesso: false,
  valoresVisiveis: true,
  receitaPeriodo: 12000,
  receitaPeriodoStr: 'R$ 12.000,00',
  aReceber: 900,
  aReceberStr: 'R$ 900,00',
  quantidadeCobrancasEmAberto: 2,
  emAtraso: 0,
  emAtrasoStr: 'R$ 0,00',
  quantidadeCobrancasVencidas: 0,
  taxaOcupacaoPercentual: 40,
  veiculosLocados: 2,
  totalVeiculos: 5,
  locacoesAtivas: 2,
  devolucoesPrevistasHoje: 1,
  frotaPorSituacao: [{ situacao: 'LOCADO', quantidade: 2 }],
};

describe('montarParametrosVisaoGeral', () => {
  it('envia o período escolhido no parâmetro periodo', () => {
    expect(montarParametrosVisaoGeral('SETE_DIAS')).toEqual({ periodo: 'SETE_DIAS' });
    expect(montarParametrosVisaoGeral('MES_ATUAL')).toEqual({ periodo: 'MES_ATUAL' });
  });
});

describe('buscarVisaoGeral', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: RESPOSTA });
  });

  it('chama o endpoint do contrato com o período na query', async () => {
    await buscarVisaoGeral('SETE_DIAS');

    expect(getMock).toHaveBeenCalledWith('/api/painel/visao-geral', { params: { periodo: 'SETE_DIAS' } });
  });

  it('devolve o corpo da resposta sem alterar os campos', async () => {
    const dados = await buscarVisaoGeral('TRINTA_DIAS');

    expect(dados).toEqual(RESPOSTA);
  });

  it('repassa o erro do cliente HTTP sem tratar', async () => {
    const falha = new Error('falha de rede');
    getMock.mockRejectedValueOnce(falha);

    await expect(buscarVisaoGeral('HOJE')).rejects.toBe(falha);
  });
});
