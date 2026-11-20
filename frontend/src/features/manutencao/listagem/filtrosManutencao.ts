/**
 * Filtros e página da listagem de ordens (tela 14). Funções puras, sem React.
 *
 * Regra: qualquer filtro novo volta para a página 1; trocar só a página mantém os filtros; "Limpar filtros" zera tudo
 * (inclusive o veículo do "Ver Histórico") e mantém as linhas por página escolhidas.
 */
import type { TipoManutencao } from '@/types/enums';
import type { FiltroManutencao, SituacaoOrdemManutencao } from '@/types/manutencao';

/** As linhas que a tela oferece. A API aceita de 1 a 100 (padrão do projeto). */
export const OPCOES_LINHAS_POR_PAGINA_MANUTENCAO: readonly number[] = [10, 20, 50];
export const TAMANHO_PAGINA_MANUTENCAO_PADRAO = 20;
/** Tamanho máximo da busca (igual à busca de reservas). */
export const TAMANHO_MAXIMO_BUSCA_MANUTENCAO = 60;
/** Pausa depois de digitar antes de consultar (uma consulta por pausa, não por tecla). */
export const ATRASO_BUSCA_MANUTENCAO_MS = 400;

export interface EstadoFiltrosManutencao {
  /** Texto digitado na busca (ainda não aparado nem consultado). */
  busca: string;
  tipo: TipoManutencao | '';
  situacao: SituacaoOrdemManutencao | '';
  /** Veículo do "Ver Histórico", ou nulo para todos. */
  idVeiculo: number | null;
  pagina: number;
  tamanhoPagina: number;
}

export const ESTADO_FILTROS_MANUTENCAO_INICIAL: EstadoFiltrosManutencao = {
  busca: '',
  tipo: '',
  situacao: '',
  idVeiculo: null,
  pagina: 1,
  tamanhoPagina: TAMANHO_PAGINA_MANUTENCAO_PADRAO,
};

export type MudancaFiltrosManutencao =
  | { tipo: 'busca'; busca: string }
  | { tipo: 'tipo'; valor: TipoManutencao | '' }
  | { tipo: 'situacao'; valor: SituacaoOrdemManutencao | '' }
  | { tipo: 'veiculo'; idVeiculo: number | null }
  | { tipo: 'pagina'; pagina: number }
  | { tipo: 'linhas'; tamanhoPagina: number }
  | { tipo: 'limpar' };

/** Aplica uma mudança e devolve um estado novo. Filtros e linhas voltam para a página 1; trocar a página não mexe neles. */
export function alterarFiltrosManutencao(
  estado: EstadoFiltrosManutencao,
  mudanca: MudancaFiltrosManutencao,
): EstadoFiltrosManutencao {
  switch (mudanca.tipo) {
    case 'busca':
      return { ...estado, busca: mudanca.busca, pagina: 1 };
    case 'tipo':
      return { ...estado, tipo: mudanca.valor, pagina: 1 };
    case 'situacao':
      return { ...estado, situacao: mudanca.valor, pagina: 1 };
    case 'veiculo':
      return { ...estado, idVeiculo: mudanca.idVeiculo, pagina: 1 };
    case 'pagina':
      return { ...estado, pagina: mudanca.pagina };
    case 'linhas':
      return { ...estado, tamanhoPagina: mudanca.tamanhoPagina, pagina: 1 };
    case 'limpar':
      return { ...ESTADO_FILTROS_MANUTENCAO_INICIAL, tamanhoPagina: estado.tamanhoPagina };
  }
}

/** Há algum filtro aplicado (a busca digitada conta, mesmo antes da consulta). As linhas por página não contam. */
export function temFiltroManutencaoAplicado(estado: EstadoFiltrosManutencao): boolean {
  return estado.busca.trim() !== '' || estado.tipo !== '' || estado.situacao !== '' || estado.idVeiculo !== null;
}

/**
 * Filtro que vai para a consulta. `buscaAplicada` é a busca que já passou da pausa (o chamador usa o useDebounce);
 * o aparo acontece na API. Tipo, situação e veículo só entram quando escolhidos.
 */
export function montarFiltroManutencao(estado: EstadoFiltrosManutencao, buscaAplicada: string): FiltroManutencao {
  const filtro: FiltroManutencao = {
    pagina: estado.pagina,
    tamanhoPagina: estado.tamanhoPagina,
    busca: buscaAplicada.trim(),
  };
  if (estado.tipo !== '') filtro.tipo = estado.tipo;
  if (estado.situacao !== '') filtro.situacao = estado.situacao;
  if (estado.idVeiculo !== null) filtro.idVeiculo = estado.idVeiculo;
  return filtro;
}
