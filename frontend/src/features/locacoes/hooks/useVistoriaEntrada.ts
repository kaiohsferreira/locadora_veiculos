import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { buscarContextoVistoriaEntrada, registrarVistoriaEntrada } from '@/api/vistorias';
import { ErroApi } from '@/types/api';
import type { ContextoVistoriaEntrada, RegistrarVistoriaEntrada } from '@/types/vistorias';

export const chaveVistoriaEntrada = (id: number | null) => ['vistoria-entrada', id] as const;

export function useVistoriaEntrada(id: number | null) {
  return useQuery({
    queryKey: chaveVistoriaEntrada(id),
    queryFn: () => buscarContextoVistoriaEntrada(id as number),
    enabled: id !== null,
    retry: false,
  });
}

export function useRegistrarVistoriaEntrada(id: number) {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: (dados: RegistrarVistoriaEntrada) => registrarVistoriaEntrada(id, dados),
    retry: false,
    onSuccess: async (vistoria) => {
      // Uma consulta iniciada antes do POST não pode sobrescrever o sucesso com ausência antiga.
      await cliente.cancelQueries({ queryKey: chaveVistoriaEntrada(id) });
      cliente.setQueryData<ContextoVistoriaEntrada>(chaveVistoriaEntrada(id), (anterior) =>
        anterior ? { ...anterior, vistoria } : anterior,
      );
    },
    onError: async (erro: unknown) => {
      if (erro instanceof ErroApi && erro.status === 409) {
        await cliente.invalidateQueries({ queryKey: chaveVistoriaEntrada(id) });
      }
    },
  });
}
