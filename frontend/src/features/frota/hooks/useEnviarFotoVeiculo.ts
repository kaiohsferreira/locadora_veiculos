import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { enviarFotoVeiculo } from '@/api/fotosVeiculo';
import type { ErroApi } from '@/types/api';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';

/** Dados de um envio: o veículo, o arquivo e, opcionais, o progresso e o cancelamento. */
export interface VariaveisEnvioFoto {
  idVeiculo: number;
  arquivo: File;
  aoProgredir?: (percentual: number) => void;
  sinal?: AbortSignal;
}

/**
 * Envia uma foto (uma chamada por foto). Não invalida as listas: quem envia em lote (a fila) invalida ao
 * terminar o lote.
 */
export function useEnviarFotoVeiculo(): UseMutationResult<FotoDoVeiculo, ErroApi | Error, VariaveisEnvioFoto> {
  return useMutation<FotoDoVeiculo, ErroApi | Error, VariaveisEnvioFoto>({
    mutationFn: ({ idVeiculo, arquivo, aoProgredir, sinal }) =>
      enviarFotoVeiculo(idVeiculo, arquivo, { aoProgredir, sinal }),
  });
}
