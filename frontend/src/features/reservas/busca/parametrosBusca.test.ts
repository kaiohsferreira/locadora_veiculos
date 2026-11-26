import { describe, expect, it } from 'vitest';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { ErroApi } from '@/types/api';
import {
  avaliarPeriodo, BUSCA_VAZIA, calcularLimiteControle, camposMudadosNaBusca, decidirAplicacaoFiltros, errosBuscaDaApi,
  errosDoPeriodoDaUrl, hojeLocal, limparErrosAoEditar, lerBuscaDaUrl, montarFiltroBusca, montarLeituraFaixaDiaria, montarParametrosBusca,
  posicaoDoControle, validarAplicacaoFiltros, validarBusca, validarFaixaDiaria, valorDoControle,
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
    // A serialização usa só a busca aplicada: o rascunho em edição não aparece na página 2.
    expect(montarParametrosBusca(aplicados, 2).get('idCategoria')).toBe('3');
    expect(montarParametrosBusca(aplicados, 2).get('valorDiariaMax')).toBe('130');
    expect(montarParametrosBusca(rascunho, 2).get('idCategoria')).toBe('7');
  });
});

describe('faixa de diária e controle deslizante', () => {
  it('calcula o limite do controle: 500 por padrão e cresce quando mínimo ou máximo passam de 500', () => {
    expect(calcularLimiteControle('', '')).toBe(500);
    expect(calcularLimiteControle('100', '500')).toBe(500);
    expect(calcularLimiteControle('', '800')).toBe(800);
    expect(calcularLimiteControle('650', '')).toBe(650);
    expect(calcularLimiteControle('499.5', '')).toBe(500);
    expect(calcularLimiteControle('abc', '1e2')).toBe(500);
  });

  it('posiciona o controle: mínimo vazio em 0, máximo vazio no fim do trilho', () => {
    expect(posicaoDoControle('valorDiariaMin', '', 500)).toBe(0);
    expect(posicaoDoControle('valorDiariaMax', '', 500)).toBe(500);
    expect(posicaoDoControle('valorDiariaMin', '120.4', 500)).toBe(120);
    expect(posicaoDoControle('valorDiariaMax', '900', 900)).toBe(900);
  });

  it('grava o máximo vazio quando o controle chega ao fim e passa a gravar o valor nos demais pontos', () => {
    expect(valorDoControle('valorDiariaMax', 500, 500)).toBe('');
    expect(valorDoControle('valorDiariaMax', 499, 500)).toBe('499');
    expect(valorDoControle('valorDiariaMin', 500, 500)).toBe('500');
    expect(valorDoControle('valorDiariaMin', 0, 500)).toBe('0');
  });

  it('monta a leitura do intervalo com mínimo vazio em R$ 0 e máximo vazio com o limite e "+"', () => {
    expect(montarLeituraFaixaDiaria({ valorDiariaMin: '100', valorDiariaMax: '' })).toBe('R$ 100 – R$ 500+');
    expect(montarLeituraFaixaDiaria({ valorDiariaMin: '', valorDiariaMax: '' })).toBe('R$ 0 – R$ 500+');
    expect(montarLeituraFaixaDiaria({ valorDiariaMin: '100', valorDiariaMax: '350' })).toBe('R$ 100 – R$ 350');
    expect(montarLeituraFaixaDiaria({ valorDiariaMin: '99.5', valorDiariaMax: '' })).toBe('R$ 99,50 – R$ 500+');
    expect(montarLeituraFaixaDiaria({ valorDiariaMin: '', valorDiariaMax: '800' })).toBe('R$ 0 – R$ 800');
  });

  it('valida a faixa sozinha: valor inválido, negativo e máximo menor que o mínimo', () => {
    expect(validarFaixaDiaria({ valorDiariaMin: '-1', valorDiariaMax: '' })).toEqual({ valorDiariaMin: texto.erroDiariaMin });
    expect(validarFaixaDiaria({ valorDiariaMin: '', valorDiariaMax: 'NaN' })).toEqual({ valorDiariaMax: texto.erroDiariaMax });
    expect(validarFaixaDiaria({ valorDiariaMin: '150', valorDiariaMax: '99' })).toEqual({ valorDiariaMax: texto.erroFaixaDiaria });
    expect(validarFaixaDiaria({ valorDiariaMin: '50', valorDiariaMax: '' })).toEqual({});
  });

  it('aplica a faixa com datas e local digitados (válidos) e volta para a página 1', () => {
    const aplicados = { ...BUSCA, local: 'SP:São Paulo', idCategoria: '3' };
    const proposta = { ...aplicados, valorDiariaMin: '100', valorDiariaMax: '' };
    const decisao = decidirAplicacaoFiltros(aplicados, proposta, HOJE);
    expect(decisao.tipo).toBe('aplicar');
    if (decisao.tipo !== 'aplicar') return;
    expect(decisao.parametros.get('valorDiariaMin')).toBe('100');
    expect(decisao.parametros.get('dataRetirada')).toBe(HOJE);
    expect(decisao.parametros.get('cidade')).toBe('São Paulo');
    expect(decisao.parametros.has('pagina')).toBe(false);
  });

  it('aplica junto a data e o local digitados mesmo sem mudar o filtro da lateral', () => {
    const proposta = { ...BUSCA, dataRetirada: '2028-03-02', dataDevolucao: '2028-03-05', local: 'RJ:Rio de Janeiro' };
    const decisao = decidirAplicacaoFiltros(BUSCA, proposta, HOJE);
    expect(decisao.tipo).toBe('aplicar');
    if (decisao.tipo !== 'aplicar') return;
    expect(decisao.parametros.get('dataRetirada')).toBe('2028-03-02');
    expect(decisao.parametros.get('dataDevolucao')).toBe('2028-03-05');
    expect(decisao.parametros.get('cidade')).toBe('Rio de Janeiro');
    expect(decisao.parametros.get('uf')).toBe('RJ');
  });

  it('não aplica nada quando a data digitada é inválida, e devolve o erro do campo', () => {
    const proposta = { ...BUSCA, idCategoria: '5', dataRetirada: '2028-02-27' };
    expect(decidirAplicacaoFiltros(BUSCA, proposta, HOJE)).toEqual({
      tipo: 'erro',
      erros: { dataRetirada: texto.erroRetiradaPassada },
    });
  });

  it('aplica o filtro com a devolução vazia: a data vazia fica para o clique em Buscar', () => {
    const decisao = decidirAplicacaoFiltros(BUSCA, { ...BUSCA, dataDevolucao: '', idCategoria: '5' }, HOJE);
    expect(decisao.tipo).toBe('aplicar');
    if (decisao.tipo !== 'aplicar') return;
    expect(decisao.parametros.get('idCategoria')).toBe('5');
    expect(decisao.parametros.has('dataDevolucao')).toBe(false);
    expect(decisao.parametros.get('dataRetirada')).toBe(HOJE);
  });

  it('aplica o filtro com a retirada vazia, com as demais datas e o local digitados', () => {
    const proposta = { ...BUSCA, dataRetirada: '', local: 'SP:São Paulo' };
    const decisao = decidirAplicacaoFiltros(BUSCA, proposta, HOJE);
    expect(decisao.tipo).toBe('aplicar');
    if (decisao.tipo !== 'aplicar') return;
    expect(decisao.parametros.has('dataRetirada')).toBe(false);
    expect(decisao.parametros.get('dataDevolucao')).toBe('2028-03-01');
    expect(decisao.parametros.get('cidade')).toBe('São Paulo');
  });

  it('a data vazia não bloqueia, mas a devolução digitada fora do período ainda bloqueia', () => {
    expect(validarAplicacaoFiltros({ ...BUSCA, dataDevolucao: '' }, HOJE)).toEqual({});
    expect(validarAplicacaoFiltros({ ...BUSCA, dataRetirada: '', dataDevolucao: HOJE }, HOJE)).toEqual({
      dataDevolucao: texto.erroDevolucaoPeriodo,
    });
  });

  it('não aplica quando o local tem UF inválida, e devolve o erro do campo', () => {
    expect(decidirAplicacaoFiltros(BUSCA, { ...BUSCA, local: 'XX:Nada' }, HOJE)).toEqual({
      tipo: 'erro',
      erros: { local: texto.erroCidade },
    });
  });

  it('não aplica faixa inválida e devolve o erro do campo, mesmo com as datas válidas', () => {
    expect(decidirAplicacaoFiltros(BUSCA, { ...BUSCA, valorDiariaMin: '200', valorDiariaMax: '10' }, HOJE)).toEqual({
      tipo: 'erro',
      erros: { valorDiariaMax: texto.erroFaixaDiaria },
    });
  });

  it('não consulta de novo quando nada muda (a página não entra na decisão)', () => {
    const aplicados = { ...BUSCA, idCategoria: '3' };
    expect(decidirAplicacaoFiltros(aplicados, { ...aplicados, valorDiariaMin: '', valorDiariaMax: '' }, HOJE)).toEqual({ tipo: 'igual' });
  });

  it('volta para a página 1 com a mudança de um filtro, inclusive a partir da página 2', () => {
    const aplicados = { ...BUSCA, idCategoria: '3' };
    const decisao = decidirAplicacaoFiltros(aplicados, { ...aplicados, idCategoria: '5' }, HOJE);
    expect(decisao.tipo).toBe('aplicar');
    if (decisao.tipo !== 'aplicar') return;
    expect(decisao.parametros.get('idCategoria')).toBe('5');
    expect(decisao.parametros.has('pagina')).toBe(false);
  });

  it('troca a URL sem criar passo no histórico ao aplicar um filtro da lateral', () => {
    const aplicados = { ...BUSCA, idCategoria: '3' };
    const decisao = decidirAplicacaoFiltros(aplicados, { ...aplicados, idCategoria: '' }, HOJE);
    expect(decisao).toMatchObject({ tipo: 'aplicar', substituirHistorico: true });
  });
});

