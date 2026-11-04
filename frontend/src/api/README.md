# src/api

Por enquanto só existe `client.ts`: a instância única do Axios com os dois
interceptadores (token na requisição e conversão do ProblemDetails em `ErroApi`
na resposta).

## Regra: nenhum endpoint inventado

Cada módulo ganha o seu arquivo aqui **somente quando o contrato do backend
sair** (ex.: `veiculos.ts`, `reservas.ts`), com as funções tipadas de chamada.
Os tipos dos DTOs vão para `src/types/`, exatamente como no contrato.

## Como cada módulo vai ficar

```ts
// src/api/<modulo>.ts  (funções tipadas, usando a instância única)
import { api } from './client';
import type { Pagina } from '@/types/api';
import type { MeuDto } from '@/types/<modulo>';

export async function listarAlgo(): Promise<Pagina<MeuDto>> {
  const { data } = await api.get<Pagina<MeuDto>>('<rota exata do contrato>');
  return data;
}
```

```ts
// src/features/<processo>/hooks/useAlgo.ts  (único ponto de acesso das telas)
import { useQuery } from '@tanstack/react-query';
import { listarAlgo } from '@/api/<modulo>';

export function useAlgo() {
  return useQuery({ queryKey: ['<modulo>', 'lista'], queryFn: listarAlgo });
}
```

- Nenhum componente importa `client.ts` nem chama o Axios direto.
- Toda falha chega como `ErroApi` (`status`, `mensagem`, `erros?`). Para exibir,
  use `obterMensagemErro(erro)` de `@/lib/erros` com `useAviso().erro(...)` ou
  o componente `EstadoErro`.
- Toda mutação invalida as queries afetadas (`queryClient.invalidateQueries`)
  e dá retorno visível com `useAviso().sucesso(...)`.
