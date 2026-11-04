import { useEffect, useState } from 'react';

/**
 * Devolve o valor somente depois que ele para de mudar por `atrasoMs`.
 * Uso típico: busca por placa ou nome sem disparar uma requisição a cada tecla.
 */
export function useDebounce<T>(valor: T, atrasoMs = 400): T {
  const [valorAtrasado, setValorAtrasado] = useState<T>(valor);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setValorAtrasado(valor), atrasoMs);
    return () => window.clearTimeout(temporizador);
  }, [valor, atrasoMs]);

  return valorAtrasado;
}
