import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { autenticar } from '@/api/autenticacao';
import { sessaoJaVencida } from '@/features/clientes/acesso/sessaoJaVencida';
import { converterRespostaLogin } from '@/lib/conversaoLogin';
import { ACESSO_ERRO_SESSAO_NAO_ABERTA } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { ErroApi } from '@/types/api';
import type { RequisicaoLogin, RespostaLogin } from '@/types/autenticacao';

/**
 * Login no portal. Ao dar certo, abre a sessão (token + usuário) pelo contexto de autenticação.
 * Se o token já nasce vencido (relógio do dispositivo atrasado), a sessão não é aberta e o
 * erro vai para a tela, em vez de o login parecer bem-sucedido sem avançar.
 * É o único ponto que as telas usam para abrir sessão. Não guarda a senha em lugar nenhum.
 */
export function useEntrar(): UseMutationResult<RespostaLogin, Error, RequisicaoLogin> {
  const { entrar } = useAutenticacao();

  return useMutation<RespostaLogin, Error, RequisicaoLogin>({
    mutationFn: async (requisicao) => {
      const resposta = await autenticar(requisicao);
      if (sessaoJaVencida(converterRespostaLogin(resposta), new Date())) {
        throw new ErroApi({ status: 0, mensagem: ACESSO_ERRO_SESSAO_NAO_ABERTA });
      }
      return resposta;
    },
    // Sem cache depois da mutação: a senha enviada não fica guardada no QueryClient após a falha.
    gcTime: 0,
    onSuccess: (resposta) => {
      entrar(resposta.token, converterRespostaLogin(resposta));
    },
  });
}
