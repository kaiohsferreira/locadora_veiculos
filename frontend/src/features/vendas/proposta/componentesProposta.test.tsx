import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { formatarMoeda } from '@/lib/formatters';
import type { PropostaCompra } from '@/types/vendas';
import {
  CAMPOS_SIMULACAO_INICIAIS,
  camposValidos,
  converterCampos,
  mascararParcelas,
  validarCampos,
} from './camposSimulacao';
import { ConfirmacaoProposta } from './ConfirmacaoProposta';
import { lerIdVeiculo, textoAnos, textoCidadeUf } from './textosProposta';

const PROPOSTA: PropostaCompra = {
  idPropostaCompra: 11,
  idVeiculo: 4,
  marca: 'Jeep',
  modelo: 'Renegade',
  anoModelo: 2023,
  status: 'ENVIADA',
  dataProposta: '2026-11-21T15:30:00',
  valorVenda: 10000,
  valorVendaStr: '',
  valorEntrada: 0,
  valorEntradaStr: '',
  valorFinanciado: 10000,
  valorFinanciadoStr: '',
  numeroParcelas: 3,
  valorParcela: 3333.33,
  valorParcelaStr: '',
  valorUltimaParcela: 3333.34,
  valorUltimaParcelaStr: '',
};

describe('camposSimulacao', () => {
  it('começa com entrada R$ 0,00 e 12 parcelas, sem erro', () => {
    expect(converterCampos(CAMPOS_SIMULACAO_INICIAIS)).toEqual({ valorEntrada: 0, numeroParcelas: 12 });
    expect(camposValidos(validarCampos(CAMPOS_SIMULACAO_INICIAIS, 10000))).toBe(true);
  });

  it('converte a entrada mascarada em reais', () => {
    expect(converterCampos({ entrada: '20.000,50', parcelas: '60' })).toEqual({ valorEntrada: 20000.5, numeroParcelas: 60 });
  });

  it('entrada vazia, maior ou igual ao valor e parcelas fora de 1..60 dão erro no campo certo', () => {
    expect(validarCampos({ entrada: '', parcelas: '12' }, 10000).entrada).toBeDefined();
    expect(validarCampos({ entrada: '10.000,00', parcelas: '12' }, 10000).entrada).toBe(
      'A entrada deve ser menor que o valor de venda do veículo.',
    );
    expect(validarCampos({ entrada: '0,00', parcelas: '0' }, 10000).parcelas).toBe('O número de parcelas deve ficar entre 1 e 60.');
    expect(validarCampos({ entrada: '0,00', parcelas: '61' }, 10000).parcelas).toBeDefined();
    expect(validarCampos({ entrada: '0,00', parcelas: '' }, 10000).parcelas).toBeDefined();
  });

  it('o campo de parcelas guarda só dois dígitos', () => {
    expect(mascararParcelas('1a23')).toBe('12');
  });
});

describe('textosProposta', () => {
  it('anos iguais viram um só; diferentes, "fabricação/modelo"', () => {
    expect(textoAnos(2023, 2023)).toBe('2023');
    expect(textoAnos(2022, 2023)).toBe('2022/2023');
  });

  it('cidade e UF sem barra solta', () => {
    expect(textoCidadeUf('Campinas', 'SP')).toBe('Campinas/SP');
    expect(textoCidadeUf('', 'SP')).toBe('SP');
    expect(textoCidadeUf('', '')).toBe('');
  });

  it('id da rota só aceita inteiro positivo', () => {
    expect(lerIdVeiculo('4')).toBe(4);
    expect(lerIdVeiculo('0')).toBeNull();
    expect(lerIdVeiculo('-1')).toBeNull();
    expect(lerIdVeiculo('4.5')).toBeNull();
    expect(lerIdVeiculo(undefined)).toBeNull();
  });
});

describe('ConfirmacaoProposta', () => {
  function renderizar(proposta: PropostaCompra, locadora: string): string {
    return renderToStaticMarkup(
      <MemoryRouter>
        <ConfirmacaoProposta proposta={proposta} locadora={locadora} />
      </MemoryRouter>,
    );
  }

  it('mostra os números gravados pelo servidor, a situação e os caminhos de saída', () => {
    const html = renderizar(PROPOSTA, 'Locadora Um');

    expect(html).toContain('Proposta enviada!');
    expect(html).toContain('Locadora Um vai analisar sua proposta e entrar em contato.');
    expect(html).toContain('Jeep Renegade 2023');
    expect(html).toContain(`3 × ${formatarMoeda(3333.33)}`);
    expect(html).toContain(formatarMoeda(3333.34));
    expect(html).toContain('Enviada');
    expect(html).toContain('Ver outros seminovos');
    expect(html).toContain('Voltar ao início');
  });

  it('sem nome da locadora usa "A locadora" e esconde a última parcela quando é igual', () => {
    const html = renderizar({ ...PROPOSTA, valorParcela: 5000, valorUltimaParcela: 5000 }, '');

    expect(html).toContain('A locadora vai analisar');
    expect(html).not.toContain('Última parcela');
  });
});
