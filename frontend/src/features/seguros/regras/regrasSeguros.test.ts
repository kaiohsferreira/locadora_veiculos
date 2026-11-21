import { describe, expect, it } from 'vitest';
import { ErroApi } from '@/types/api';
import type { Apolice } from '@/types/seguros';
import { escreverFiltrosSeguros, lerFiltrosSeguros, montarFiltroApolices } from './enderecoSeguros';
import { interpretarErroApolice } from './errosApolice';
import {
  descricaoVeiculo,
  esquemaApolice,
  montarRequisicaoApolice,
  valoresIniciaisCadastro,
  valoresIniciaisEdicao,
  type ValoresApolice,
} from './formularioApolice';
import { descreverVencimento } from './vencimento';

const APOLICE: Apolice = {
  idSeguro: 3,
  idVeiculo: 7,
  placaVeiculo: 'ABC1D23',
  seguradora: 'Porto Seguro',
  numeroApolice: '123.456-7',
  dataInicio: '2026-01-01',
  dataInicioStr: '01/01/2026',
  dataFim: '2026-12-30',
  dataFimStr: '30/12/2026',
  valorPremio: 3450.5,
  valorPremioStr: '',
  cobertura: null,
  diasParaVencer: 30,
  venceEm60Dias: true,
};

describe('descreverVencimento (sempre com texto)', () => {
  it.each([
    [-5, false, 'Vencida há 5 dias', 'perigo'],
    [-1, false, 'Vencida há 1 dia', 'perigo'],
    [0, true, 'Vence hoje', 'perigo'],
    [1, true, 'Vence em 1 dia', 'pendente'],
    [30, true, 'Vence em 30 dias', 'pendente'],
    [60, true, 'Vence em 60 dias', 'pendente'],
  ])('%i dias (venceEm60Dias %s) → "%s"', (dias, venceEm60Dias, texto, tom) => {
    const descricao = descreverVencimento({ diasParaVencer: dias, venceEm60Dias, dataFimStr: '30/12/2026' });
    expect(descricao.texto).toBe(texto);
    expect(descricao.tom).toBe(tom);
    expect(descricao.detalhe).toBeNull();
  });

  it('mais de 60 dias fica "Em dia", com até quando vale', () => {
    const descricao = descreverVencimento({ diasParaVencer: 90, venceEm60Dias: false, dataFimStr: '30/12/2026' });
    expect(descricao).toEqual({ texto: 'Em dia', tom: 'sucesso', icone: 'check_circle', detalhe: 'até 30/12/2026' });
  });
});

describe('formulário da apólice', () => {
  const VALIDO: ValoresApolice = {
    veiculo: { idVeiculo: 7, placa: 'ABC1D23', marca: 'Jeep', modelo: 'Renegade' },
    seguradora: ' Porto Seguro ',
    numeroApolice: '123.456-7',
    dataInicio: '2026-01-01',
    dataFim: '2026-12-30',
    valorPremio: '3.450,50',
    cobertura: '  ',
  };

  it('aceita o formulário válido e monta o corpo da API', () => {
    expect(esquemaApolice.safeParse(VALIDO).success).toBe(true);
    expect(montarRequisicaoApolice(VALIDO)).toEqual({
      idVeiculo: 7,
      seguradora: 'Porto Seguro',
      numeroApolice: '123.456-7',
      dataInicio: '2026-01-01',
      dataFim: '2026-12-30',
      valorPremio: 3450.5,
      cobertura: null,
    });
  });

  it('vigência invertida ou igual dá erro no fim da vigência', () => {
    const resultado = esquemaApolice.safeParse({ ...VALIDO, dataFim: '2026-01-01' });
    expect(resultado.success).toBe(false);
    const erro = resultado.error?.issues.find((issue) => issue.path[0] === 'dataFim');
    expect(erro?.message).toBe('A data de fim da vigência deve ser posterior à data de início.');
  });

  it('sem veículo, sem seguradora e prêmio zero dão erro', () => {
    const resultado = esquemaApolice.safeParse({ ...valoresIniciaisCadastro(), dataInicio: '2026-01-01', dataFim: '2026-02-01' });
    const campos = resultado.error?.issues.map((issue) => issue.path[0]);
    expect(campos).toEqual(expect.arrayContaining(['veiculo', 'seguradora', 'numeroApolice', 'valorPremio']));
  });

  it('a edição abre com os dados atuais (prêmio com a máscara de reais)', () => {
    const valores = valoresIniciaisEdicao(APOLICE);
    expect(valores.veiculo?.placa).toBe('ABC1D23');
    expect(valores.valorPremio).toBe('3.450,50');
    expect(valores.cobertura).toBe('');
    expect(descricaoVeiculo(valores.veiculo!)).toBe('ABC1D23');
    expect(descricaoVeiculo({ idVeiculo: 1, placa: 'XYZ9A87', marca: 'Fiat', modelo: 'Argo' })).toBe('XYZ9A87 · Fiat Argo');
  });
});

describe('interpretarErroApolice (erros da API em português, no campo certo)', () => {
  it('400 de vigência vai para o fim da vigência', () => {
    const erro = new ErroApi({
      status: 400,
      mensagem: 'A data de fim da vigência deve ser posterior à data de início.',
      erros: { DataFim: ['A data de fim da vigência deve ser posterior à data de início.'] },
    });
    expect(interpretarErroApolice(erro)).toEqual({
      campos: { dataFim: 'A data de fim da vigência deve ser posterior à data de início.' },
      aviso: null,
    });
  });

  it('409 de número repetido vai para o número da apólice', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'Já existe uma apólice ativa com este número.' });
    expect(interpretarErroApolice(erro)).toEqual({ campos: { numeroApolice: 'Já existe uma apólice ativa com este número.' }, aviso: null });
  });

  it('404 do veículo vai para o campo veículo; 404 da apólice vira aviso', () => {
    const veiculo = new ErroApi({ status: 404, mensagem: 'O veículo informado não foi encontrado.' });
    const apolice = new ErroApi({ status: 404, mensagem: 'A apólice informada não foi encontrada.' });
    expect(interpretarErroApolice(veiculo).campos.veiculo).toBe('O veículo informado não foi encontrado.');
    expect(interpretarErroApolice(apolice)).toEqual({ campos: {}, aviso: 'A apólice informada não foi encontrada.' });
  });

  it('falha de conexão vira aviso geral', () => {
    expect(interpretarErroApolice(new ErroApi({ status: 0, mensagem: 'Sem conexão.' }))).toEqual({ campos: {}, aviso: 'Sem conexão.' });
  });
});

describe('endereço da gestão de seguros', () => {
  it('lê veículo, placa, "a vencer" e página', () => {
    const estado = lerFiltrosSeguros(new URLSearchParams('veiculo=7&placa=ABC1D23&aVencer=1&pagina=2'));
    expect(estado).toEqual({ pagina: 2, apenasAVencer: true, veiculo: { idVeiculo: 7, placa: 'ABC1D23' } });
    expect(montarFiltroApolices(estado)).toEqual({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, idVeiculo: 7 });
  });

  it('ignora valores inválidos e escreve só o que está ativo', () => {
    expect(lerFiltrosSeguros(new URLSearchParams('veiculo=abc&pagina=0&aVencer=sim'))).toEqual({
      pagina: 1,
      apenasAVencer: false,
      veiculo: null,
    });
    expect(escreverFiltrosSeguros({ pagina: 1, apenasAVencer: true, veiculo: null }).toString()).toBe('aVencer=1');
  });
});
