import { describe, expect, it } from 'vitest';
import {
  alterarListagemFrota,
  ESTADO_FILTROS_FROTA_INICIAL,
  ESTADO_LISTAGEM_FROTA_INICIAL,
  estadoRodapeFrota,
  filtroMudou,
  montarFiltroListagemFrota,
  OPCOES_LINHAS_POR_PAGINA_FROTA,
  PILULAS_FROTA,
  paginaAposAplicarBusca,
  paginaAposResposta,
  temFiltroFrotaAplicado,
  temFiltroNaListaMostrada,
  termoBuscaFrota,
  TAMANHO_MAXIMO_BUSCA_FROTA,
  type EstadoListagemFrota,
  ultimaPaginaFrota,
} from './parametrosListagemFrota';

const ESTADO_PAGINADO: EstadoListagemFrota = {
  pagina: 4,
  tamanhoPagina: 10,
  filtros: { busca: 'argo', idCategoria: 2, status: 'LOCADO' },
};

describe('montarFiltroListagemFrota', () => {
  it('sem filtro, leva só página e tamanho', () => {
    expect(montarFiltroListagemFrota(1, 10, ESTADO_FILTROS_FROTA_INICIAL)).toEqual({ pagina: 1, tamanhoPagina: 10 });
  });

  it('leva status, categoria e busca quando escolhidos', () => {
    expect(montarFiltroListagemFrota(2, 25, { busca: 'Onix', idCategoria: 3, status: 'MANUTENCAO' })).toEqual({
      pagina: 2,
      tamanhoPagina: 25,
      status: 'MANUTENCAO',
      idCategoria: 3,
      busca: 'Onix',
    });
  });

  it('busca vazia ou só com espaços não entra no filtro', () => {
    expect(montarFiltroListagemFrota(1, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: '   ' })).not.toHaveProperty('busca');
    expect(montarFiltroListagemFrota(1, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: '' })).not.toHaveProperty('busca');
  });

  it('a busca sai aparada', () => {
    expect(montarFiltroListagemFrota(1, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: '  Corolla  ' }).busca).toBe('Corolla');
  });

  it('a busca é limitada ao teto do contrato (60 caracteres)', () => {
    const digitada = 'a'.repeat(TAMANHO_MAXIMO_BUSCA_FROTA + 10);
    const busca = montarFiltroListagemFrota(1, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: digitada }).busca;

    expect(busca).toHaveLength(TAMANHO_MAXIMO_BUSCA_FROTA);
  });

  it('o teto de 60 cai no caractere de corte: espaço no fim não sobra', () => {
    const digitada = `${'a'.repeat(59)} b`;
    const busca = montarFiltroListagemFrota(1, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: digitada }).busca;

    expect(busca).toBe('a'.repeat(59));
  });
});

describe('alterarListagemFrota', () => {
  it('trocar a pílula volta para a página 1 e mantém a busca e a categoria', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { status: 'DISPONIVEL' } });

    expect(proximo.pagina).toBe(1);
    expect(proximo.filtros).toEqual({ busca: 'argo', idCategoria: 2, status: 'DISPONIVEL' });
  });

  it('trocar a categoria volta para a página 1', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { idCategoria: '' } });

    expect(proximo.pagina).toBe(1);
    expect(proximo.filtros.idCategoria).toBe('');
  });

  it('um filtro com busca no parcial volta para a página 1 (a digitação não passa por aqui: ver paginaAposAplicarBusca)', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { busca: 'Onix' } });

    expect(proximo.pagina).toBe(1);
    expect(proximo.filtros.busca).toBe('Onix');
  });

  it('trocar as linhas por página volta para a página 1 e grava o tamanho', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'linhas', tamanhoPagina: 50 });

    expect(proximo.pagina).toBe(1);
    expect(proximo.tamanhoPagina).toBe(50);
    expect(proximo.filtros).toEqual(ESTADO_PAGINADO.filtros);
  });

  it('trocar só de página não mexe nos filtros', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'pagina', pagina: 7 });

    expect(proximo.pagina).toBe(7);
    expect(proximo.filtros).toEqual(ESTADO_PAGINADO.filtros);
  });

  it('não altera o estado recebido', () => {
    alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { status: 'LOCADO' } });

    expect(ESTADO_PAGINADO.pagina).toBe(4);
    expect(ESTADO_PAGINADO.filtros.status).toBe('LOCADO');
  });

  it('escolher a pílula que já está marcada não volta para a página 1 (o estado é o mesmo)', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { status: 'LOCADO' } });

    expect(proximo).toBe(ESTADO_PAGINADO);
    expect(proximo.pagina).toBe(4);
  });

  it('repetir a mesma categoria não volta para a página 1', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { idCategoria: 2 } });

    expect(proximo.pagina).toBe(4);
  });

  it('a busca com o mesmo termo aparado (só com espaços a mais) não volta para a página 1', () => {
    const proximo = alterarListagemFrota(ESTADO_PAGINADO, { tipo: 'filtro', parcial: { busca: '  argo  ' } });

    expect(proximo.pagina).toBe(4);
    expect(proximo.filtros.busca).toBe('argo');
  });
});

