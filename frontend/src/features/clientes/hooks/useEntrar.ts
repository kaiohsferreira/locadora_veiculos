import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { autenticar } from '@/api/autenticacao';
import { converterRespostaLogin } from '@/lib/conversaoLogin';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import type { RequisicaoLogin, RespostaLogin } from '@/types/autenticacao';

/**
 * Login no portal. Ao dar certo, abre a sessão (token + usuário) pelo contexto de autenticação.
 * É o único ponto que as telas usam para abrir sessão. Não guarda a senha em lugar nenhum.
 */
export function useEntrar(): UseMutationResult<RespostaLogin, Error, RequisicaoLogin> {
  const { entrar } = useAutenticacao();

  return useMutation<RespostaLogin, Error, RequisicaoLogin>({
    mutationFn: autenticar,
    // Sem cache depois da mutação: a senha enviada não fica guardada no QueryClient após a falha.
    gcTime: 0,
    onSuccess: (resposta) => {
      entrar(resposta.token, converterRespostaLogin(resposta));
    },
  });
}
