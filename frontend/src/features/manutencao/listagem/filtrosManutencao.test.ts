import { describe, expect, it } from 'vitest';
import {
  ESTADO_FILTROS_MANUTENCAO_INICIAL,
  OPCOES_LINHAS_POR_PAGINA_MANUTENCAO,
  TAMANHO_MAXIMO_BUSCA_MANUTENCAO,
  TAMANHO_PAGINA_MANUTENCAO_PADRAO,
  alterarFiltrosManutencao,
  montarFiltroManutencao,
  temFiltroManutencaoAplicado,
  type EstadoFiltrosManutencao,
} from './filtrosManutencao';

const ESTADO_COM_PAGINA_3: EstadoFiltrosManutencao = { ...ESTADO_FILTROS_MANUTENCAO_INICIAL, pagina: 3 };

describe('constantes da listagem', () => {
  it('a tela oferece 10, 20 e 50 linhas, com 20 de padrão', () => {
    expect(OPCOES_LINHAS_POR_PAGINA_MANUTENCAO).toEqual([10, 20, 50]);
    expect(TAMANHO_PAGINA_MANUTENCAO_PADRAO).toBe(20);
    expect(ESTADO_FILTROS_MANUTENCAO_INICIAL.tamanhoPagina).toBe(20);
  });

  it('a busca tem até 60 caracteres, como a busca da frota', () => {
    expect(TAMANHO_MAXIMO_BUSCA_MANUTENCAO).toBe(60);
  });
});

describe('alterarFiltrosManutencao: cada filtro volta para a página 1', () => {
  it('mudar a busca, o tipo, a situação ou o veículo leva para a página 1', () => {
    expect(alterarFiltrosManutencao(ESTADO_COM_PAGINA_3, { tipo: 'busca', busca: 'onix' }).pagina).toBe(1);
    expect(alterarFiltrosManutencao(ESTADO_COM_PAGINA_3, { tipo: 'tipo', valor: 'CORRETIVA' }).pagina).toBe(1);
    expect(alterarFiltrosManutencao(ESTADO_COM_PAGINA_3, { tipo: 'situacao', valor: 'ABERTA' }).pagina).toBe(1);
    expect(alterarFiltrosManutencao(ESTADO_COM_PAGINA_3, { tipo: 'veiculo', idVeiculo: 7 }).pagina).toBe(1);
  });

  it('mudar as linhas por página leva para a página 1 e guarda o novo tamanho', () => {
    const estado = alterarFiltrosManutencao(ESTADO_COM_PAGINA_3, { tipo: 'linhas', tamanhoPagina: 50 });

    expect(estado.pagina).toBe(1);
    expect(estado.tamanhoPagina).toBe(50);
  });

  it('trocar só a página não mexe nos filtros', () => {
    const estado = alterarFiltrosManutencao({ ...ESTADO_COM_PAGINA_3, tipo: 'PREVENTIVA' }, { tipo: 'pagina', pagina: 4 });

    expect(estado.pagina).toBe(4);
    expect(estado.tipo).toBe('PREVENTIVA');
  });

  it('não muda o estado original (função pura)', () => {
    const original = { ...ESTADO_FILTROS_MANUTENCAO_INICIAL };
    alterarFiltrosManutencao(original, { tipo: 'veiculo', idVeiculo: 9 });

    expect(original.idVeiculo).toBeNull();
  });
});

describe('alterarFiltrosManutencao: limpar', () => {
  it('zera busca, tipo, situação e veículo (inclusive o "Ver Histórico") e mantém as linhas por página', () => {
    const preenchido: EstadoFiltrosManutencao = {
      busca: 'onix',
      tipo: 'CORRETIVA',
      situacao: 'ENCERRADA',
      idVeiculo: 7,
      pagina: 3,
      tamanhoPagina: 50,
    };

    const estado = alterarFiltrosManutencao(preenchido, { tipo: 'limpar' });

    expect(estado).toEqual({ busca: '', tipo: '', situacao: '', idVeiculo: null, pagina: 1, tamanhoPagina: 50 });
  });
});

describe('temFiltroManutencaoAplicado', () => {
  it('sem nenhum filtro, não há o que limpar', () => {
    expect(temFiltroManutencaoAplicado(ESTADO_FILTROS_MANUTENCAO_INICIAL)).toBe(false);
  });

  it('a busca digitada conta, mesmo antes de chegar à consulta', () => {
    expect(temFiltroManutencaoAplicado({ ...ESTADO_FILTROS_MANUTENCAO_INICIAL, busca: 'o' })).toBe(true);
  });

  it('o veículo do "Ver Histórico" conta como filtro', () => {
    expect(temFiltroManutencaoAplicado({ ...ESTADO_FILTROS_MANUTENCAO_INICIAL, idVeiculo: 7 })).toBe(true);
  });

  it('só as linhas por página não contam', () => {
    expect(temFiltroManutencaoAplicado({ ...ESTADO_FILTROS_MANUTENCAO_INICIAL, tamanhoPagina: 50 })).toBe(false);
  });
});

describe('montarFiltroManutencao', () => {
  it('usa a busca que já chegou à consulta (a que passou da pausa), aparada', () => {
    const filtro = montarFiltroManutencao({ ...ESTADO_FILTROS_MANUTENCAO_INICIAL, busca: 'digitando' }, '  onix  ');

    expect(filtro.busca).toBe('onix');
  });

  it('leva tipo, situação e veículo só quando escolhidos', () => {
    const filtro = montarFiltroManutencao(
      { ...ESTADO_FILTROS_MANUTENCAO_INICIAL, tipo: 'PREVENTIVA', situacao: 'ABERTA', idVeiculo: 7, pagina: 2 },
      '',
    );

    expect(filtro).toEqual({ pagina: 2, tamanhoPagina: 20, busca: '', tipo: 'PREVENTIVA', situacao: 'ABERTA', idVeiculo: 7 });
  });

  it('com filtros vazios, o tipo, a situação e o veículo ficam ausentes', () => {
    const filtro = montarFiltroManutencao(ESTADO_FILTROS_MANUTENCAO_INICIAL, '');

    expect(filtro.tipo).toBeUndefined();
    expect(filtro.situacao).toBeUndefined();
    expect(filtro.idVeiculo).toBeUndefined();
  });
});
