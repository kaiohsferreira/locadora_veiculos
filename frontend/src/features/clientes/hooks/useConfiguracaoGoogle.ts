import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { obterConfiguracaoGoogle } from '@/api/autenticacao';
import type { ReturnGoogleConfiguracaoVO } from '@/types/autenticacao';

/** Chave fixa: a configuração não depende de quem está na tela. */
export const CHAVE_CONFIGURACAO_GOOGLE = ['auth', 'google', 'configuracao'] as const;

/** Cinco minutos: a configuração muda pouco e não precisa ser buscada a cada troca de perfil. */
const TEMPO_FRESCO_MS = 5 * 60_000;

/**
 * Configuração pública do login com Google. Só busca quando `ativo` é verdadeiro (perfil Cliente).
 * Sem novas tentativas: falha ou `habilitado` falso só esconde o bloco, sem mensagem de erro (RN3.1).
 */
export function useConfiguracaoGoogle(ativo: boolean): UseQueryResult<ReturnGoogleConfiguracaoVO, Error> {
  return useQuery<ReturnGoogleConfiguracaoVO, Error>({
    queryKey: CHAVE_CONFIGURACAO_GOOGLE,
    queryFn: obterConfiguracaoGoogle,
    enabled: ativo,
    retry: false,
    refetchOnWindowFocus: false,
    staleTime: TEMPO_FRESCO_MS,
  });
}
