import { describe, expect, it, vi } from 'vitest';
import {
  CHAVE_PAGAMENTOS_CLIENTE,
  invalidarCobrancasClientePosPix,
  tratarErroGeracaoPixPagamento,
  type AlvoInvalidacaoPagamentos,
} from '@/features/financeiro/hooks/usePagamentosCliente';
import { ErroApi } from '@/types/api';

// A camada de API carrega o cliente HTTP, que usa `window` ao ser importado: no ambiente de teste ele é trocado por um mock.
vi.mock('@/api/client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));

describe('tratarErroGeracaoPixPagamento (o que o onError do hook faz)', () => {
  it('409 invalida a família de cobranças do cliente, uma vez', () => {
    const invalidateQueries = vi.fn<AlvoInvalidacaoPagamentos['invalidateQueries']>(() => Promise.resolve());
    tratarErroGeracaoPixPagamento(
      { invalidateQueries },
      new ErroApi({ status: 409, mensagem: 'Este pagamento já foi pago. Não é preciso gerar um PIX.' }),
    );
    expect(invalidateQueries).toHaveBeenCalledTimes(1);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_PAGAMENTOS_CLIENTE] });
  });

  it('502 não invalida as cobranças', () => {
    const invalidateQueries = vi.fn<AlvoInvalidacaoPagamentos['invalidateQueries']>(() => Promise.resolve());
    tratarErroGeracaoPixPagamento({ invalidateQueries }, new ErroApi({ status: 502, mensagem: 'Provedor indisponível.' }));
    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});

describe('invalidarCobrancasClientePosPix', () => {
  it('invalida a família de cobranças do cliente', () => {
    const invalidateQueries = vi.fn<AlvoInvalidacaoPagamentos['invalidateQueries']>(() => Promise.resolve());
    invalidarCobrancasClientePosPix({ invalidateQueries });
    expect(invalidateQueries).toHaveBeenCalledTimes(1);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_PAGAMENTOS_CLIENTE] });
  });

  it('não devolve a promise da invalidação, mesmo que o refresh demore (o onSuccess não pode esperar)', () => {
    const refreshPendente = new Promise<unknown>(() => undefined);
    const resultado: unknown = invalidarCobrancasClientePosPix({
      invalidateQueries: () => refreshPendente,
    });
    expect(resultado).toBeUndefined();
  });
});
