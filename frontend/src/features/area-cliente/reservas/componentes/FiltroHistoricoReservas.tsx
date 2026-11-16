import { Icone } from '@/components/ui';
import {
  MENSAGEM_RESERVAS_BUSCA_BOTAO,
  MENSAGEM_RESERVAS_BUSCA_PLACEHOLDER,
  MENSAGEM_RESERVAS_BUSCA_ROTULO,
  MENSAGEM_RESERVAS_HISTORICO_BUSCA_FORM,
} from '@/lib/mensagens';
import { TAMANHO_MAXIMO_BUSCA_RESERVAS } from '@/features/area-cliente/reservas/parametrosReservas';

const ID_CAMPO_BUSCA = 'reservas-busca-historico';

export interface FiltroHistoricoReservasProps {
  /** Texto que está digitado agora. */
  valor: string;
  aoMudar: (texto: string) => void;
  /** Filtrar na hora, sem esperar a pausa da digitação. */
  aoEnviar: () => void;
}

/** Busca do histórico por código ou modelo. A consulta roda no servidor (o parâmetro busca), não na página. */
export function FiltroHistoricoReservas({ valor, aoMudar, aoEnviar }: FiltroHistoricoReservasProps) {
  return (
    <form
      aria-label={MENSAGEM_RESERVAS_HISTORICO_BUSCA_FORM}
      onSubmit={(evento) => {
        evento.preventDefault();
        aoEnviar();
      }}
      className="flex w-full flex-wrap items-center gap-2 sm:w-auto"
    >
      <label htmlFor={ID_CAMPO_BUSCA} className="sr-only">
        {MENSAGEM_RESERVAS_BUSCA_ROTULO}
      </label>
      <div className="relative w-full sm:w-64">
        <Icone
          nome="search"
          tamanho={16}
          className="pointer-events-none absolute left-2.5 top-2 text-texto-suave"
        />
        <input
          id={ID_CAMPO_BUSCA}
          type="text"
          value={valor}
          maxLength={TAMANHO_MAXIMO_BUSCA_RESERVAS}
          placeholder={MENSAGEM_RESERVAS_BUSCA_PLACEHOLDER}
          onChange={(evento) => aoMudar(evento.target.value)}
          className="h-9 w-full rounded-sm bg-branco pl-8 pr-3 text-legenda text-texto shadow-sm placeholder:text-texto-suave focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel"
        />
      </div>
      <button
        type="submit"
        className="flex h-9 items-center gap-1 rounded-sm bg-steel-suave px-3 text-rotulo font-semibold text-navy transition-colors hover:bg-steel-alto focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel"
      >
        <Icone nome="filter_list" tamanho={16} />
        <span>{MENSAGEM_RESERVAS_BUSCA_BOTAO}</span>
      </button>
    </form>
  );
}
