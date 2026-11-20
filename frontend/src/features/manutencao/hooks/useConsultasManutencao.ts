import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarResumoManutencao, listarAlertasRevisao, listarManutencoes } from '@/api/manutencoes';
import type { ErroApi, Pagina } from '@/types/api';
import type {
  AlertasRevisaoRetorno,
  FiltroManutencao,
  ManutencaoListaRetorno,
  ResumoManutencaoRetorno,
} from '@/types/manutencao';
import { chaveAlertasRevisao, chaveListagemManutencoes, chaveResumoManutencao } from '@/features/manutencao/hooks/chavesManutencao';

/**
 * Listagem paginada de ordens para um filtro. Ao trocar de página ou de filtro, mantém a lista anterior na tela até a
 * nova chegar (sem piscar). Erros 4xx não são repetidos automaticamente (regra do QueryClient).
 */
export function useListarManutencoes(
  filtro: FiltroManutencao,
): UseQueryResult<Pagina<ManutencaoListaRetorno>, ErroApi | Error> {
  return useQuery<Pagina<ManutencaoListaRetorno>, ErroApi | Error>({
    queryKey: chaveListagemManutencoes(filtro),
    queryFn: () => listarManutencoes(filtro),
    placeholderData: keepPreviousData,
  });
}

/** Números do cabeçalho: OS Ativas e Custo Mês. */
export function useResumoManutencao(): UseQueryResult<ResumoManutencaoRetorno, ErroApi | Error> {
  return useQuery<ResumoManutencaoRetorno, ErroApi | Error>({
    queryKey: chaveResumoManutencao(),
    queryFn: buscarResumoManutencao,
  });
}

/** Veículos que pedem revisão (cartões do topo da tela). */
export function useAlertasRevisao(): UseQueryResult<AlertasRevisaoRetorno, ErroApi | Error> {
  return useQuery<AlertasRevisaoRetorno, ErroApi | Error>({
    queryKey: chaveAlertasRevisao(),
    queryFn: listarAlertasRevisao,
  });
}
