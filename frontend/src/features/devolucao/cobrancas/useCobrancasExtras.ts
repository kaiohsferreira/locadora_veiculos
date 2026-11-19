import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { buscarApuracaoCobrancasExtras, registrarApuracaoCobrancasExtras, simularCobrancasExtras } from '@/api/cobrancasExtras';
import type { ConfirmarApuracaoCobrancasExtras } from '@/types/cobrancasExtras';
import { ErroApi } from '@/types/api';

export const chaveApuracao = (id: number) => ['apuracao-cobrancas-extras', id] as const;
export function useApuracaoCobrancasExtras(id: number) {
  return useQuery({ queryKey: chaveApuracao(id), queryFn: ({ signal }) => buscarApuracaoCobrancasExtras(id, signal), retry: false, staleTime: 0, refetchOnMount: 'always' });
}
export function useSimularCobrancasExtras(id: number) {
  return useMutation({ mutationFn: () => simularCobrancasExtras(id), retry: false, gcTime: 0 });
}
export function useRegistrarApuracaoCobrancasExtras(id: number) {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: async (confirmacao: ConfirmarApuracaoCobrancasExtras) => {
      try { return await registrarApuracaoCobrancasExtras(id, confirmacao); }
      catch (erro: unknown) {
        if (erro instanceof ErroApi && (erro.status === 0 || erro.status === 409)) {
          // Uma resposta perdida pode ocultar o commit; GET recupera sem repetir o POST.
          const persistida = await buscarApuracaoCobrancasExtras(id);
          if (persistida !== null) return persistida;
        }
        throw erro;
      }
    },
    retry: false, gcTime: 0,
    onSuccess: async apuracao => {
      await cliente.cancelQueries({ queryKey: chaveApuracao(id) });
      cliente.setQueryData(chaveApuracao(id), apuracao);
    },
    onError: async () => { await cliente.invalidateQueries({ queryKey: chaveApuracao(id) }); },
  });
}
