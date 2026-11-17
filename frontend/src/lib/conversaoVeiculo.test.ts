import { describe, expect, it } from 'vitest';
import { valoresIniciaisCadastro, valoresIniciaisEdicao, montarRequisicaoVeiculo } from '@/lib/conversaoVeiculo';
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import type { VeiculoRetorno } from '@/types/veiculos';

const PREENCHIDO: ValoresFormularioVeiculo = {
  idCategoria: '2',
  placa: 'bra-2e19',
  chassi: ' 9bwzzz377vt004251 ',
  renavam: '00984521360',
  marca: '  Fiat ',
  modelo: ' Argo Drive 1.0 ',
  anoFabricacao: '2023',
  anoModelo: '2024',
  cor: ' Prata Shark ',
  kmAtual: '12',
  valorDiaria: '229,00',
  valorVenda: '108.200,00',
  dataAquisicao: '2024-10-15',
  fornecedor: '  Jeep Dahruj SP Motors ',
  numeroNotaFiscalCompra: ' NF-e 492.180 ',
};

const VEICULO_DA_API: VeiculoRetorno = {
  idVeiculo: 7,
  idCategoria: 3,
  categoriaNome: 'Grupo G',
  placa: 'BRA2E19',
  chassi: '9BWZZZ377VT004251',
  renavam: '00984521360',
  marca: 'Fiat',
  modelo: 'Argo Drive 1.0',
  anoFabricacao: 2023,
  anoModelo: 2024,
  cor: null,
  kmAtual: 24850,
  valorDiaria: 229,
  valorDiariaStr: 'R$ 229,00',
  valorVenda: 104500,
  valorVendaStr: 'R$ 104.500,00',
  status: 'LOCADO',
  dataAquisicao: '2024-10-15',
  dataAquisicaoStr: '15/10/2024',
  fornecedor: 'Jeep Dahruj SP Motors',
  numeroNotaFiscalCompra: 'NF-e 492.180',
  locadoraNomeFantasia: 'Rota Sul',
  locadoraCidade: 'Curitiba',
  locadoraUf: 'PR',
};

describe('valoresIniciaisCadastro', () => {
  it('nasce com tudo vazio: sem categoria, marca, cor nem valores pré-marcados', () => {
    const valores = valoresIniciaisCadastro();

    expect(Object.values(valores).every((valor) => valor === '')).toBe(true);
    expect(Object.keys(valores)).toHaveLength(15);
  });
});

describe('valoresIniciaisEdicao', () => {
  it('leva o veículo da API para o formulário, com o valor em reais no formato da máscara', () => {
    const valores = valoresIniciaisEdicao(VEICULO_DA_API);

    expect(valores.idCategoria).toBe('3');
    expect(valores.placa).toBe('BRA2E19');
    expect(valores.anoModelo).toBe('2024');
    expect(valores.kmAtual).toBe('24850');
    expect(valores.valorDiaria).toBe('229,00');
    expect(valores.valorVenda).toBe('104.500,00');
    expect(valores.dataAquisicao).toBe('2024-10-15');
  });

  it('cor, venda, data, fornecedor e nota nulos viram texto vazio no formulário', () => {
    const valores = valoresIniciaisEdicao({
      ...VEICULO_DA_API,
      cor: null,
      valorVenda: null,
      dataAquisicao: null,
      fornecedor: null,
      numeroNotaFiscalCompra: null,
    });

    expect(valores.cor).toBe('');
    expect(valores.valorVenda).toBe('');
    expect(valores.dataAquisicao).toBe('');
    expect(valores.fornecedor).toBe('');
    expect(valores.numeroNotaFiscalCompra).toBe('');
  });

  it('leva fornecedor e nota fiscal de compra do veículo da API para o formulário', () => {
    const valores = valoresIniciaisEdicao(VEICULO_DA_API);

    expect(valores.fornecedor).toBe('Jeep Dahruj SP Motors');
    expect(valores.numeroNotaFiscalCompra).toBe('NF-e 492.180');
  });
});

