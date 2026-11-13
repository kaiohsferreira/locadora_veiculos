import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { cadastrarCliente } from '@/api/clientes';
import type { RequisicaoCadastroCliente, RespostaCliente } from '@/types/clientes';

/**
 * Cadastro público do cliente (POST /api/clientes). A tela trata o resultado:
 * no sucesso ela faz o login automático; no erro, mostra a mensagem pelo `obterMensagemErro`.
 * A senha, o CPF, a CNH e o endereço vão só no corpo da requisição. `gcTime: 0` tira a
 * mutação do cache logo após terminar, para esses dados não ficarem na memória.
 */
export function useCadastrarCliente(): UseMutationResult<
  RespostaCliente,
  Error,
  RequisicaoCadastroCliente
> {
  return useMutation<RespostaCliente, Error, RequisicaoCadastroCliente>({
    mutationFn: cadastrarCliente,
    gcTime: 0,
  });
}
