import { describe, expect, it } from 'vitest';
import {
  aplicarMudancaFiltro,
  chaveDaGeracaoPix,
  cobrancaParaGaveta,
  deveRecarregarCobrancasAposErroPix,
  destinoBotaoPagarResumo,
  devePedirPixAoAbrir,
  estadoGavetaPix,
  FILTRO_INICIAL_PAGAMENTOS_CLIENTE,
  formatarContagemPix,
  opcoesPeriodo,
  pixExpirou,
  podePagarCobranca,
  podePagarNaLinha,
  proximaContagemPix,
  rotuloForma,
  rotuloOrigem,
  subtextoVencimento,
  temFiltroAplicado,
  textoFaixaPendencias,
  ultimaPaginaPagamentos,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import { mensagemCobrancasQuitadas } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { MeuPagamento } from '@/types/pagamentos';

function pagamento(parcial: Partial<MeuPagamento>): MeuPagamento {
  return {
    idPagamento: 77,
    tipo: 'DIARIA',
    valor: 395.6,
    valorStr: 'R$ 395,60',
    dataVencimento: '2026-11-22',
    dataVencimentoStr: '22/11/2026',
    dataPagamentoStr: null,
    diasParaVencer: 3,
    situacao: 'PENDENTE',
    forma: null,
    idLocacao: 123,
    idVenda: null,
    veiculoDescricao: 'Jeep Renegade Longitude',
    ...parcial,
  };
}

describe('faixa de pendência', () => {
  it('sem pendente não há texto (a faixa some)', () => {
    expect(textoFaixaPendencias(0)).toBeNull();
  });

  it('1 cobrança no singular e N no plural', () => {
    expect(textoFaixaPendencias(1)).toBe('Você possui 1 cobrança pendente.');
    expect(textoFaixaPendencias(3)).toBe('Você possui 3 cobranças pendentes.');
  });
});

describe('cartão de quitadas', () => {
  it('singular com 1 e plural com N', () => {
    expect(mensagemCobrancasQuitadas(1)).toBe('1 cobrança quitada');
    expect(mensagemCobrancasQuitadas(4)).toBe('4 cobranças quitadas');
  });
});

describe('subtexto do vencimento', () => {
  it('pendente: hoje, amanhã e N dias', () => {
    expect(subtextoVencimento(pagamento({ diasParaVencer: 0 }))).toBe('Vence hoje');
    expect(subtextoVencimento(pagamento({ diasParaVencer: 1 }))).toBe('Vence amanhã');
    expect(subtextoVencimento(pagamento({ diasParaVencer: 4 }))).toBe('Vence em 4 dias');
  });

  it('atrasado: vencida há N dias (singular com 1)', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'ATRASADO', diasParaVencer: -3 }))).toBe('Vencida há 3 dias');
    expect(subtextoVencimento(pagamento({ situacao: 'ATRASADO', diasParaVencer: -1 }))).toBe('Vencida há 1 dia');
  });

  it('pago: com a forma usada, e nada sem forma', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: 'PIX' }))).toBe('Pago via PIX');
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: 'CARTAO' }))).toBe('Pago via Cartão');
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: null }))).toBeNull();
  });

  it('cancelado não tem subtexto', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'CANCELADO' }))).toBeNull();
  });

  it('caução pendente com vencimento passado não tem subtexto (não é dívida)', () => {
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', diasParaVencer: -12 }))).toBeNull();
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', diasParaVencer: 3 }))).toBeNull();
  });

  it('caução paga com forma mostra Pago via', () => {
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PAGO', forma: 'CARTAO' }))).toBe('Pago via Cartão');
  });
});

describe('botão de pagar da linha', () => {
  it('aparece só em pendente ou atrasada que não seja caução', () => {
    expect(podePagarNaLinha(pagamento({ situacao: 'PENDENTE' }))).toBe(true);
    expect(podePagarNaLinha(pagamento({ situacao: 'ATRASADO', tipo: 'COBRANCA_EXTRA' }))).toBe(true);
    expect(podePagarNaLinha(pagamento({ situacao: 'PENDENTE', tipo: 'CAUCAO' }))).toBe(false);
    expect(podePagarNaLinha(pagamento({ situacao: 'PAGO' }))).toBe(false);
    expect(podePagarNaLinha(pagamento({ situacao: 'CANCELADO' }))).toBe(false);
  });
});

