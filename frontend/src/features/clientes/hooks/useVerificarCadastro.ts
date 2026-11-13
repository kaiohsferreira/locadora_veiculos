import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { verificarCadastroCliente } from '@/api/clientes';
import {
  CHAVE_VERIFICACAO_CADASTRO,
  OPCOES_CONSULTA_VERIFICACAO,
  verificarComFalhaAberta,
  type ResultadoVerificacao,
} from '@/features/clientes/cadastro/verificacaoCadastro';
import type { CampoVerificacaoCadastro, RespostaVerificarCadastro } from '@/types/clientes';

/** Pergunta à API se já existe cliente ativo com o valor (já normalizado) do campo. Nunca lança. */
export type VerificarCadastro = (
  campo: CampoVerificacaoCadastro,
  valorNormalizado: string,
) => Promise<ResultadoVerificacao>;

/**
 * Verificação de CPF, e-mail e CNH já cadastrados (POST /api/clientes/verificar-cadastro).
 * Usa o cache do TanStack Query só para não repetir o mesmo valor (chave com o campo e o valor, 5 minutos)
 * e para juntar chamadas iguais em andamento. O valor consultado é dado pessoal: fica só em memória e o
 * `ProvedorCadastro` apaga a família `['verificar-cadastro']` ao sair do cadastro.
 *
 * Uma nova verificação do mesmo campo cancela a de outro valor que ainda esteja em andamento.
 * A decisão (falha aberta e leitura da resposta) é da lógica pura `verificarComFalhaAberta`: falha
 * (rede, 5xx, 429, 400), cancelamento e resposta fora do contrato viram `'indisponivel'`, sem lançar e
 * sem mensagem, porque a verificação não pode impedir o cadastro (o 409 do envio é a autoridade).
 */
export function useVerificarCadastro(): { verificar: VerificarCadastro } {
  const queryClient = useQueryClient();

  const verificar = useCallback<VerificarCadastro>(
    async (campo, valorNormalizado) => {
      const chave = [CHAVE_VERIFICACAO_CADASTRO, campo, valorNormalizado];
      const { resultado, respostaInvalida } = await verificarComFalhaAberta(async () => {
        await queryClient.cancelQueries({
          queryKey: [CHAVE_VERIFICACAO_CADASTRO, campo],
          predicate: (consulta) => consulta.queryKey[2] !== valorNormalizado,
        });
        return queryClient.fetchQuery<RespostaVerificarCadastro>({
          queryKey: chave,
          queryFn: ({ signal }) => verificarCadastroCliente({ campo, valor: valorNormalizado }, signal),
          ...OPCOES_CONSULTA_VERIFICACAO,
        });
      });
      // Resposta fora do contrato não pode ficar 5 minutos em cache como dado: a próxima tentativa refaz a chamada.
      if (respostaInvalida) queryClient.removeQueries({ queryKey: chave, exact: true });
      return resultado;
    },
    [queryClient],
  );

  return { verificar };
}
