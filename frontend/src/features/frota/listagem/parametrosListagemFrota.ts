/**
 * Regras da listagem da frota (tela 11), sem React: como o estado da tela vira o filtro da API, quais são as pílulas de
 * situação e quando a página volta para a primeira. A chamada HTTP fica em `@/api/veiculos`.
 */
import type { Pagina } from '@/types/api';
import type { StatusVeiculo } from '@/types/enums';
import type { FiltroListagemVeiculos } from '@/types/veiculos';
import {
  MENSAGEM_FROTA_PILULA_DISPONIVEIS,
  MENSAGEM_FROTA_PILULA_LOCADOS,
  MENSAGEM_FROTA_PILULA_MANUTENCAO,
  MENSAGEM_FROTA_PILULA_RESERVADOS,
  MENSAGEM_FROTA_PILULA_TODOS,
} from '@/lib/mensagens';

/** Linhas por página da tela (o contrato aceita de 1 a 100). */
export const TAMANHO_PAGINA_FROTA_PADRAO = 10;
export const OPCOES_LINHAS_POR_PAGINA_FROTA: readonly number[] = [10, 25, 50, 100];

/** Espera depois da última tecla antes de consultar a busca (uma consulta por pausa, não por tecla). */
export const ATRASO_BUSCA_FROTA_MS = 400;

/** Teto da busca no contrato (TamanhoMaximoBuscaVeiculo = 60, o maior campo buscado). */
export const TAMANHO_MAXIMO_BUSCA_FROTA = 60;

/** Pílula escolhida: vazio = todos os status; senão, o status que a API filtra. */
export type SituacaoPilulaFrota = StatusVeiculo | '';

export interface PilulaFrota {
  valor: SituacaoPilulaFrota;
  rotulo: string;
}

/** Pílulas na ordem do desenho: Todos, Disponíveis, Locados, Manutenção e, no lugar de "Aguardando Laudo", Reservados. */
export const PILULAS_FROTA: readonly PilulaFrota[] = [
  { valor: '', rotulo: MENSAGEM_FROTA_PILULA_TODOS },
  { valor: 'DISPONIVEL', rotulo: MENSAGEM_FROTA_PILULA_DISPONIVEIS },
  { valor: 'LOCADO', rotulo: MENSAGEM_FROTA_PILULA_LOCADOS },
  { valor: 'MANUTENCAO', rotulo: MENSAGEM_FROTA_PILULA_MANUTENCAO },
  { valor: 'RESERVADO', rotulo: MENSAGEM_FROTA_PILULA_RESERVADOS },
];

/** Filtros digitados ou escolhidos na tela. Categoria vazia = todas. */
export interface EstadoFiltrosFrota {
  busca: string;
  idCategoria: number | '';
  status: SituacaoPilulaFrota;
}

export const ESTADO_FILTROS_FROTA_INICIAL: EstadoFiltrosFrota = {
  busca: '',
  idCategoria: '',
  status: '',
};

/** Página, tamanho e filtros da listagem, como a tela guarda. */
export interface EstadoListagemFrota {
  pagina: number;
  tamanhoPagina: number;
  filtros: EstadoFiltrosFrota;
}

export const ESTADO_LISTAGEM_FROTA_INICIAL: EstadoListagemFrota = {
  pagina: 1,
  tamanhoPagina: TAMANHO_PAGINA_FROTA_PADRAO,
  filtros: ESTADO_FILTROS_FROTA_INICIAL,
};

/** O que a pessoa mudou na tela: um filtro, o tamanho da página ou a página em si. */
export type MudancaListagemFrota =
  | { tipo: 'filtro'; parcial: Partial<EstadoFiltrosFrota> }
  | { tipo: 'linhas'; tamanhoPagina: number }
  | { tipo: 'pagina'; pagina: number };

/**
 * Se o parcial muda algum filtro do estado atual. Busca compara o termo aparado (espaços não contam); categoria e
 * pílula comparam o valor. Campo ausente no parcial não conta como mudança.
 */
export function filtroMudou(atual: EstadoFiltrosFrota, parcial: Partial<EstadoFiltrosFrota>): boolean {
  if (parcial.busca !== undefined && termoBuscaFrota(parcial.busca) !== termoBuscaFrota(atual.busca)) return true;
  if (parcial.idCategoria !== undefined && parcial.idCategoria !== atual.idCategoria) return true;
  if (parcial.status !== undefined && parcial.status !== atual.status) return true;
  return false;
}

/**
 * Aplica uma mudança. Trocar filtro (busca, pílula ou categoria) ou o tamanho da página volta para a página 1; trocar
 * de página só muda a página. Escolher o filtro que já está marcado não muda nada (a página fica como está). Não altera
 * o estado recebido.
 */
