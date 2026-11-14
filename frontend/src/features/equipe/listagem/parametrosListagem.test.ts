import { describe, expect, it } from 'vitest';
import { MENSAGEM_EQUIPE_SITUACAO_ATIVOS, MENSAGEM_EQUIPE_SITUACAO_INATIVOS, MENSAGEM_EQUIPE_SITUACAO_TODOS } from '@/lib/mensagens';
import {
  ativoDaSituacao,
  ESTADO_FILTROS_EQUIPE_INICIAL,
  montarFiltroEquipe,
  OPCOES_SITUACAO_EQUIPE,
  temFiltroAplicado,
  TAMANHO_MAXIMO_BUSCA_EQUIPE,
  TAMANHO_PAGINA_EQUIPE,
} from './parametrosListagem';

describe('montarFiltroEquipe', () => {
  it('começa em Ativos, com paginação de 20 e sem busca nem cargo', () => {
    expect(montarFiltroEquipe(1, ESTADO_FILTROS_EQUIPE_INICIAL)).toEqual({
      pagina: 1,
      tamanhoPagina: TAMANHO_PAGINA_EQUIPE,
      ativo: true,
    });
    expect(TAMANHO_PAGINA_EQUIPE).toBe(20);
  });

  it('situação Todos não envia o parâmetro ativo', () => {
    const filtro = montarFiltroEquipe(1, { ...ESTADO_FILTROS_EQUIPE_INICIAL, situacao: 'todos' });
    expect(filtro).not.toHaveProperty('ativo');
  });

  it('situação Inativos envia ativo = false', () => {
    const filtro = montarFiltroEquipe(3, { ...ESTADO_FILTROS_EQUIPE_INICIAL, situacao: 'inativos' });
    expect(filtro).toMatchObject({ pagina: 3, ativo: false });
  });

  it('apara a busca e não envia busca só com espaços', () => {
    expect(montarFiltroEquipe(1, { ...ESTADO_FILTROS_EQUIPE_INICIAL, busca: '  Marina  ' })).toMatchObject({
      busca: 'Marina',
    });
    expect(montarFiltroEquipe(1, { ...ESTADO_FILTROS_EQUIPE_INICIAL, busca: '    ' })).not.toHaveProperty('busca');
  });

  it('limita a busca ao máximo do contrato (60 caracteres)', () => {
    const longa = 'a'.repeat(80);
    const filtro = montarFiltroEquipe(1, { ...ESTADO_FILTROS_EQUIPE_INICIAL, busca: longa });
    expect(filtro.busca).toHaveLength(TAMANHO_MAXIMO_BUSCA_EQUIPE);
  });

  it('leva o cargo escolhido e não envia cargo vazio', () => {
    expect(montarFiltroEquipe(1, { ...ESTADO_FILTROS_EQUIPE_INICIAL, cargo: 'FINANCEIRO' })).toMatchObject({
      cargo: 'FINANCEIRO',
    });
    expect(montarFiltroEquipe(1, ESTADO_FILTROS_EQUIPE_INICIAL)).not.toHaveProperty('cargo');
  });
});

describe('ativoDaSituacao', () => {
  it('mapeia a situação para o parâmetro ativo', () => {
    expect(ativoDaSituacao('ativos')).toBe(true);
    expect(ativoDaSituacao('inativos')).toBe(false);
    expect(ativoDaSituacao('todos')).toBeUndefined();
  });
});

describe('OPCOES_SITUACAO_EQUIPE', () => {
  it('são Todos, Ativos e Inativos, nesta ordem', () => {
    expect(OPCOES_SITUACAO_EQUIPE.map((opcao) => opcao.valor)).toEqual(['todos', 'ativos', 'inativos']);
    expect(OPCOES_SITUACAO_EQUIPE.map((opcao) => opcao.rotulo)).toEqual([
      MENSAGEM_EQUIPE_SITUACAO_TODOS,
      MENSAGEM_EQUIPE_SITUACAO_ATIVOS,
      MENSAGEM_EQUIPE_SITUACAO_INATIVOS,
    ]);
  });
});

describe('temFiltroAplicado', () => {
  it('considera o padrão (Ativos) como filtro aplicado', () => {
    expect(temFiltroAplicado(ESTADO_FILTROS_EQUIPE_INICIAL)).toBe(true);
  });

  it('é falso só com busca vazia, cargo vazio e situação Todos', () => {
    expect(temFiltroAplicado({ busca: '  ', cargo: '', situacao: 'todos' })).toBe(false);
    expect(temFiltroAplicado({ busca: 'Ana', cargo: '', situacao: 'todos' })).toBe(true);
    expect(temFiltroAplicado({ busca: '', cargo: 'VENDEDOR', situacao: 'todos' })).toBe(true);
  });
});
