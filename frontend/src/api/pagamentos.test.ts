import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { baixarPagamento, caminhoBaixarPagamento } from '@/api/pagamentos';
import type { BaixarPagamentoRequisicao, RespostaBaixaPagamento } from '@/types/painelFinanceiro';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const patchMock = vi.mocked(api.patch);

const REQUISICAO: BaixarPagamentoRequisicao = { forma: 'DINHEIRO', confirmarBaixaComPixGerado: false };

const RESPOSTA: RespostaBaixaPagamento = {
  idPagamento: 77,
  tipo: 'DIARIA',
  valor: 1840,
  valorStr: 'R$ 1.840,00',
  dataVencimentoStr: '12/11/2026',
  dataPagamentoStr: '21/11/2026',
  forma: 'DINHEIRO',
  status: 'PAGO',
};

describe('caminhoBaixarPagamento', () => {
  it('monta a rota da baixa com o id da cobrança', () => {
    expect(caminhoBaixarPagamento(77)).toBe('/api/pagamentos/77/baixa');
  });
});

describe('baixarPagamento', () => {
  beforeEach(() => {
    patchMock.mockReset();
    patchMock.mockResolvedValue({ data: RESPOSTA });
  });

  it('chama PATCH na rota da baixa com o corpo (forma e confirmação)', async () => {
    await baixarPagamento(77, REQUISICAO);

    expect(patchMock).toHaveBeenCalledWith('/api/pagamentos/77/baixa', REQUISICAO);
  });

  it('devolve a cobrança baixada como veio', async () => {
    const dados = await baixarPagamento(77, REQUISICAO);

    expect(dados).toEqual(RESPOSTA);
  });

  it('repassa o erro do cliente HTTP sem tratar (o diálogo mostra a mensagem do servidor)', async () => {
    const falha = new Error('409');
    patchMock.mockRejectedValueOnce(falha);

    await expect(baixarPagamento(77, REQUISICAO)).rejects.toBe(falha);
  });
});