export function alterarListagemFrota(estado: EstadoListagemFrota, mudanca: MudancaListagemFrota): EstadoListagemFrota {
  if (mudanca.tipo === 'pagina') return { ...estado, pagina: mudanca.pagina };
  if (mudanca.tipo === 'linhas') return { ...estado, tamanhoPagina: mudanca.tamanhoPagina, pagina: 1 };
  if (!filtroMudou(estado.filtros, mudanca.parcial)) return estado;
  return { ...estado, filtros: { ...estado.filtros, ...mudanca.parcial }, pagina: 1 };
}

/**
 * Filtro pronto para a API. A busca é aparada e limitada a 60 caracteres; busca vazia ou só com espaços não entra.
 * Categoria e status só entram quando escolhidos.
 */
export function montarFiltroListagemFrota(
  pagina: number,
  tamanhoPagina: number,
  filtros: EstadoFiltrosFrota,
): FiltroListagemVeiculos {
  const filtro: FiltroListagemVeiculos = { pagina, tamanhoPagina };
  if (filtros.status !== '') filtro.status = filtros.status;
  if (filtros.idCategoria !== '') filtro.idCategoria = filtros.idCategoria;
  const busca = termoBuscaFrota(filtros.busca);
  if (busca !== '') filtro.busca = busca;
  return filtro;
}

/**
 * Termo da busca como a API recebe: aparado e limitado ao teto. Vazio = sem busca. É a identidade da busca na tela:
 * espaços digitados não mudam o termo, nem a página, nem a chave da consulta.
 */
export function termoBuscaFrota(busca: string): string {
  return busca.trim().slice(0, TAMANHO_MAXIMO_BUSCA_FROTA).trim();
}

/**
 * Página depois que a busca entra na consulta (ou junto de uma troca de página). Volta para a página 1 só quando o
 * termo que a API recebe muda: espaços digitados antes ou depois do texto não mudam a consulta, então não tiram a
 * pessoa da página em que está. Se uma busca nova ainda não entrou na consulta, a troca de página também vai para 1.
 */
export function paginaAposAplicarBusca(buscaAplicada: string, novaBusca: string, paginaAtual: number): number {
  return termoBuscaFrota(buscaAplicada) === termoBuscaFrota(novaBusca) ? paginaAtual : 1;
}

/**
 * Página ajustada à resposta que a tela mostra. Só ajusta com resposta da consulta atual: enquanto a próxima carrega, a
 * tela mostra a resposta anterior, e ela não serve de base para corrigir a página. Página além da última volta para a
 * última existente.
 */
export function paginaAposResposta(
  paginaAtual: number,
  resposta: Pick<Pagina<unknown>, 'total' | 'tamanhoPagina'> | undefined,
  daConsultaAtual: boolean,
): number {
  if (resposta === undefined || !daConsultaAtual) return paginaAtual;
  const ultima = ultimaPaginaFrota(resposta.total, resposta.tamanhoPagina);
  return paginaAtual > ultima ? ultima : paginaAtual;
}

/**
 * Valores do rodapé. Pagina e linhas por página vêm do estado da tela (o que a pessoa escolheu); o total vem da resposta
 * que está na tela. Sem resposta, o rodapé não aparece.
 */
export function estadoRodapeFrota(
  estado: Pick<EstadoListagemFrota, 'pagina' | 'tamanhoPagina'>,
  resposta: Pick<Pagina<unknown>, 'total'> | undefined,
): { pagina: number; tamanhoPagina: number; total: number } | null {
  if (resposta === undefined) return null;
  return { pagina: estado.pagina, tamanhoPagina: estado.tamanhoPagina, total: resposta.total };
}

/**
 * Se a lista mostrada tem filtro, para escolher o texto de lista vazia. Enquanto a próxima resposta carrega, a tabela
 * mostra a última resposta recebida: então vale o filtro DELA, não o que foi digitado agora.
 */
export function temFiltroNaListaMostrada(
  temFiltroAtual: boolean,
  temFiltroDaUltimaResposta: boolean,
  aguardandoConsulta: boolean,
): boolean {
  return aguardandoConsulta ? temFiltroDaUltimaResposta : temFiltroAtual;
}

/** Última página de uma lista paginada: pelo menos 1, mesmo sem nenhum item. */
export function ultimaPaginaFrota(total: number, tamanhoPagina: number): number {
  return Math.max(1, Math.ceil(total / tamanhoPagina));
}

/** Há algum filtro aplicado (usado para escolher a mensagem de lista vazia). */
export function temFiltroFrotaAplicado(filtros: EstadoFiltrosFrota): boolean {
  return termoBuscaFrota(filtros.busca) !== '' || filtros.idCategoria !== '' || filtros.status !== '';
}
