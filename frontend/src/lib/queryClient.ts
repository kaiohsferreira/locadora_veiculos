import { QueryClient } from '@tanstack/react-query';
import { ErroApi } from '@/types/api';

const MAXIMO_TENTATIVAS = 1;

/** Erros 4xx são definitivos (dado inválido, sem permissão, não encontrado): não adianta repetir. */
function deveTentarNovamente(falhas: number, erro: Error): boolean {
  if (erro instanceof ErroApi && erro.status >= 400 && erro.status < 500) return false;
  return falhas < MAXIMO_TENTATIVAS;
}

/** Cliente único do TanStack Query, com padrões da aplicação. */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: deveTentarNovamente,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
    mutations: {
      retry: false,
    },
  },
});
