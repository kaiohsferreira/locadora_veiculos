import { useQuery } from '@tanstack/react-query';
import { buscarRetiradasDeHoje } from '@/api/painelOperacional';
import { chaveRetiradasDeHoje } from './chavesPainelOperacional';

/**
 * Consulta da lista de retiradas de hoje. staleTime 0: a lista é buscada a cada abertura da página de entrada, para não
 * mostrar como reserva o que já virou locação (locacao.contract.json, pagina_de_entrada_da_retirada.atualizacao).
 */
export function opcoesRetiradasDeHoje() {
  return {
    queryKey: chaveRetiradasDeHoje(),
    queryFn: () => buscarRetiradasDeHoje(),
    retry: false as const,
    staleTime: 0,
  };
}

export function useRetiradasDeHoje() {
  return useQuery(opcoesRetiradasDeHoje());
}
