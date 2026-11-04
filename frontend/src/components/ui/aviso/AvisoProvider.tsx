import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Icone } from '../Icone';
import { AvisoContexto, type AvisoContextoValor, type ItemAviso, type TipoAviso } from './AvisoContexto';

/** Tempo na tela de cada tipo de aviso. Erros ficam mais tempo para dar tempo de ler. */
const DURACAO_MS: Readonly<Record<TipoAviso, number>> = {
  sucesso: 5_000,
  erro: 8_000,
};

/** Máximo de avisos simultâneos; os mais antigos saem primeiro. */
const LIMITE_AVISOS = 4;

const APARENCIA: Readonly<Record<TipoAviso, { titulo: string; icone: string; classes: string }>> = {
  sucesso: {
    titulo: 'Sucesso',
    icone: 'check_circle',
    classes: 'border-status-sucesso text-status-sucesso',
  },
  erro: {
    titulo: 'Erro',
    icone: 'error',
    classes: 'border-status-perigo text-status-perigo',
  },
};

export interface AvisoProviderProps {
  children: ReactNode;
}

/** Provedor das notificações. Envolve a aplicação no `main.tsx`. */
export function AvisoProvider({ children }: AvisoProviderProps) {
  const [avisos, setAvisos] = useState<ItemAviso[]>([]);
  const proximoId = useRef(1);
  const temporizadores = useRef(new Map<number, number>());

  const remover = useCallback((id: number) => {
    setAvisos((atuais) => atuais.filter((aviso) => aviso.id !== id));
    const temporizador = temporizadores.current.get(id);
    if (temporizador !== undefined) {
      window.clearTimeout(temporizador);
      temporizadores.current.delete(id);
    }
  }, []);

  const adicionar = useCallback(
    (tipo: TipoAviso, mensagem: string) => {
      const id = proximoId.current;
      proximoId.current += 1;
      setAvisos((atuais) => [...atuais, { id, tipo, mensagem }].slice(-LIMITE_AVISOS));
      temporizadores.current.set(
        id,
        window.setTimeout(() => remover(id), DURACAO_MS[tipo]),
      );
    },
    [remover],
  );

  useEffect(() => {
    const pendentes = temporizadores.current;
    return () => {
      pendentes.forEach((temporizador) => window.clearTimeout(temporizador));
      pendentes.clear();
    };
  }, []);

  const valor = useMemo<AvisoContextoValor>(
    () => ({
      sucesso: (mensagem) => adicionar('sucesso', mensagem),
      erro: (mensagem) => adicionar('erro', mensagem),
    }),
    [adicionar],
  );

  return (
    <AvisoContexto.Provider value={valor}>
      {children}
      <section
        aria-label="Notificações"
        className="pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-6 sm:right-6 sm:w-96"
      >
        {avisos.map((aviso) => {
          const aparencia = APARENCIA[aviso.tipo];
          return (
            <div
              key={aviso.id}
              role={aviso.tipo === 'erro' ? 'alert' : 'status'}
              className={`pointer-events-auto flex items-start gap-3 rounded-cartao border-l-4 bg-branco p-4 shadow-elevado ${aparencia.classes}`}
            >
              <Icone nome={aparencia.icone} preenchido />
              <div className="min-w-0 flex-1">
                <p className="text-corpo-denso font-semibold">{aparencia.titulo}</p>
                <p className="break-words text-corpo-denso text-texto">{aviso.mensagem}</p>
              </div>
              <button
                type="button"
                onClick={() => remover(aviso.id)}
                aria-label="Fechar notificação"
                className="-m-1 inline-flex h-7 w-7 items-center justify-center rounded-md text-texto-suave transition-colors hover:bg-offwhite hover:text-navy"
              >
                <Icone nome="close" tamanho={18} />
              </button>
            </div>
          );
        })}
      </section>
    </AvisoContexto.Provider>
  );
}