describe('podePagarCobranca (botões de pagar ligados só quando elegível)', () => {
  it('PENDENTE e ATRASADO de diária, cobrança extra ou venda podem ser pagas', () => {
    expect(podePagarCobranca('PENDENTE', 'DIARIA')).toBe(true);
    expect(podePagarCobranca('ATRASADO', 'COBRANCA_EXTRA')).toBe(true);
    expect(podePagarCobranca('PENDENTE', 'VENDA')).toBe(true);
  });

  it('caução, paga e cancelada nunca podem ser pagas', () => {
    expect(podePagarCobranca('PENDENTE', 'CAUCAO')).toBe(false);
    expect(podePagarCobranca('ATRASADO', 'CAUCAO')).toBe(false);
    expect(podePagarCobranca('PAGO', 'DIARIA')).toBe(false);
    expect(podePagarCobranca('CANCELADO', 'DIARIA')).toBe(false);
  });
});

describe('destinoBotaoPagarResumo (faixa e cartão de pendentes)', () => {
  it('nenhuma pendente: sem destino', () => {
    expect(destinoBotaoPagarResumo(0)).toBe('NENHUM');
    expect(destinoBotaoPagarResumo(-1)).toBe('NENHUM');
  });

  it('uma pendente: abre a gaveta dela', () => {
    expect(destinoBotaoPagarResumo(1)).toBe('COBRANCA_UNICA');
  });

  it('várias pendentes: leva à aba Pendentes', () => {
    expect(destinoBotaoPagarResumo(2)).toBe('ABA_PENDENTES');
    expect(destinoBotaoPagarResumo(7)).toBe('ABA_PENDENTES');
  });
});

describe('contagem regressiva do PIX', () => {
  it('formata mm:ss com zeros à esquerda nos limites', () => {
    expect(formatarContagemPix(900)).toBe('15:00');
    expect(formatarContagemPix(899)).toBe('14:59');
    expect(formatarContagemPix(60)).toBe('01:00');
    expect(formatarContagemPix(59)).toBe('00:59');
    expect(formatarContagemPix(1)).toBe('00:01');
    expect(formatarContagemPix(3600)).toBe('60:00');
  });

  it('zero, negativo e valor inválido viram 00:00', () => {
    expect(formatarContagemPix(0)).toBe('00:00');
    expect(formatarContagemPix(-5)).toBe('00:00');
    expect(formatarContagemPix(Number.NaN)).toBe('00:00');
  });

  it('um segundo a menos por passo, sem passar de zero', () => {
    expect(proximaContagemPix(900)).toBe(899);
    expect(proximaContagemPix(1)).toBe(0);
    expect(proximaContagemPix(0)).toBe(0);
    expect(proximaContagemPix(-3)).toBe(0);
    expect(proximaContagemPix(Number.NaN)).toBe(0);
  });

  it('expira quando chega a zero, e não antes', () => {
    expect(pixExpirou(1)).toBe(false);
    expect(pixExpirou(0)).toBe(true);
    expect(pixExpirou(-1)).toBe(true);
    expect(pixExpirou(Number.NaN)).toBe(true);
  });
});

describe('estado da gaveta de PIX', () => {
  it('erro vence qualquer outra situação', () => {
    expect(estadoGavetaPix(true, true, true)).toBe('ERRO');
    expect(estadoGavetaPix(true, false, true)).toBe('ERRO');
  });

  it('enquanto espera, ou sem cobrança ainda, fica gerando', () => {
    expect(estadoGavetaPix(false, true, false)).toBe('GERANDO');
    expect(estadoGavetaPix(false, true, true)).toBe('GERANDO');
    expect(estadoGavetaPix(false, false, false)).toBe('GERANDO');
  });

  it('só vira pronto com a cobrança devolvida e sem espera', () => {
    expect(estadoGavetaPix(false, false, true)).toBe('PRONTO');
  });
});

describe('guarda de uma chamada por abertura da gaveta', () => {
  it('pede na primeira vez e não pede de novo na mesma abertura', () => {
    expect(devePedirPixAoAbrir(false)).toBe(true);
    expect(devePedirPixAoAbrir(true)).toBe(false);
  });
});

describe('recarga das cobranças depois de erro ao gerar o PIX', () => {
  function erroDaApi(status: number): ErroApi {
    return new ErroApi({ status, mensagem: 'Mensagem de teste.' });
  }

  it('409 (a cobrança mudou de situação no servidor) recarrega as cobranças', () => {
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(409))).toBe(true);
  });

  it('502, 429, 404 e 401 não recarregam', () => {
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(502))).toBe(false);
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(429))).toBe(false);
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(404))).toBe(false);
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(401))).toBe(false);
  });

  it('erro de rede (sem status) não recarrega', () => {
    expect(deveRecarregarCobrancasAposErroPix(erroDaApi(0))).toBe(false);
  });

  it('erro comum, nulo e indefinido não recarregam', () => {
    expect(deveRecarregarCobrancasAposErroPix(new Error('falha'))).toBe(false);
    expect(deveRecarregarCobrancasAposErroPix(null)).toBe(false);
    expect(deveRecarregarCobrancasAposErroPix(undefined)).toBe(false);
  });
});

