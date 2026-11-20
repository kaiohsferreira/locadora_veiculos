import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { buscarConfiguracaoCobrancasExtras, criarConfiguracaoCobrancasExtras, desativarConfiguracaoCobrancasExtras, editarConfiguracaoCobrancasExtras, listarConfiguracoesCobrancasExtras } from '@/api/configuracoesCobrancasExtras';
import type { DadosConfiguracaoCobrancasExtras, EditarConfiguracaoCobrancasExtras, FiltroConfiguracoesCobrancasExtras } from '@/types/configuracoesCobrancasExtras';

export const CHAVE_CONFIGURACOES_COBRANCAS = ['configuracoes-cobrancas-extras'] as const;
export function useConfiguracoesCobrancas(filtro: FiltroConfiguracoesCobrancasExtras) {
  return useQuery({ queryKey: [...CHAVE_CONFIGURACOES_COBRANCAS, 'lista', filtro], queryFn: ({ signal }) => listarConfiguracoesCobrancasExtras(filtro, signal), retry: false });
}
export function useConfiguracaoCobrancas(id: number | null) {
  return useQuery({ queryKey: [...CHAVE_CONFIGURACOES_COBRANCAS, id], queryFn: ({ signal }) => buscarConfiguracaoCobrancasExtras(id as number, signal), enabled: id !== null, retry: false });
}
export interface SalvarConfiguracaoCobrancas { id: number | null; dados: DadosConfiguracaoCobrancasExtras; versaoEsperada?: number }
export function useSalvarConfiguracaoCobrancas() {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dados, versaoEsperada }: SalvarConfiguracaoCobrancas) => {
      if (id === null) return criarConfiguracaoCobrancasExtras(dados);
      if (!versaoEsperada || !Number.isInteger(versaoEsperada) || versaoEsperada <= 0) throw new Error('Versão de edição ausente.');
      const requisicao: EditarConfiguracaoCobrancasExtras = { ...dados, versaoEsperada };
      await editarConfiguracaoCobrancasExtras(id, requisicao);
      return null;
    },
    retry: false, gcTime: 0,
    onSuccess: () => cliente.invalidateQueries({ queryKey: CHAVE_CONFIGURACOES_COBRANCAS }),
  });
}
export function useDesativarConfiguracaoCobrancas() {
  const cliente = useQueryClient();
  return useMutation({ mutationFn: desativarConfiguracaoCobrancasExtras, retry: false, gcTime: 0,
    onSuccess: () => cliente.invalidateQueries({ queryKey: CHAVE_CONFIGURACOES_COBRANCAS }) });
}
