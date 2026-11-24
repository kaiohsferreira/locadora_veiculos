import { describe, expect, it } from 'vitest';
import { termosCortadosPresentesRetirada, termosDaCaucaoPresentesRetirada } from '@/features/locacoes/retirada/termosCortadosRetirada';
import { MENSAGENS_RETIRADA as M, mensagemAcaoLinhaRetirada } from './mensagensRetirada';

describe('mensagemAcaoLinhaRetirada (nome da ação da linha para o leitor de tela)', () => {
  it('junta o texto visível do botão e o código da reserva da linha', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, 'AV-00042')).toBe('Iniciar retirada AV-00042');
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, 'AV-00046')).toBe('Continuar para vistoria AV-00046');
  });

  it('linha de locação sem código usa "Locação nº {id}"', () => {
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, M.locacaoNumero(931))).toBe('Continuar para vistoria Locação nº 931');
  });

  it('sem referência (vazia ou só com espaços) não há nome: o link fica com o texto visível', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, '')).toBeUndefined();
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, '   ')).toBeUndefined();
  });

  it('referência com espaços nas pontas não deixa espaço sobrando no nome', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, ' AV-00042 ')).toBe('Iniciar retirada AV-00042');
  });
});

describe('botão de volta dos estados de erro da tela 16', () => {
  it('usa o texto aprovado pelo Kaio em 24/11/2026', () => {
    expect(M.voltarRetiradas).toBe('Voltar às retiradas');
  });
});

describe('textos da caução na tela 16 (locacao.contract.json v4, tela_16_v4.textos)', () => {
  it('os textos do desenho que voltam com a caução', () => {
    expect(M.subtituloComCaucao).toBe('Conferência inicial de documentação, garantias e validação de reserva para entrega das chaves.');
    expect(M.caucaoRotulo).toBe('Garantia / Caução');
    expect(M.itens.titularidadeCartao.titulo).toBe('Titularidade do cartão de crédito');
    expect(M.itens.titularidadeCartao.descricao).toBe('Titular do cartão corresponde ao condutor principal ou responsável financeiro com procuração reconhecida.');
    expect(M.preAutorizacaoTitulo('R$ 1.200,00')).toBe('Pré-autorização da caução (R$ 1.200,00)');
  });

  it('o contador usa o total da lista: /3 sem caução e /4 com caução', () => {
    expect(M.contadorConferidos(0, 3)).toBe('0/3 Confirmados');
    expect(M.contadorConferidos(3, 3)).toBe('3/3 Confirmados');
    expect(M.contadorConferidos(0, 4)).toBe('0/4 Confirmados');
    expect(M.contadorConferidos(4, 4)).toBe('4/4 Confirmados');
  });

  it('os textos novos, aprovados pelo Kaio em 24/11/2026, existem como no contrato', () => {
    expect(M.caucaoSituacaoAoIniciar).toBe('Pré-autorização no cartão do cliente ao iniciar a locação.');
    expect(M.caucaoIndisponivel).toBe('Pré-autorização indisponível: o provedor de pagamento não está liberado para a caução. A locação não pode ser iniciada.');
    expect(M.preAutorizacaoDescricao).toBe('Bloqueio de garantia financeira no cartão de crédito do cliente, feito quando a locação é iniciada.');
    expect(M.simulacaoTitulo).toBe('Simulação do cartão');
    expect(M.simulacaoAviso).toBe('Ambiente de simulação: nenhum valor é reservado em cartão de verdade. Escolha o resultado da pré-autorização.');
    expect(M.simulacaoAprovar).toBe('Aprovar');
    expect(M.simulacaoRecusar).toBe('Recusar');
    expect(M.simulacaoAnalise).toBe('Deixar em análise');
  });

  it('os textos dos ajustes aprovados depois da revisão (complemento encurtado, A4 e A1), exatamente como aprovados', () => {
    expect(M.caucaoTenteNovamente).toBe('Escolha de novo o resultado da simulação e tente outra vez.');
    expect(M.caucaoIndisponivelCurto).toBe('Pré-autorização indisponível.');
    expect(M.caucaoCargoSemPermissao).toBe('Com caução, só atendente ou administrador inicia a locação.');
  });

  it('o complemento encurtado não repete o "A locação não foi iniciada" que os textos do servidor já trazem', () => {
    expect(M.caucaoTenteNovamente).not.toContain('A locação não foi iniciada');
  });

  it('nenhum texto da caução contém termo cortado de sempre (bandeira, final do cartão, NSU, AutoVia Pay, cartão físico)', () => {
    const textos = [
      M.subtituloComCaucao, M.caucaoRotulo, M.caucaoSituacaoAoIniciar, M.caucaoIndisponivel, M.caucaoIndisponivelCurto,
      M.itens.titularidadeCartao.titulo, M.itens.titularidadeCartao.descricao, M.preAutorizacaoTitulo('R$ 1.200,00'),
      M.preAutorizacaoDescricao, M.simulacaoTitulo, M.simulacaoAviso, M.simulacaoAprovar, M.simulacaoRecusar, M.simulacaoAnalise,
      M.caucaoCargoSemPermissao, M.caucaoTenteNovamente, M.contadorConferidos(0, 4),
    ];
    for (const texto of textos) expect(termosCortadosPresentesRetirada(`<p>${texto}</p>`)).toEqual([]);
  });

  it('o subtítulo sem caução não fala da caução (nenhum termo da lista da caução)', () => {
    expect(termosDaCaucaoPresentesRetirada(`<p>${M.subtitulo}</p>`)).toEqual([]);
    expect(termosDaCaucaoPresentesRetirada(`<p>${M.subtituloComCaucao}</p>`)).toEqual(['Garantia']);
  });
});
