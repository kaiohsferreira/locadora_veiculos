import { describe, expect, it } from 'vitest';
import {
  alterarListagemFrota,
  type EstadoListagemFrota,
} from './parametrosListagemFrota';

/** Estado no meio de uma lista: página 3, com busca, categoria e status escolhidos e 25 linhas. */
const ESTADO_NO_MEIO: EstadoListagemFrota = {
  pagina: 3,
  tamanhoPagina: 25,
  filtros: { busca: 'Onix', idCategoria: 2, status: 'LOCADO' },
};

describe('alterarListagemFrota com a mudança de tela (Painel | Lista)', () => {
  it('trocar de tela volta para a página 1 e mantém busca, categoria, status e linhas', () => {
    const depois = alterarListagemFrota(ESTADO_NO_MEIO, { tipo: 'tela' });

    expect(depois.pagina).toBe(1);
    expect(depois.tamanhoPagina).toBe(25);
    expect(depois.filtros).toEqual({ busca: 'Onix', idCategoria: 2, status: 'LOCADO' });
  });

  it('não altera o estado recebido', () => {
    alterarListagemFrota(ESTADO_NO_MEIO, { tipo: 'tela' });

    expect(ESTADO_NO_MEIO.pagina).toBe(3);
  });

  it('as outras mudanças continuam como eram (página escolhida fica)', () => {
    const depois = alterarListagemFrota(ESTADO_NO_MEIO, { tipo: 'pagina', pagina: 5 });

    expect(depois.pagina).toBe(5);
    expect(depois.filtros).toEqual(ESTADO_NO_MEIO.filtros);
  });
});