describe('filtroMudou', () => {
  it('muda quando a pílula, a categoria ou o termo aparado da busca são diferentes', () => {
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { status: 'DISPONIVEL' })).toBe(true);
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { idCategoria: '' })).toBe(true);
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { busca: 'onix' })).toBe(true);
  });

  it('não muda quando o valor é o mesmo, inclusive com espaços a mais na busca', () => {
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { status: 'LOCADO' })).toBe(false);
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { idCategoria: 2 })).toBe(false);
    expect(filtroMudou(ESTADO_PAGINADO.filtros, { busca: ' argo ' })).toBe(false);
  });

  it('campo ausente no parcial não conta como mudança', () => {
    expect(filtroMudou(ESTADO_PAGINADO.filtros, {})).toBe(false);
  });
});

describe('temFiltroFrotaAplicado', () => {
  it('falso no padrão e verdadeiro com busca, categoria ou pílula', () => {
    expect(temFiltroFrotaAplicado(ESTADO_FILTROS_FROTA_INICIAL)).toBe(false);
    expect(temFiltroFrotaAplicado({ ...ESTADO_FILTROS_FROTA_INICIAL, busca: 'x' })).toBe(true);
    expect(temFiltroFrotaAplicado({ ...ESTADO_FILTROS_FROTA_INICIAL, idCategoria: 1 })).toBe(true);
    expect(temFiltroFrotaAplicado({ ...ESTADO_FILTROS_FROTA_INICIAL, status: 'LOCADO' })).toBe(true);
  });

  it('busca só com espaços não conta como filtro', () => {
    expect(temFiltroFrotaAplicado({ ...ESTADO_FILTROS_FROTA_INICIAL, busca: '   ' })).toBe(false);
  });
});

describe('paginaAposAplicarBusca', () => {
  it('busca nova (termo diferente) volta para a página 1', () => {
    expect(paginaAposAplicarBusca('argo', 'onix', 4)).toBe(1);
  });

  it('busca que só ganha ou perde espaços não tira a pessoa da página', () => {
    expect(paginaAposAplicarBusca('argo', 'argo ', 4)).toBe(4);
    expect(paginaAposAplicarBusca('argo', '   argo', 3)).toBe(3);
  });

  it('busca só com espaços é o mesmo que sem busca: não muda a página', () => {
    expect(paginaAposAplicarBusca('', '   ', 5)).toBe(5);
  });

  it('a comparação usa o termo que a API recebe (o corte de 60 caracteres conta)', () => {
    const base = 'a'.repeat(TAMANHO_MAXIMO_BUSCA_FROTA);
    expect(paginaAposAplicarBusca(base, `${base}b`, 2)).toBe(2);
  });
});

describe('termoBuscaFrota (identidade da busca na tela)', () => {
  it('apara os espaços e devolve vazio para busca só com espaços', () => {
    expect(termoBuscaFrota('  Onix  ')).toBe('Onix');
    expect(termoBuscaFrota('   ')).toBe('');
    expect(termoBuscaFrota('')).toBe('');
  });
});

describe('C: digitar só espaços não muda a consulta', () => {
  it('a chave (o filtro que vai à API) é a mesma de sem busca', () => {
    const semBusca = montarFiltroListagemFrota(3, 10, ESTADO_FILTROS_FROTA_INICIAL);
    const soEspacos = montarFiltroListagemFrota(3, 10, { ...ESTADO_FILTROS_FROTA_INICIAL, busca: '     ' });

    expect(soEspacos).toEqual(semBusca);
  });

  it('não é filtro aplicado e não tira a pessoa da página (vale para qualquer quantidade de espaços)', () => {
    expect(temFiltroFrotaAplicado({ ...ESTADO_FILTROS_FROTA_INICIAL, busca: '    ' })).toBe(false);
    expect(paginaAposAplicarBusca('', '     ', 6)).toBe(6);
  });
});

