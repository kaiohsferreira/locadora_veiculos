import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { buscarVeiculosPorPlaca } from '@/api/veiculos';
import { useDebounce } from '@/hooks/useDebounce';
import type { ErroApi } from '@/types/api';
import type { VeiculoPlaca } from '@/types/seguros';
import { chaveBuscaPlaca } from './chavesSeguros';

/** Espera depois da última tecla antes de buscar. */
export const ATRASO_BUSCA_PLACA_MS = 400;

/** A busca só começa com pelo menos 2 caracteres (letras e números da placa). */
export const MINIMO_CARACTERES_PLACA = 2;

/** Mantém só letras e números, em maiúsculas (o servidor normaliza do mesmo jeito). */
export function normalizarTermoPlaca(termo: string): string {
  return termo.replace(/[^a-z0-9]/gi, '').toUpperCase();
}

/**
 * Busca veículos pela placa enquanto a pessoa digita: espera a pausa (`useDebounce`), normaliza o termo e só consulta
 * com 2 caracteres ou mais. Devolve só o resumo que a busca mostra (placa, marca e modelo).
 */
export function useBuscaVeiculoPlaca(termo: string): UseQueryResult<VeiculoPlaca[], ErroApi | Error> {
  const termoAtrasado = normalizarTermoPlaca(useDebounce(termo, ATRASO_BUSCA_PLACA_MS));
  return useQuery<VeiculoPlaca[], ErroApi | Error>({
    queryKey: chaveBuscaPlaca(termoAtrasado),
    queryFn: async () => {
      const veiculos = await buscarVeiculosPorPlaca(termoAtrasado);
      return veiculos.map(({ idVeiculo, placa, marca, modelo }) => ({ idVeiculo, placa, marca, modelo }));
    },
    enabled: termoAtrasado.length >= MINIMO_CARACTERES_PLACA,
  });
}
