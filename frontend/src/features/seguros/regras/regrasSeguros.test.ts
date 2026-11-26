import { describe, expect, it, vi } from 'vitest';
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
import { normalizarTermoPlaca } from '@/features/seguros/hooks/useBuscaVeiculoPlaca';
import { descreverPrazo, descreverVencimento, iniciaisSeguradora, proporcaoAVencer } from './vencimento';

// O hook da busca por placa importa o cliente HTTP; nenhum teste chama a API.
vi.mock('@/api/client', () => ({ api: { get: vi.fn() } }));

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
  valorPremioStr: 'R$ 3.450,50',
  cobertura: null,
  diasParaVencer: 30,
  venceEm60Dias: true,
};

describe('descreverVencimento (status sempre com texto)', () => {
  it.each([
    [-5, false, 'Vencida há 5 dias', 'perigo'],
    [-1, false, 'Vencida há 1 dia', 'perigo'],
    [0, true, 'Vence hoje', 'perigo'],
    [1, true, 'Vence em breve (1 dia)', 'pendente'],
    [30, true, 'Vence em breve (30 dias)', 'pendente'],
    [60, true, 'Vence em breve (60 dias)', 'pendente'],
  ])('%i dias (venceEm60Dias %s) → "%s"', (dias, venceEm60Dias, texto, tom) => {
    const descricao = descreverVencimento({ diasParaVencer: dias, venceEm60Dias });
    expect(descricao.texto).toBe(texto);
    expect(descricao.tom).toBe(tom);
  });

  it('mais de 60 dias fica "Vigente"', () => {
    expect(descreverVencimento({ diasParaVencer: 90, venceEm60Dias: false })).toEqual({
      texto: 'Vigente',
      tom: 'sucesso',
      icone: 'check_circle',
    });
  });
});

describe('descreverPrazo (embaixo da vigência)', () => {
  it('"Faltam N dias", em destaque só quando vence em até 60 dias', () => {
    expect(descreverPrazo({ diasParaVencer: 31, venceEm60Dias: true })).toEqual({ texto: 'Faltam 31 dias', destaque: true });
    expect(descreverPrazo({ diasParaVencer: 1, venceEm60Dias: true })).toEqual({ texto: 'Falta 1 dia', destaque: true });
    expect(descreverPrazo({ diasParaVencer: 216, venceEm60Dias: false })).toEqual({ texto: 'Faltam 216 dias', destaque: false });
  });

  it('no último dia e depois de vencer, diz isso', () => {
    expect(descreverPrazo({ diasParaVencer: 0, venceEm60Dias: true })).toEqual({ texto: 'Vence hoje', destaque: true });
    expect(descreverPrazo({ diasParaVencer: -3, venceEm60Dias: false })).toEqual({ texto: 'Vencida há 3 dias', destaque: true });
  });
});

describe('iniciaisSeguradora', () => {
  it.each([
    ['Porto Seguro', 'PS'],
    ['Tokio Marine Seguradora', 'TM'],
    ['Mapfre', 'MA'],
    ['  bradesco   auto/re ', 'BA'],
    ['Companhia de Seguros', 'CS'],
    ['', '?'],
  ])('"%s" → %s', (seguradora, iniciais) => {
    expect(iniciaisSeguradora(seguradora)).toBe(iniciais);
  });
});

describe('proporcaoAVencer (barra do cartão dos 60 dias)', () => {
  it('é a parte das cadastradas que vence em até 60 dias', () => {
    expect(proporcaoAVencer(42, 1480)).toBe(3);
    expect(proporcaoAVencer(4, 37)).toBe(11);
    expect(proporcaoAVencer(10, 10)).toBe(100);
  });

  it('sem total (carregando, falha ou zero) fica vazia', () => {
    expect(proporcaoAVencer(undefined, 10)).toBe(0);
    expect(proporcaoAVencer(3, undefined)).toBe(0);
    expect(proporcaoAVencer(0, 0)).toBe(0);
  });
});

describe('normalizarTermoPlaca', () => {
  it('mantém só letras e números, em maiúsculas', () => {
    expect(normalizarTermoPlaca('abc-1d23')).toBe('ABC1D23');
    expect(normalizarTermoPlaca(' abc 1234 ')).toBe('ABC1234');
    expect(normalizarTermoPlaca('ab.c/1')).toBe('ABC1');
  });

  it('sem letra nem número fica vazio (a busca não começa)', () => {
    expect(normalizarTermoPlaca('')).toBe('');
    expect(normalizarTermoPlaca('-- ..')).toBe('');
    expect(normalizarTermoPlaca('ção')).toBe('O');
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
    expect(estado).toEqual({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, veiculo: { idVeiculo: 7, placa: 'ABC1D23' } });
    expect(montarFiltroApolices(estado)).toEqual({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, idVeiculo: 7 });
  });

  it('ignora valores inválidos e escreve só o que está ativo', () => {
    expect(lerFiltrosSeguros(new URLSearchParams('veiculo=abc&pagina=0&aVencer=sim&itens=7'))).toEqual({
      pagina: 1,
      tamanhoPagina: 20,
      apenasAVencer: false,
      veiculo: null,
    });
    expect(escreverFiltrosSeguros({ pagina: 1, tamanhoPagina: 20, apenasAVencer: true, veiculo: null }).toString()).toBe('aVencer=1');
  });

  it('itens por página: lê e escreve 10 e 50; 20 é o padrão e não vai para o endereço', () => {
    const estado = lerFiltrosSeguros(new URLSearchParams('itens=50'));
    expect(estado.tamanhoPagina).toBe(50);
    expect(montarFiltroApolices(estado).tamanhoPagina).toBe(50);
    expect(escreverFiltrosSeguros({ ...estado, tamanhoPagina: 10 }).toString()).toBe('itens=10');
    expect(escreverFiltrosSeguros({ ...estado, tamanhoPagina: 20 }).toString()).toBe('');
  });
});
