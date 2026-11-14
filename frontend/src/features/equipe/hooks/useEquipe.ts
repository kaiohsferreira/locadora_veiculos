import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarFuncionarioEquipe, listarEquipe } from '@/api/equipe';
import type { ErroApi } from '@/types/api';
import type { FiltroEquipe, FuncionarioEquipe, RespostaEquipe } from '@/types/equipe';

/** Família de chaves da equipe: as mutações invalidam esta raiz para atualizar lista, totais e consultas. */
export const CHAVE_EQUIPE = 'equipe';

/**
 * Lista paginada da equipe com os totais. Ao trocar de página ou de filtro, mantém a lista anterior na tela até a nova
 * chegar (sem piscar). Erros 4xx não são repetidos automaticamente (regra do QueryClient).
 */
export function useEquipe(filtro: FiltroEquipe): UseQueryResult<RespostaEquipe, ErroApi | Error> {
  return useQuery<RespostaEquipe, ErroApi | Error>({
    queryKey: [CHAVE_EQUIPE, 'lista', filtro],
    queryFn: () => listarEquipe(filtro),
    placeholderData: keepPreviousData,
  });
}

/**
 * Consulta de um funcionário para a edição. Quando `idFuncionario` é null, a consulta não roda. `gcTime: 0` tira os
 * dados pessoais do cache assim que o painel fecha.
 */
export function useFuncionarioEquipe(
  idFuncionario: number | null,
): UseQueryResult<FuncionarioEquipe, ErroApi | Error> {
  return useQuery<FuncionarioEquipe, ErroApi | Error>({
    queryKey: [CHAVE_EQUIPE, 'funcionario', idFuncionario],
    queryFn: () => buscarFuncionarioEquipe(idFuncionario as number),
    enabled: idFuncionario !== null,
    gcTime: 0,
    retry: false,
  });
}