describe('chave da geração (reinicia a contagem quando chega um PIX novo)', () => {
  it('cada envio tem uma chave diferente, mesmo que o PIX volte com a mesma validade', () => {
    expect(chaveDaGeracaoPix(1000)).not.toBe(chaveDaGeracaoPix(2000));
  });

  it('o mesmo envio mantém a mesma chave (a contagem não recomeça à toa)', () => {
    expect(chaveDaGeracaoPix(1000)).toBe(chaveDaGeracaoPix(1000));
  });

  it('a chave de um envio que ainda não aconteceu (zero) é estável', () => {
    expect(chaveDaGeracaoPix(0)).toBe(chaveDaGeracaoPix(0));
  });
});

describe('busca da cobrança única (Pagar Fatura e Regularizar)', () => {
  it('busca vazia (paga no meio do caminho) não abre gaveta', () => {
    expect(cobrancaParaGaveta(null)).toBeNull();
  });

  it('cobrança pendente ou atrasada abre a gaveta dela', () => {
    const pendente = pagamento({ situacao: 'PENDENTE' });
    const atrasada = pagamento({ situacao: 'ATRASADO', tipo: 'COBRANCA_EXTRA' });
    expect(cobrancaParaGaveta(pendente)).toBe(pendente);
    expect(cobrancaParaGaveta(atrasada)).toBe(atrasada);
  });

  it('cobrança paga, cancelada ou de caução não abre gaveta', () => {
    expect(cobrancaParaGaveta(pagamento({ situacao: 'PAGO' }))).toBeNull();
    expect(cobrancaParaGaveta(pagamento({ situacao: 'CANCELADO' }))).toBeNull();
    expect(cobrancaParaGaveta(pagamento({ situacao: 'PENDENTE', tipo: 'CAUCAO' }))).toBeNull();
  });
});

describe('rótulo da origem', () => {
  it('locação, venda e nenhuma origem', () => {
    expect(rotuloOrigem(pagamento({ idLocacao: 123, idVenda: null }))).toBe('Locação nº 123');
    expect(rotuloOrigem(pagamento({ idLocacao: null, idVenda: 45 }))).toBe('Venda nº 45');
    expect(rotuloOrigem(pagamento({ idLocacao: null, idVenda: null }))).toBeNull();
  });
});

describe('forma de pagamento', () => {
  it('rótulo da lista fechada e nulo para o resto', () => {
    expect(rotuloForma('PIX')).toBe('PIX');
    expect(rotuloForma('BOLETO')).toBe('Boleto');
    expect(rotuloForma(null)).toBeNull();
    expect(rotuloForma('CHEQUE')).toBeNull();
  });
});

describe('mudança de filtro', () => {
  it('trocar aba volta para a página 1', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, pagina: 4 };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'aba', aba: 'PENDENTES' })).toMatchObject({ aba: 'PENDENTES', pagina: 1 });
  });

  it('trocar período volta para a página 1', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, pagina: 2 };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'periodo', periodo: 'TUDO' })).toMatchObject({ periodo: 'TUDO', pagina: 1 });
  });

  it('trocar só a página mantém aba e período', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, aba: 'PAGOS' as const };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'pagina', pagina: 3 })).toMatchObject({ aba: 'PAGOS', pagina: 3 });
  });

  it('filtro inicial é Todos com 12 meses e não conta como filtro aplicado', () => {
    expect(FILTRO_INICIAL_PAGAMENTOS_CLIENTE).toMatchObject({ aba: 'TODOS', periodo: 'DOZE_MESES', pagina: 1 });
    expect(temFiltroAplicado(FILTRO_INICIAL_PAGAMENTOS_CLIENTE)).toBe(false);
    expect(temFiltroAplicado({ ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, aba: 'PENDENTES' })).toBe(true);
    expect(temFiltroAplicado({ ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, periodo: 'TUDO' })).toBe(true);
  });
});

describe('seletor de período e paginação', () => {
  it('opções na ordem do desenho, com o ano no Ano Atual', () => {
    expect(opcoesPeriodo(2026)).toEqual([
      { valor: 'DOZE_MESES', rotulo: 'Últimos 12 meses' },
      { valor: 'ANO_ATUAL', rotulo: 'Ano Atual (2026)' },
      { valor: 'SEIS_MESES', rotulo: 'Últimos 6 meses' },
      { valor: 'TUDO', rotulo: 'Histórico Completo' },
    ]);
  });

  it('última página existente, com pelo menos 1', () => {
    expect(ultimaPaginaPagamentos(0, 10)).toBe(1);
    expect(ultimaPaginaPagamentos(10, 10)).toBe(1);
    expect(ultimaPaginaPagamentos(21, 10)).toBe(3);
  });
});
