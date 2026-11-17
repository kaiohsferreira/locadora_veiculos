import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { cadastrarVeiculo } from '@/api/veiculos';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';
import type { ErroApi } from '@/types/api';
import type { VeiculoRequisicao, VeiculoRetorno } from '@/types/veiculos';
import { chaveFamiliaVeiculos, chaveFotosVeiculo } from './chavesVeiculos';

/**
 * Cadastra um veículo (POST, 201). Ao dar certo:
 * - invalida a família de veículos (lista e detalhes);
 * - grava lista de fotos VAZIA na chave de fotos do veículo novo. Sem isso, o bloco de fotos mostraria a consulta
 *   pendente e esconderia contador e miniaturas enquanto as fotos da fila são enviadas.
 * O cadastro nunca é desfeito por falha de foto: isso é tratado pela fila de fotos.
 */
export function useCadastrarVeiculo(): UseMutationResult<VeiculoRetorno, ErroApi | Error, VeiculoRequisicao> {
  const queryClient = useQueryClient();
  return useMutation<VeiculoRetorno, ErroApi | Error, VeiculoRequisicao>({
    mutationFn: cadastrarVeiculo,
    gcTime: 0,
    onSuccess: (veiculo) => {
      const invalidacao = queryClient.invalidateQueries({ queryKey: chaveFamiliaVeiculos() });
      const fotosDoNovo: FotoDoVeiculo[] = [];
      queryClient.setQueryData<FotoDoVeiculo[]>(chaveFotosVeiculo(veiculo.idVeiculo), fotosDoNovo);
      return invalidacao;
    },
  });
}