describe('montarRequisicaoVeiculo', () => {
  it('monta os 15 campos do VeiculoVO (v4) com a normalização do servidor e sem status nem empresa', () => {
    const requisicao = montarRequisicaoVeiculo(PREENCHIDO);

    expect(Object.keys(requisicao).sort()).toEqual(
      [
        'anoFabricacao',
        'anoModelo',
        'chassi',
        'cor',
        'dataAquisicao',
        'fornecedor',
        'idCategoria',
        'kmAtual',
        'marca',
        'modelo',
        'numeroNotaFiscalCompra',
        'placa',
        'renavam',
        'valorDiaria',
        'valorVenda',
      ].sort(),
    );
    expect(requisicao).not.toHaveProperty('status');
    expect(requisicao).not.toHaveProperty('idEmpresa');
  });

  it('placa sem hífen e em maiúsculas; chassi em maiúsculas; textos aparados', () => {
    const requisicao = montarRequisicaoVeiculo(PREENCHIDO);

    expect(requisicao.placa).toBe('BRA2E19');
    expect(requisicao.chassi).toBe('9BWZZZ377VT004251');
    expect(requisicao.marca).toBe('Fiat');
    expect(requisicao.modelo).toBe('Argo Drive 1.0');
    expect(requisicao.cor).toBe('Prata Shark');
  });

  it('renavam sai como texto, com os zeros à esquerda', () => {
    expect(montarRequisicaoVeiculo(PREENCHIDO).renavam).toBe('00984521360');
  });

  it('valores em reais viram número: "229,00" vira 229 e "108.200,00" vira 108200', () => {
    const requisicao = montarRequisicaoVeiculo(PREENCHIDO);

    expect(requisicao.valorDiaria).toBe(229);
    expect(requisicao.valorVenda).toBe(108200);
  });

  it('venda vazia vai como nulo; cor vazia vai como texto vazio; data vazia vai como nulo', () => {
    const requisicao = montarRequisicaoVeiculo({ ...PREENCHIDO, valorVenda: '  ', cor: '  ', dataAquisicao: '' });

    expect(requisicao.valorVenda).toBeNull();
    expect(requisicao.cor).toBe('');
    expect(requisicao.dataAquisicao).toBeNull();
  });

  it('quilometragem vazia vai como zero; números inteiros viram número', () => {
    const requisicao = montarRequisicaoVeiculo({ ...PREENCHIDO, kmAtual: '' });

    expect(requisicao.kmAtual).toBe(0);
    expect(requisicao.idCategoria).toBe(2);
    expect(requisicao.anoFabricacao).toBe(2023);
    expect(requisicao.anoModelo).toBe(2024);
  });

  it('data preenchida vai como AAAA-MM-DD', () => {
    expect(montarRequisicaoVeiculo(PREENCHIDO).dataAquisicao).toBe('2024-10-15');
  });

  it('fornecedor e nota fiscal vão aparados; vazios vão como texto vazio (o servidor grava nulo)', () => {
    const requisicao = montarRequisicaoVeiculo(PREENCHIDO);
    const semNada = montarRequisicaoVeiculo({ ...PREENCHIDO, fornecedor: '   ', numeroNotaFiscalCompra: '' });

    expect(requisicao.fornecedor).toBe('Jeep Dahruj SP Motors');
    expect(requisicao.numeroNotaFiscalCompra).toBe('NF-e 492.180');
    expect(semNada.fornecedor).toBe('');
    expect(semNada.numeroNotaFiscalCompra).toBe('');
  });

  it('nunca leva status nem empresa, mesmo com os campos novos', () => {
    const requisicao = montarRequisicaoVeiculo(PREENCHIDO);

    expect(requisicao).not.toHaveProperty('status');
    expect(requisicao).not.toHaveProperty('idEmpresa');
  });
});
