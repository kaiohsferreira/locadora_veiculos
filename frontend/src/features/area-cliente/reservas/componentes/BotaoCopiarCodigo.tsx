import { useEffect, useState } from 'react';
import { Icone, useAviso } from '@/components/ui';
import {
  MENSAGEM_RESERVAS_COPIADO,
  MENSAGEM_RESERVAS_COPIAR,
  MENSAGEM_RESERVAS_COPIA_FALHOU,
} from '@/lib/mensagens';

export interface BotaoCopiarCodigoProps {
  codigo: string;
}

/** Copia o código da reserva para a área de transferência. O texto vira "Copiado!" por 2 segundos. */
export function BotaoCopiarCodigo({ codigo }: BotaoCopiarCodigoProps) {
  const aviso = useAviso();
  const [copiado, setCopiado] = useState(false);

  useEffect(() => {
    if (!copiado) return undefined;
    const temporizador = window.setTimeout(() => setCopiado(false), 2000);
    return () => window.clearTimeout(temporizador);
  }, [copiado]);

  async function copiar() {
    try {
      await navigator.clipboard.writeText(codigo);
      setCopiado(true);
    } catch {
      aviso.erro(MENSAGEM_RESERVAS_COPIA_FALHOU);
    }
  }

  return (
    <button
      type="button"
      onClick={() => void copiar()}
      className="ml-1 flex items-center gap-1 text-micro font-semibold text-orange-escuro transition-colors hover:text-status-pendente focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel"
    >
      <Icone nome="content_copy" tamanho={14} />
      <span aria-live="polite">{copiado ? MENSAGEM_RESERVAS_COPIADO : MENSAGEM_RESERVAS_COPIAR}</span>
    </button>
  );
}
