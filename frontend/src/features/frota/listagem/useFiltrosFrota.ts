import { useContext } from 'react';
import { ContextoFiltrosFrota, type FiltrosListagemFrotaValor } from './ContextoFiltrosFrota';

/** Filtros da frota (busca, categoria, status, página e linhas) do provedor da rota. Só dentro do ProvedorFiltrosFrota. */
export function useFiltrosFrota(): FiltrosListagemFrotaValor {
  const valor = useContext(ContextoFiltrosFrota);
  if (valor === null) {
    throw new Error('useFiltrosFrota precisa estar dentro do ProvedorFiltrosFrota.');
  }
  return valor;
}
