import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import type { PropostaCompra, SimulacaoParcelamento } from '@/types/vendas';
import {
  CAMPOS_SIMULACAO_INICIAIS,
  camposValidos,
  converterCampos,
  mascararParcelas,
  validarCampos,
  type CamposSimulacao,
} from './camposSimulacao';
import { ConfirmacaoProposta } from './ConfirmacaoProposta';
import { SimuladorProposta, type SimuladorPropostaProps } from './SimuladorProposta';
import { lerIdVeiculo, separarCentavos, textoAnos, textoCidadeUf, textoReaisInteiros } from './textosProposta';

// Textos como o servidor manda (StaticMethods.FormateToMonetary): a tela mostra estes, sem formatar de novo.
const PROPOSTA: PropostaCompra = {
  idPropostaCompra: 11,
  idVeiculo: 4,
  marca: 'Jeep',
  modelo: 'Renegade',
  anoModelo: 2023,
  status: 'ENVIADA',
  dataProposta: '2026-11-21T15:30:00',
  dataPropostaStr: '21/11/2026 15:30',
  valorVenda: 10000,
  valorVendaStr: 'R$ 10.000,00',
  valorEntrada: 1000,
  valorEntradaStr: 'R$ 1.000,00',
  valorFinanciado: 9000,
  valorFinanciadoStr: 'R$ 9.000,00',
  numeroParcelas: 7,
  valorParcela: 1285.71,
  valorParcelaStr: 'R$ 1.285,71',
  valorUltimaParcela: 1285.74,
  valorUltimaParcelaStr: 'R$ 1.285,74',
};