describe('B: trocar de página com busca digitada e ainda não aplicada', () => {
  it('a página pedida só vale se a busca não mudou; com busca nova pendente, a página vai para 1', () => {
    expect(paginaAposAplicarBusca('', 'onix', 5)).toBe(1);
    expect(paginaAposAplicarBusca('argo', 'onix', 5)).toBe(1);
  });

  it('com a busca igual à da consulta, a página pedida é a que vale', () => {
    expect(paginaAposAplicarBusca('argo', 'argo', 5)).toBe(5);
  });
});

describe('A: ajuste de página só com resposta da consulta atual', () => {
  it('com resposta da consulta atual, página além da última volta para a última existente', () => {
    expect(paginaAposResposta(7, { total: 25, tamanhoPagina: 10 }, true)).toBe(3);
  });

  it('com resposta da consulta atual, página dentro do limite fica como está', () => {
    expect(paginaAposResposta(2, { total: 25, tamanhoPagina: 10 }, true)).toBe(2);
  });

  it('enquanto a próxima consulta carrega (resposta anterior), não corrige a página, mesmo fora do limite', () => {
    expect(paginaAposResposta(7, { total: 25, tamanhoPagina: 10 }, false)).toBe(7);
  });

  it('sem resposta, a página não muda', () => {
    expect(paginaAposResposta(4, undefined, true)).toBe(4);
  });
});

describe('A: rodapé com a página e as linhas da tela', () => {
  it('sem resposta, não há rodapé', () => {
    expect(estadoRodapeFrota(ESTADO_LISTAGEM_FROTA_INICIAL, undefined)).toBeNull();
  });

  it('pagina e linhas vêm do estado da tela; o total vem da resposta mostrada', () => {
    const estado = { pagina: 3, tamanhoPagina: 50 };

    expect(estadoRodapeFrota(estado, { total: 120 })).toEqual({ pagina: 3, tamanhoPagina: 50, total: 120 });
  });
});

describe('A: texto de lista vazia pelo filtro da lista mostrada', () => {
  it('sem espera, vale o filtro da consulta atual', () => {
    expect(temFiltroNaListaMostrada(true, false, false)).toBe(true);
    expect(temFiltroNaListaMostrada(false, true, false)).toBe(false);
  });

  it('enquanto espera, vale o filtro da última resposta (a tabela mostra essas linhas), não o digitado agora', () => {
    expect(temFiltroNaListaMostrada(true, false, true)).toBe(false);
    expect(temFiltroNaListaMostrada(false, true, true)).toBe(true);
  });
});

describe('ultimaPaginaFrota', () => {
  it('sem item, a última página é a 1', () => {
    expect(ultimaPaginaFrota(0, 10)).toBe(1);
  });

  it('total exato de múltiplo do tamanho não cria página extra', () => {
    expect(ultimaPaginaFrota(20, 10)).toBe(2);
    expect(ultimaPaginaFrota(21, 10)).toBe(3);
  });

  it('pega o arredondamento para cima com o tamanho de 25 linhas', () => {
    expect(ultimaPaginaFrota(51, 25)).toBe(3);
  });
});

describe('constantes da listagem', () => {
  it('as pílulas seguem a ordem do desenho, com Reservados no lugar de Aguardando Laudo', () => {
    expect(PILULAS_FROTA.map((pilula) => pilula.valor)).toEqual(['', 'DISPONIVEL', 'LOCADO', 'MANUTENCAO', 'RESERVADO']);
    expect(PILULAS_FROTA.map((pilula) => pilula.rotulo)).toEqual([
      'Todos',
      'Disponíveis',
      'Locados',
      'Manutenção',
      'Reservados',
    ]);
  });

  it('as linhas por página são 10, 25, 50 e 100, e a página começa em 1 com 10 linhas', () => {
    expect(OPCOES_LINHAS_POR_PAGINA_FROTA).toEqual([10, 25, 50, 100]);
    expect(ESTADO_LISTAGEM_FROTA_INICIAL).toMatchObject({ pagina: 1, tamanhoPagina: 10 });
  });
});