describe('erros da lateral ao editar um campo', () => {
  it('a data de retirada leva junto o erro da devolução, que depende dela; o local e a categoria ficam', () => {
    const erros = { dataRetirada: 'a', dataDevolucao: 'b', local: 'c', idCategoria: 'd' };
    expect(limparErrosAoEditar(erros, 'dataRetirada')).toEqual({ local: 'c', idCategoria: 'd' });
  });

  it('a devolução só tira o próprio erro, e a retirada continua com o dela', () => {
    const erros = { dataRetirada: 'a', dataDevolucao: 'b' };
    expect(limparErrosAoEditar(erros, 'dataDevolucao')).toEqual({ dataRetirada: 'a' });
  });

  it('o mínimo da diária leva junto o erro do máximo (faixa invertida); o máximo tira só o dele', () => {
    const erros = { valorDiariaMin: 'a', valorDiariaMax: 'b', dataRetirada: 'c' };
    expect(limparErrosAoEditar(erros, 'valorDiariaMin')).toEqual({ dataRetirada: 'c' });
    expect(limparErrosAoEditar(erros, 'valorDiariaMax')).toEqual({ valorDiariaMin: 'a', dataRetirada: 'c' });
  });

  it('não muda o quadro original', () => {
    const erros = { local: 'c' };
    limparErrosAoEditar(erros, 'local');
    expect(erros).toEqual({ local: 'c' });
  });
});

