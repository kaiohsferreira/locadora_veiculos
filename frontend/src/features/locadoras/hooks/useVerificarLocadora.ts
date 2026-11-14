import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { verificarCadastroEmpresa } from '@/api/empresas';
import {
  CHAVE_VERIFICACAO_LOCADORA,
  OPCOES_CONSULTA_VERIFICACAO_LOCADORA,
  verificarComFalhaAbertaLocadora,
  type ResultadoVerificacaoLocadora,
} from '@/features/locadoras/cadastro/verificacaoLocadora';
import type { CampoVerificacaoEmpresa, RespostaVerificarEmpresa } from '@/types/empresas';

/** Pergunta à API se já existe empresa com o valor (já normalizado) do campo. Nunca lança. */
export type VerificarEmpresa = (
  campo: CampoVerificacaoEmpresa,
  valorNormalizado: string,
) => Promise<ResultadoVerificacaoLocadora>;

/**
 * Verificação de CNPJ e e-mail já cadastrados (POST /api/empresas/verificar-cadastro). Usa o cache do
 * TanStack Query só para não repetir o mesmo valor (chave com o campo e o valor, 5 minutos) e para juntar
 * chamadas iguais em andamento. O valor consultado é dado comercial e fica só em memória: o provedor
 * apaga a família `verificar-empresa` ao sair do cadastro.
 *
 * Uma nova verificação do mesmo campo cancela a de outro valor que ainda esteja em andamento. Falha
 * (rede, 5xx, 429, 400), cancelamento e resposta fora do contrato viram `indisponivel`, sem lançar.
 */
export function useVerificarLocadora(): { verificar: VerificarEmpresa } {
  const queryClient = useQueryClient();

  const verificar = useCallback<VerificarEmpresa>(
    async (campo, valorNormalizado) => {
      const chave = [CHAVE_VERIFICACAO_LOCADORA, campo, valorNormalizado];
      const { resultado, respostaInvalida } = await verificarComFalhaAbertaLocadora(async () => {
        await queryClient.cancelQueries({
          queryKey: [CHAVE_VERIFICACAO_LOCADORA, campo],
          predicate: (consulta) => consulta.queryKey[2] !== valorNormalizado,
        });
        return queryClient.fetchQuery<RespostaVerificarEmpresa>({
          queryKey: chave,
          queryFn: ({ signal }) => verificarCadastroEmpresa({ campo, valor: valorNormalizado }, signal),
          ...OPCOES_CONSULTA_VERIFICACAO_LOCADORA,
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
