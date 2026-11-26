import { useEffect, useState } from 'react';

/**
 * Relógio da tela: devolve o instante atual e o atualiza a cada `intervaloMs`. O temporizador é limpo quando a tela
 * sai. Só é usado pelo cronômetro; a data da devolução continua vindo do servidor.
 */
export function useAgora(intervaloMs: number): Date {
  const [agora, setAgora] = useState<Date>(() => new Date());

  useEffect(() => {
    const temporizador = window.setInterval(() => setAgora(new Date()), intervaloMs);
    return () => window.clearInterval(temporizador);
  }, [intervaloMs]);

  return agora;
}
