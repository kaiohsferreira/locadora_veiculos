import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import type { GravidadeAvaria } from '@/types/enums';
import { estiloSeloGravidade } from './estiloSeloGravidade';
import { Icone } from './Icone';

export interface SeloGravidadeProps {
  /** Gravidade da avaria: Leve, Média ou Grave. */
  gravidade: GravidadeAvaria;
  className?: string;
}

/**
 * Selo de gravidade único das telas 06 e 19: ícone decorativo e o nome da gravidade como texto (nunca só cor).
 * Os nomes vêm de MENSAGENS_DEVOLUCAO.gravidades, já aprovados.
 */
export function SeloGravidade({ gravidade, className = '' }: SeloGravidadeProps) {
  const estilo = estiloSeloGravidade(gravidade);
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-micro font-bold ${estilo.cores} ${className}`}
    >
      <Icone nome={estilo.icone} tamanho={14} />
      {MENSAGENS_DEVOLUCAO.gravidades[gravidade]}
    </span>
  );
}
