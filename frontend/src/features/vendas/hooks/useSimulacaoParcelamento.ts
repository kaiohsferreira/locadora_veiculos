import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query';
import { simularParcelamento } from '@/api/vendas';
import { useDebounce } from '@/hooks/useDebounce';
import { podeSimular } from '@/features/vendas/proposta/regrasSimulacao';
import type { ErroApi } from '@/types/api';
import type { ParametrosSimulacao, SimulacaoParcelamento } from '@/types/vendas';
import { chaveSimulacao } from './chavesVendas';

/** Espera depois da última tecla antes de simular, para não chamar a API a cada dígito. */
export const ATRASO_SIMULACAO_MS = 400;

/**
 * Simula o parcelamento enquanto a pessoa digita. A entrada e as parcelas passam pelo `useDebounce` separadamente,
 * como números (um objeto novo a cada render reiniciaria a espera sem parar), e só vão para a API quando são válidas
 * (`podeSimular`). Ao trocar os valores, o resultado anterior continua na tela até o novo chegar (`keepPreviousData`),
 * para o resumo não piscar.
 */
export function useSimulacaoParcelamento(
  idVeiculo: number | null,
  valorEntrada: number | null,
  numeroParcelas: number | null,
  valorVenda?: number,
): UseQueryResult<SimulacaoParcelamento, ErroApi | Error> {
  const entradaAtrasada = useDebounce(valorEntrada, ATRASO_SIMULACAO_MS);
  const parcelasAtrasadas = useDebounce(numeroParcelas, ATRASO_SIMULACAO_MS);
  const parametros: ParametrosSimulacao | null =
    entradaAtrasada === null || parcelasAtrasadas === null
      ? null
      : { valorEntrada: entradaAtrasada, numeroParcelas: parcelasAtrasadas };
  const habilitada = idVeiculo !== null && podeSimular(parametros, valorVenda);

  return useQuery<SimulacaoParcelamento, ErroApi | Error>({
    queryKey: chaveSimulacao(idVeiculo, parametros),
    queryFn: () => simularParcelamento(idVeiculo as number, parametros as ParametrosSimulacao),
    enabled: habilitada,
    placeholderData: keepPreviousData,
  });
}