const SIMULACAO: SimulacaoParcelamento = {
  idVeiculo: 4,
  valorVenda: 10000,
  valorVendaStr: 'R$ 10.000,00',
  valorEntrada: 0,
  valorEntradaStr: 'R$ 0,00',
  valorFinanciado: 10000,
  valorFinanciadoStr: 'R$ 10.000,00',
  numeroParcelas: 12,
  valorParcela: 833.33,
  valorParcelaStr: 'R$ 833,33',
  valorUltimaParcela: 833.37,
  valorUltimaParcelaStr: 'R$ 833,37',
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
  it('o ano é sempre fabricação/modelo, mesmo quando são iguais', () => {
    expect(textoAnos(2023, 2023)).toBe('2023/2023');
    expect(textoAnos(2022, 2023)).toBe('2022/2023');
    expect(textoAnos(2023, 2024, ' / ')).toBe('2023 / 2024');
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

  it('separa os centavos do texto do servidor para o preço grande', () => {
    expect(separarCentavos('R$ 104.500,00')).toEqual({ inteiro: 'R$ 104.500', centavos: ',00' });
    expect(separarCentavos('R$ 0')).toEqual({ inteiro: 'R$ 0', centavos: '' });
  });

  it('reais sem centavos para os atalhos da entrada', () => {
    expect(textoReaisInteiros(10450)).toBe('R$ 10.450');
    expect(textoReaisInteiros(0)).toBe('R$ 0');
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

  it('mostra o código, os textos gravados pelo servidor, a data do servidor, a situação e as saídas', () => {
    const html = renderizar(PROPOSTA, 'Locadora Um');

    expect(html).toContain('Proposta enviada!');
    expect(html).toContain('Locadora Um vai analisar sua proposta e entrar em contato.');
    expect(html).toContain('Código da proposta');
    expect(html).toContain('#11');
    expect(html).toContain('Jeep Renegade 2023');
    expect(html).toContain('R$ 1.000,00');
    expect(html).toContain('R$ 9.000,00');
    expect(html).toContain('7x de R$ 1.285,71');
    expect(html).toContain('R$ 1.285,74');
    expect(html).toContain('21/11/2026 15:30');
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

describe('SimuladorProposta', () => {
  function renderizar(parcial: Partial<SimuladorPropostaProps> = {}, campos: CamposSimulacao = CAMPOS_SIMULACAO_INICIAIS): string {
    const props: SimuladorPropostaProps = {
      veiculo: { valorVenda: 104500, valorVendaStr: 'R$ 104.500,00' },
      campos,
      erros: {},
      aoMudar: vi.fn(),
      simulacao: SIMULACAO,
      calculando: false,
      enviando: false,
      podeEnviar: true,
      aoEnviar: vi.fn(),
      ...parcial,
    };
    return renderToStaticMarkup(<SimuladorProposta {...props} />);
  }

  it('mostra o valor negociado travado com o texto do servidor', () => {
    const html = renderizar();

    expect(html).toContain('Valor Negociado do Veículo');
    expect(html).toMatch(/id="proposta-valor-negociado"[^>]*readonly=""[^>]*value="R\$ 104\.500,00"/);
  });

  it('os atalhos de 10, 20, 30 e 50% trazem o valor em reais; o da entrada atual fica marcado', () => {
    const html = renderizar({}, { entrada: '31.350,00', parcelas: '12' });

    expect(html).toContain('10% (R$ 10.450)');
    expect(html).toContain('20% (R$ 20.900)');
    expect(html).toContain('50% (R$ 52.250)');
    expect(html).toMatch(/aria-pressed="true"[^>]*>30% \(R\$ 31\.350\)/);
    expect(html).toMatch(/aria-pressed="false"[^>]*>10%/);
  });

  it('a barra vai de zero até abaixo do valor do veículo e mostra a entrada e a porcentagem', () => {
    const html = renderizar({}, { entrada: '31.350,00', parcelas: '12' });

    expect(html).toContain('type="range"');
    expect(html).toContain('max="104400"');
    expect(html).toContain('R$ 0 (Sem entrada)');
    expect(html).toContain('R$ 31.350 (30%)');
    expect(html).toContain('R$ 104.400 (Máx)');
  });

  it('os cartões vão de 12x a 60x e o das parcelas atuais fica marcado', () => {
    const html = renderizar({}, { entrada: '0,00', parcelas: '48' });

    for (const opcao of ['12x', '24x', '36x', '48x', '60x']) expect(html).toContain(opcao);
    expect(html).toContain('1 ano');
    expect(html).toContain('5 anos');
    expect(html).toMatch(/checked="" value="48"/);
    expect(html).not.toMatch(/checked="" value="12"/);
  });

  it('o resultado usa os textos do servidor, com o destaque "Nx de R$ ..." e a última parcela diferente', () => {
    const html = renderizar();

    expect(html).toContain('Resultado da Simulação');
    expect(html).toContain('Saldo Financiado');
    expect(html).toContain('R$ 10.000,00');
    expect(html).toContain('12x de R$ 833,33');
    expect(html).toContain('Última parcela: R$ 833,37');
    expect(html).toContain('text-orange');
  });

  it('não tem os blocos de juros do desenho', () => {
    const html = renderizar();

    for (const texto of ['Taxa', 'CET', 'Carência', 'IOF', 'com juros']) expect(html).not.toContain(texto);
  });

  it('calculando: mostra "Calculando…", esconde o resultado anterior e desliga o envio', () => {
    const html = renderizar({ calculando: true, simulacao: null, podeEnviar: false });

    expect(html).toContain('Calculando…');
    expect(html).not.toContain('R$ 833,33');
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>.*Enviar Proposta de Compra/s);
  });

  it('com erro, mostra a mensagem num alerta', () => {
    const html = renderizar({ erro: 'Veículo não encontrado ou não está à venda.', simulacao: null, podeEnviar: false });

    expect(html).toContain('role="alert"');
    expect(html).toContain('Veículo não encontrado ou não está à venda.');
  });

  it('com erro no campo da entrada, mostra o erro ligado ao campo', () => {
    const html = renderizar({ erros: { entrada: 'A entrada deve ser menor que o valor de venda do veículo.' } });

    expect(html).toContain('aria-invalid="true"');
    expect(html).toContain('id="proposta-entrada-erro"');
  });
});
