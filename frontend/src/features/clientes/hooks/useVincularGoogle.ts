import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { vincularGoogle } from '@/api/autenticacao';
import { sessaoJaVencida } from '@/features/clientes/acesso/sessaoJaVencida';
import { converterRespostaLogin } from '@/lib/conversaoLogin';
import { limparCredencialGoogle } from '@/lib/credencialGoogle';
import { ACESSO_ERRO_SESSAO_NAO_ABERTA } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { ErroApi } from '@/types/api';
import type { GoogleVinculoVO, RespostaLogin } from '@/types/autenticacao';

/**
 * Vínculo da conta Google com o cliente (POST /api/auth/google/vinculo): prova a senha da conta
 * AutoVia e abre a sessão pelo mesmo caminho do login por CPF. No sucesso, a credencial sai da
 * memória. `gcTime: 0`: CPF e senha não ficam no cache depois do envio.
 */
export function useVincularGoogle(): UseMutationResult<RespostaLogin, Error, GoogleVinculoVO> {
  const { entrar } = useAutenticacao();

  return useMutation<RespostaLogin, Error, GoogleVinculoVO>({
    mutationFn: async (requisicao) => {
      const resposta = await vincularGoogle(requisicao);
      if (sessaoJaVencida(converterRespostaLogin(resposta), new Date())) {
        throw new ErroApi({ status: 0, mensagem: ACESSO_ERRO_SESSAO_NAO_ABERTA });
      }
      return resposta;
    },
    gcTime: 0,
    onSuccess: (resposta) => {
      limparCredencialGoogle();
      entrar(resposta.token, converterRespostaLogin(resposta));
    },
  });
}