describe('sincronização do rascunho com a busca aplicada', () => {
  it('devolve só os campos cujo valor aplicado mudou', () => {
    const anterior = { ...BUSCA, idCategoria: '3' };
    const atual = { ...anterior, idCategoria: '5', dataRetirada: '2028-03-02' };
    expect(camposMudadosNaBusca(anterior, atual)).toEqual({ idCategoria: '5', dataRetirada: '2028-03-02' });
  });

  it('não devolve nada quando a busca aplicada não muda (inclusive a página, que não entra no rascunho)', () => {
    expect(camposMudadosNaBusca(BUSCA, { ...BUSCA })).toEqual({});
  });

  it('a diária aplicada volta para o rascunho quando muda, e o local também', () => {
    const anterior = { ...BUSCA, local: '', valorDiariaMin: '' };
    expect(camposMudadosNaBusca(anterior, { ...anterior, valorDiariaMin: '120', local: 'SP:São Paulo' })).toEqual({
      valorDiariaMin: '120',
      local: 'SP:São Paulo',
    });
  });
});

describe('erros da abertura e página além da última', () => {
  it('mostra na abertura o erro da data já aplicada que está passada, sem apagar as vazias', () => {
    const aplicados = { ...BUSCA, dataRetirada: '2028-02-27', dataDevolucao: '' };
    expect(errosDoPeriodoDaUrl(aplicados, aplicados, HOJE)).toEqual({ dataRetirada: texto.erroRetiradaPassada });
  });

  it('some com o erro do campo quando o usuário edita a data', () => {
    const aplicados = { ...BUSCA, dataRetirada: '2028-02-27' };
    expect(errosDoPeriodoDaUrl(aplicados, { ...aplicados, dataRetirada: HOJE }, HOJE)).toEqual({});
  });

  it('mostra o erro de data inexistente vinda da URL (2028-02-30) na abertura, no próprio campo', () => {
    const { rascunho } = lerBuscaDaUrl(new URLSearchParams({ dataRetirada: '2028-02-30', dataDevolucao: '2028-03-01' }));
    expect(rascunho.dataRetirada).toBe('2028-02-30');
    expect(errosDoPeriodoDaUrl(rascunho, rascunho, HOJE)).toEqual({ dataRetirada: texto.erroRetiradaInvalida });
  });

  it('descarta texto longo demais no lugar de data', () => {
    const { rascunho } = lerBuscaDaUrl(new URLSearchParams({ dataRetirada: 'x'.repeat(50) }));
    expect(rascunho.dataRetirada).toBe('');
  });
});
