/**
 * Regras da listagem da equipe, sem React: como o estado dos filtros vira o filtro da API e quais opções de situação
 * a barra mostra. A chamada HTTP fica em `@/api/equipe`.
 */
import { MENSAGEM_EQUIPE_SITUACAO_ATIVOS, MENSAGEM_EQUIPE_SITUACAO_INATIVOS, MENSAGEM_EQUIPE_SITUACAO_TODOS } from '@/lib/mensagens';
import type { CargoFuncionario } from '@/types/enums';
import type { FiltroEquipe } from '@/types/equipe';

/** Tamanho de página da tela (padrão do contrato). */
export const TAMANHO_PAGINA_EQUIPE = 20;

/** Espera depois da última tecla antes de consultar a busca (evita uma chamada por letra). */
export const ATRASO_BUSCA_EQUIPE_MS = 400;

/** Limite da busca no contrato (até 60 caracteres). */
export const TAMANHO_MAXIMO_BUSCA_EQUIPE = 60;

/** Situação escolhida na barra: todos, só ativos ou só inativos. */
export type SituacaoFiltroEquipe = 'todos' | 'ativos' | 'inativos';

/** Opções da barra de situação, na ordem do desenho com "Todos" à frente. */
export const OPCOES_SITUACAO_EQUIPE: readonly { valor: SituacaoFiltroEquipe; rotulo: string }[] = [
  { valor: 'todos', rotulo: MENSAGEM_EQUIPE_SITUACAO_TODOS },
  { valor: 'ativos', rotulo: MENSAGEM_EQUIPE_SITUACAO_ATIVOS },
  { valor: 'inativos', rotulo: MENSAGEM_EQUIPE_SITUACAO_INATIVOS },
];

/** Estado dos filtros digitados ou escolhidos na tela. A situação começa em "Ativos", como no desenho. */
export interface EstadoFiltrosEquipe {
  busca: string;
  /** Vazio = todos os cargos. */
  cargo: CargoFuncionario | '';
  situacao: SituacaoFiltroEquipe;
}

export const ESTADO_FILTROS_EQUIPE_INICIAL: EstadoFiltrosEquipe = {
  busca: '',
  cargo: '',
  situacao: 'ativos',
};

/** Converte a situação no parâmetro `ativo` da API: ausente quando "todos", senão true ou false. */
export function ativoDaSituacao(situacao: SituacaoFiltroEquipe): boolean | undefined {
  if (situacao === 'ativos') return true;
  if (situacao === 'inativos') return false;
  return undefined;
}

/**
 * Filtro pronto para a API. A busca é aparada (sem espaços nas pontas) e limitada a 60 caracteres.
 * Busca vazia e cargo vazio não entram no filtro, para virarem parâmetro ausente.
 */
export function montarFiltroEquipe(pagina: number, estado: EstadoFiltrosEquipe): FiltroEquipe {
  const filtro: FiltroEquipe = { pagina, tamanhoPagina: TAMANHO_PAGINA_EQUIPE };
  const busca = estado.busca.trim().slice(0, TAMANHO_MAXIMO_BUSCA_EQUIPE).trim();
  if (busca !== '') filtro.busca = busca;
  if (estado.cargo !== '') filtro.cargo = estado.cargo;
  const ativo = ativoDaSituacao(estado.situacao);
  if (ativo !== undefined) filtro.ativo = ativo;
  return filtro;
}

/** Há algum filtro diferente do padrão de busca (usado para escolher a mensagem de lista vazia). */
export function temFiltroAplicado(estado: EstadoFiltrosEquipe): boolean {
  return estado.busca.trim() !== '' || estado.cargo !== '' || estado.situacao !== 'todos';
}
