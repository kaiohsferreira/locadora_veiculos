import { describe, expect, it } from 'vitest';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { ErroApi } from '@/types/api';
import {
  avaliarPeriodo, BUSCA_VAZIA, errosBuscaDaApi, hojeLocal, lerBuscaDaUrl,
  montarFiltroBusca, montarParametrosBusca, validarBusca,
} from './parametrosBusca';

const HOJE = '2028-02-28';
const PERIODO = { dataRetirada: HOJE, dataDevolucao: '2028-03-01' };
const BUSCA = { ...BUSCA_VAZIA, ...PERIODO };

describe('validação compartilhada da busca', () => {
  it('usa a data local e conta diárias pela diferença de datas, inclusive em ano bissexto', () => {
    expect(hojeLocal(new Date(2028, 1, 28, 23, 59))).toBe(HOJE);
    expect(avaliarPeriodo(PERIODO, HOJE)).toEqual({
      minimoDevolucao: '2028-02-29',
      diarias: 2,
      erros: {},
    });
  });

  it('associa datas obrigatórias e inválidas ao respectivo campo', () => {
    expect(validarBusca(BUSCA_VAZIA, HOJE)).toEqual({
      dataRetirada: texto.erroRetiradaObrigatoria,
      dataDevolucao: texto.erroDevolucaoObrigatoria,
    });
    expect(validarBusca({ ...BUSCA, dataRetirada: '2028-02-30' }, HOJE)).toEqual({
      dataRetirada: texto.erroRetiradaInvalida,
    });
  });

  it('bloqueia retirada no passado e devolução no mesmo dia', () => {
    expect(validarBusca({ ...BUSCA, dataRetirada: '2028-02-27' }, HOJE)).toEqual({
      dataRetirada: texto.erroRetiradaPassada,
    });
    expect(validarBusca({ ...BUSCA, dataDevolucao: HOJE }, HOJE)).toEqual({
      dataDevolucao: texto.erroDevolucaoPeriodo,
    });
  });

  it('associa valores inválidos e faixa invertida ao campo correspondente', () => {
    expect(validarBusca({ ...BUSCA, local: 'XX:São Paulo', valorDiariaMin: '-1', valorDiariaMax: 'NaN' }, HOJE)).toEqual({
      local: texto.erroCidade,
      valorDiariaMin: texto.erroDiariaMin,
      valorDiariaMax: texto.erroDiariaMax,
    });
    expect(validarBusca({ ...BUSCA, valorDiariaMin: '150', valorDiariaMax: '99.99' }, HOJE)).toEqual({
      valorDiariaMax: texto.erroFaixaDiaria,
    });
  });

  it('associa ProblemDetails de UF ao seletor e preserva os erros por campo', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'Falha.', erros: { Uf: ['UF inválida'], ValorDiariaMin: ['Valor inválido'] } });
    expect(errosBuscaDaApi(erro)).toEqual({ local: 'UF inválida', valorDiariaMin: 'Valor inválido' });
    expect(errosBuscaDaApi(new Error())).toEqual({});
  });
});

describe('leitura da URL e montagem dos filtros', () => {
  it('preserva acentos, normaliza UF e monta o filtro paginado com as chaves do contrato', () => {
    const parametros = new URLSearchParams({
      ...PERIODO, cidade: ' São Paulo ', uf: ' sp ', idCategoria: '3', valorDiariaMin: '0', valorDiariaMax: '150.99', pagina: '2',
    });
    const { rascunho, pagina } = lerBuscaDaUrl(parametros);
    expect(montarFiltroBusca(rascunho, pagina)).toEqual({
      ...PERIODO, cidade: 'São Paulo', uf: 'SP', idCategoria: 3, valorDiariaMin: 0, valorDiariaMax: 150.99, pagina: 2, tamanhoPagina: 12,
    });
    expect(montarParametrosBusca(rascunho, pagina).get('cidade')).toBe('São Paulo');
  });

  it.each(['0', '-1', 'NaN', '1.5', '1e2', '2147483648'])('ignora pagina e idCategoria inválidos: %s', (valor) => {
    const { rascunho, pagina } = lerBuscaDaUrl(new URLSearchParams({ pagina: valor, idCategoria: valor }));
    expect(pagina).toBe(1);
    expect(rascunho.idCategoria).toBe('');
  });

  it('ignora diárias inválidas, faixa invertida e par geográfico incompleto', () => {
    const parametros = new URLSearchParams({ valorDiariaMin: '-1', valorDiariaMax: 'Infinity', cidade: 'São Paulo' });
    expect(lerBuscaDaUrl(parametros).rascunho).toEqual(BUSCA_VAZIA);
    expect(lerBuscaDaUrl(new URLSearchParams({ valorDiariaMin: '99', valorDiariaMax: '9' })).rascunho).toEqual(BUSCA_VAZIA);
    expect(montarFiltroBusca(BUSCA)).not.toHaveProperty('cidade');
    expect(montarFiltroBusca(BUSCA)).not.toHaveProperty('uf');
  });

  it('ao paginar, serializa os filtros aplicados sem incorporar os campos em edição', () => {
    const aplicados = { ...BUSCA, local: 'SP:São Paulo', idCategoria: '3', valorDiariaMax: '130' };
    const rascunho = { ...aplicados, idCategoria: '7', valorDiariaMax: '99' };
    expect(lerBuscaDaUrl(montarParametrosBusca(aplicados, 2))).toEqual({ rascunho: aplicados, pagina: 2 });
    expect(rascunho).toMatchObject({ idCategoria: '7', valorDiariaMax: '99' });
  });
});
