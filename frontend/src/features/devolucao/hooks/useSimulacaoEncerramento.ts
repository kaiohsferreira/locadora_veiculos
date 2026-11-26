import { useQuery } from '@tanstack/react-query';
import { simularCobrancasExtras } from '@/api/cobrancasExtras';
import { chaveSimulacaoEncerramento } from './chavesDevolucao';

/**
 * Detalhamento das cobranças extras (memória de cálculo) para o cartão da tela 06. Usa a simulação do servidor, que não grava
 * nada. O total confirmado NÃO sai daqui: vem de valorTotalPrevisto do contexto. Só roda com a vistoria de saída registrada e
 * para quem pode encerrar; a chave muda com a vistoria de saída, para não mostrar uma memória antiga.
 */
export function useSimulacaoEncerramento(id: number, versaoSaida: number | null, habilitada: boolean) {
  return useQuery({
    queryKey: [...chaveSimulacaoEncerramento(id), versaoSaida],
    queryFn: () => simularCobrancasExtras(id),
    enabled: habilitada && versaoSaida !== null,
    retry: false,
    gcTime: 0,
  });
}
