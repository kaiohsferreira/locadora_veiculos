/**
 * Espera antes de consultar a busca da frota. Cada nova tecla cancela a espera anterior; só a última vale, depois do
 * atraso. Os temporizadores entram por parâmetro: a tela usa os do navegador e os testes usam relógio falso.
 */

/** Temporizadores usados pela espera (os do navegador na tela). */
export interface RelogioBusca {
  agendar(acao: () => void, atrasoMs: number): unknown;
  cancelar(identificador: unknown): void;
}

export interface AgendadorBusca {
  /** Agenda `aplicar` com o valor, cancelando a espera anterior. */
  agendar(valor: string): void;
  /** Cancela a espera pendente, se houver (usado ao desmontar a tela). */
  cancelar(): void;
}

/** Relógio do navegador (`globalThis` aponta para o mesmo temporizador e funciona também nos testes). */
export const RELOGIO_NAVEGADOR: RelogioBusca = {
  agendar: (acao, atrasoMs) => globalThis.setTimeout(acao, atrasoMs),
  cancelar: (identificador) => globalThis.clearTimeout(identificador as ReturnType<typeof setTimeout>),
};

/** Cria o agendador: `aplicar` recebe só o valor da última tecla, depois de `atrasoMs` sem nova tecla. */
export function criarAgendadorBusca(
  aplicar: (valor: string) => void,
  atrasoMs: number,
  relogio: RelogioBusca = RELOGIO_NAVEGADOR,
): AgendadorBusca {
  let pendente: unknown = null;

  function cancelar(): void {
    if (pendente !== null) relogio.cancelar(pendente);
    pendente = null;
  }

  return {
    agendar(valor: string): void {
      cancelar();
      pendente = relogio.agendar(() => {
        pendente = null;
        aplicar(valor);
      }, atrasoMs);
    },
    cancelar,
  };
}
