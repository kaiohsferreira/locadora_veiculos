import { useMutation, type UseMutationResult } from '@tanstack/react-query';
import { loginComGoogle } from '@/api/autenticacao';
import { sessaoJaVencida } from '@/features/clientes/acesso/sessaoJaVencida';
import { converterResultadoLoginGoogle } from '@/features/clientes/google/resultadoLoginGoogle';
import { converterRespostaLogin } from '@/lib/conversaoLogin';
import { limparCredencialGoogle } from '@/lib/credencialGoogle';
import { ACESSO_ERRO_SESSAO_NAO_ABERTA, MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { ErroApi } from '@/types/api';
import type { GoogleLoginVO, ResultadoLoginGoogle } from '@/types/autenticacao';

/**
 * Login com Google (POST /api/auth/google/login). Resposta incoerente vira erro genérico.
 * Quando AUTENTICADO, abre a sessão pelo mesmo caminho do login por CPF (conversão, checagem de
 * sessão já vencida e `entrar`). Nas demais situações, quem chama decide o que fazer.
 * `gcTime: 0`: a credencial não fica no cache do TanStack Query depois da resposta.
 */
export function useLoginGoogle(): UseMutationResult<ResultadoLoginGoogle, Error, GoogleLoginVO> {
  const { entrar } = useAutenticacao();

  return useMutation<ResultadoLoginGoogle, Error, GoogleLoginVO>({
    mutationFn: async (requisicao) => {
      const resposta = await loginComGoogle(requisicao);
      const resultado = converterResultadoLoginGoogle(resposta);
      if (resultado === null) {
        throw new ErroApi({ status: 0, mensagem: MENSAGEM_ERRO_API_PADRAO });
      }
      if (
        resultado.situacao === 'AUTENTICADO' &&
        sessaoJaVencida(converterRespostaLogin(resultado.sessao), new Date())
      ) {
        throw new ErroApi({ status: 0, mensagem: ACESSO_ERRO_SESSAO_NAO_ABERTA });
      }
      return resultado;
    },
    gcTime: 0,
    onSuccess: (resultado) => {
      if (resultado.situacao !== 'AUTENTICADO') return;
      limparCredencialGoogle();
      entrar(resultado.sessao.token, converterRespostaLogin(resultado.sessao));
    },
  });
}
