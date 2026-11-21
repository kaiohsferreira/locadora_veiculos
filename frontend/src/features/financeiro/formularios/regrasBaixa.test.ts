import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_FINANCEIRO_ERRO_FORMA,
  MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import {
  criarEsquemaBaixa,
  decidirAposErroBaixa,
  ehErroPixGeradoSemConfirmacao,
  montarRequisicaoBaixa,
  VALORES_INICIAIS_BAIXA,
} from './regrasBaixa';

describe('criarEsquemaBaixa (forma obrigatória, lista fechada)', () => {
  it('recusa forma vazia, com a mensagem de escolher a forma', () => {
    const resultado = criarEsquemaBaixa().safeParse(VALORES_INICIAIS_BAIXA);

    expect(resultado.success).toBe(false);
    if (!resultado.success) {
      expect(resultado.error.issues[0]?.message).toBe(MENSAGEM_FINANCEIRO_ERRO_FORMA);
    }
  });

  it('aceita cada forma da lista do contrato', () => {
    for (const forma of ['PIX', 'CARTAO', 'BOLETO', 'DINHEIRO', 'TRANSFERENCIA']) {
      expect(criarEsquemaBaixa().safeParse({ forma, confirmarPix: false }).success).toBe(true);
    }
  });

  it('recusa forma fora da lista', () => {
    expect(criarEsquemaBaixa().safeParse({ forma: 'CHEQUE', confirmarPix: false }).success).toBe(false);
  });
});

describe('montarRequisicaoBaixa (corpo do PATCH)', () => {
  it('leva a forma e a confirmação do PIX como o contrato pede', () => {
    expect(montarRequisicaoBaixa({ forma: 'DINHEIRO', confirmarPix: true })).toEqual({
      forma: 'DINHEIRO',
      confirmarBaixaComPixGerado: true,
    });
  });

  it('sem a caixa marcada, a confirmação vai falsa', () => {
    expect(montarRequisicaoBaixa({ forma: 'PIX', confirmarPix: false })).toEqual({
      forma: 'PIX',
      confirmarBaixaComPixGerado: false,
    });
  });
});

describe('decidirAposErroBaixa (o que o diálogo mostra depois de uma recusa)', () => {
  it('409 com o PIX gerado: mostra a caixa e a mensagem do servidor', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO });
    expect(decidirAposErroBaixa(erro)).toEqual({
      mostrarCaixaPix: true,
      mensagem: MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO,
    });
  });

  it('409 de outro motivo (pagamento já pago): sem caixa, com a mensagem do servidor', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Este pagamento já foi baixado.' });
    expect(decidirAposErroBaixa(erro)).toEqual({ mostrarCaixaPix: false, mensagem: 'Este pagamento já foi baixado.' });
  });

  it('502 do cancelamento do PIX: sem caixa, com a mensagem do servidor', () => {
    const erro = new ErroApi({ status: 502, mensagem: 'Não foi possível cancelar o PIX. Tente novamente.' });
    expect(decidirAposErroBaixa(erro)).toEqual({
      mostrarCaixaPix: false,
      mensagem: 'Não foi possível cancelar o PIX. Tente novamente.',
    });
  });

  it('erro de conexão (sem resposta): sem caixa, com a mensagem de conexão recebida', () => {
    const erro = new ErroApi({ status: 0, mensagem: 'Falha de conexão.' });
    expect(decidirAposErroBaixa(erro)).toEqual({ mostrarCaixaPix: false, mensagem: 'Falha de conexão.' });
  });

  it('erro que não é da API: sem caixa, com a mensagem padrão', () => {
    expect(decidirAposErroBaixa(new Error('falha'))).toEqual({
      mostrarCaixaPix: false,
      mensagem: MENSAGEM_ERRO_API_PADRAO,
    });
  });
});

describe('ehErroPixGeradoSemConfirmacao (quando mostrar a caixa do PIX)', () => {
  it('é verdadeiro para o 409 com o texto do servidor do PIX gerado', () => {
    const erro = new ErroApi({ status: 409, mensagem: MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO });

    expect(ehErroPixGeradoSemConfirmacao(erro)).toBe(true);
  });

  it('é falso para outro 409 (ex.: pagamento já baixado)', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Este pagamento já foi baixado.' });

    expect(ehErroPixGeradoSemConfirmacao(erro)).toBe(false);
  });

  it('é falso para 502 com o mesmo texto (falha do cancelamento no provedor)', () => {
    const erro = new ErroApi({ status: 502, mensagem: MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO });

    expect(ehErroPixGeradoSemConfirmacao(erro)).toBe(false);
  });

  it('é falso para erro que não é da API', () => {
    expect(ehErroPixGeradoSemConfirmacao(new Error('falha'))).toBe(false);
    expect(ehErroPixGeradoSemConfirmacao(undefined)).toBe(false);
  });
});
