import { createContext } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import type { ErroApi, Pagina } from '@/types/api';
import type { VeiculoRetorno } from '@/types/veiculos';
import type { EstadoFiltrosFrota, EstadoListagemFrota } from './parametrosListagemFrota';

/** Consulta da lista da frota (a mesma para as telas 11 e 12, com os filtros do provedor). */
export type ConsultaListagemFrota = UseQueryResult<Pagina<VeiculoRetorno>, ErroApi | Error>;

/** Valores do rodapé da lista: página e linhas do estado, total da resposta que está na tela. */
export interface RodapeFiltrosFrota {
  pagina: number;
  tamanhoPagina: number;
  total: number;
}

/** O que as telas 11 e 12 leem e mudam nos filtros da frota (busca, categoria, status, página e linhas). */
export interface FiltrosListagemFrotaValor {
  /** Página, linhas e filtros que a pessoa escolheu (a busca digitada, antes da pausa). */
  estado: EstadoListagemFrota;
  /** Consulta da lista com os filtros que a API recebe. */
  consulta: ConsultaListagemFrota;
  /** Termo da busca que está na tela (aparado). */
  termoDigitado: string;
  /** A lista mostrada tem filtro (escolhe o texto da lista vazia). */
  temFiltroNaLista: boolean;
  /** Rodapé da lista, ou nulo enquanto não há resposta. */
  rodape: RodapeFiltrosFrota | null;
  digitarBusca: (busca: string) => void;
  mudarPagina: (pagina: number) => void;
  mudarFiltro: (parcial: Partial<EstadoFiltrosFrota>) => void;
  mudarLinhas: (tamanhoPagina: number) => void;
  limparFiltros: () => void;
}

/** Valor nulo fora do provedor: `useFiltrosFrota` avisa o erro de montagem. */
export const ContextoFiltrosFrota = createContext<FiltrosListagemFrotaValor | null>(null);
