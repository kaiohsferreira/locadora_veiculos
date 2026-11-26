import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { assinarContratoLocacao, buscarContratoLocacao } from '@/api/contratoLocacao';
import { ErroApi } from '@/types/api';
import type { AssinarContratoLocacao, ContratoLocacao } from '@/types/contratoLocacao';

export const chaveContratoLocacao = (id: number | null) => ['contrato-locacao', id] as const;

/** Documento do contrato da locação (sem efeito colateral no servidor). */
export function useContratoLocacao(id: number | null) {
  return useQuery({
    queryKey: chaveContratoLocacao(id),
    queryFn: ({ signal }) => buscarContratoLocacao(id as number, signal),
    enabled: id !== null,
    retry: false,
    // Sem nova leitura ao voltar o foco para a janela: ela descartaria o traço da assinatura em andamento.
    refetchOnWindowFocus: false,
  });
}

/**
 * Coleta a assinatura. Com sucesso, a resposta (201) já traz o contrato assinado e entra no cache sem nova leitura.
 * Com 409 (já assinado ou sem vistoria), relê o contrato para mostrar o estado que o servidor tem.
 */
export function useAssinarContratoLocacao(id: number) {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: (dados: AssinarContratoLocacao) => assinarContratoLocacao(id, dados),
    retry: false,
    onSuccess: async (contrato) => {
      await cliente.cancelQueries({ queryKey: chaveContratoLocacao(id) });
      cliente.setQueryData<ContratoLocacao>(chaveContratoLocacao(id), contrato);
    },
    onError: async (erro: unknown) => {
      if (erro instanceof ErroApi && erro.status === 409) {
        await cliente.invalidateQueries({ queryKey: chaveContratoLocacao(id) });
      }
    },
  });
}
