import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CONTRATO_ASSINADO, CONTRATO_PENDENTE, HASH_EXEMPLO } from './contratoLocacao.fixture';
import { CSS_IMPRESSAO_CONTRATO, EstiloImpressaoContrato, PapelContrato } from './PapelContrato';

describe('PapelContrato', () => {
  it('mostra o documento pendente com o cabeçalho, o veículo e as cláusulas do servidor', () => {
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    expect(html).toContain('CONTRATO DE LOCAÇÃO DE VEÍCULOS Nº 15');
    expect(html).toContain('AUTOVIA LOCADORA DE VEÍCULOS S.A.');
    expect(html).toContain('Jeep Renegade Longitude 1.3T');
    expect(html).toContain('BRA2E19');
    expect(html).toContain('Placa:');
    expect(html).toContain('CLÁUSULA 1ª – Objeto da locação');
    expect(html).toContain('CLÁUSULA 8ª – Natureza do registro');
    expect(html).toContain('id="documento-contrato"');
    expect(html).toContain('Imprimir contrato');
  });

  it('não mostra hash antes da assinatura e mostra depois', () => {
    expect(renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />)).not.toContain('Hash SHA-256:');
    const assinado = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_ASSINADO} />);
    expect(assinado).toContain(`Hash SHA-256: ${HASH_EXEMPLO}`);
    expect(assinado).toContain('Contrato assinado');
  });

  it('não traz textos cortados do desenho (selo de totem, ICP-Brasil de selo, prefixo LOC-)', () => {
    // A cláusula 8 aprovada diz "Não tem certificação ICP-Brasil"; o que foi cortado do desenho é o selo "Válida via ICP-Brasil".
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    for (const trecho of [
      'Totem',
      'Válida via ICP-Brasil',
      'LOC-2024',
      'Minuta Validada',
      'Folha 1 de 2',
      'Terminal Ativo',
      'Token',
      'SMS',
      'WhatsApp',
      'Despachar',
      'SSL',
      'térmica',
      'Agente de Entrega',
    ]) {
      expect(html).not.toContain(trecho);
    }
    expect(html).toContain('Não tem certificação ICP-Brasil.');
  });

  it('mostra o título da cláusula em caixa alta e a cláusula 1 no fundo próprio', () => {
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    expect(html).toContain('uppercase text-navy-profundo">CLÁUSULA 1ª – Objeto da locação');
    expect(html).toContain('rounded-lg bg-steel-fundo/50 p-2');
  });

  it('mostra o quadro de valores previstos dentro da cláusula de valores, com a frase das extras', () => {
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    expect(html).toContain('O valor previsto da locação é de R$ 916,00, assim discriminado:');
    expect(html).toContain('4 diárias × R$ 229,00');
    expect(html).toContain('Total previsto');
    expect(html).toContain('não estão incluídas no total previsto.');
    expect(html.indexOf('O valor previsto')).toBeGreaterThan(html.indexOf('CLÁUSULA 3ª – Valores'));
    expect(html.indexOf('O valor previsto')).toBeLessThan(html.indexOf('CLÁUSULA 4ª'));
  });

  it('mostra o quadro com uma diária no singular', () => {
    const umaDiaria = {
      ...CONTRATO_PENDENTE,
      documento: {
        ...CONTRATO_PENDENTE.documento,
        locacao: { ...CONTRATO_PENDENTE.documento.locacao, diariasPrevistas: 1, valorTotalPrevistoStr: 'R$ 229,00' },
      },
    };
    const html = renderToStaticMarkup(<PapelContrato contrato={umaDiaria} />);
    expect(html).toContain('1 diária × R$ 229,00');
    expect(html).toContain('O valor previsto da locação é de R$ 229,00, assim discriminado:');
  });

  it('quebra o chassi e o renavam dentro da célula, sem mudar a grade', () => {
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    expect(html).toContain('min-w-0');
    expect(html).toContain('break-all text-dado-tabular font-semibold text-texto-desenho">9883569X7M483921');
    expect(html).toContain('sm:grid-cols-4');
  });

  it('o quadro de valores fica dentro do papel, então a impressão do papel o inclui', () => {
    const html = renderToStaticMarkup(<PapelContrato contrato={CONTRATO_PENDENTE} />);
    expect(html.indexOf('id="documento-contrato"')).toBeLessThan(html.indexOf('O valor previsto'));
  });

  it('imprime só o papel: o bloco de impressão esconde o resto e mostra o documento', () => {
    const html = renderToStaticMarkup(<EstiloImpressaoContrato />);
    expect(html).toContain('@media print');
    expect(html).toContain('#documento-contrato');
    expect(CSS_IMPRESSAO_CONTRATO).toContain('visibility: hidden');
  });
});
