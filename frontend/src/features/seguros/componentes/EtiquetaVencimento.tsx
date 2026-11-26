import { Icone } from '@/components/ui';
import { descreverVencimento, type TomVencimento } from '@/features/seguros/regras/vencimento';
import type { Apolice } from '@/types/seguros';

const CLASSES_TOM: Readonly<Record<TomVencimento, string>> = {
  perigo: 'bg-status-perigo-fundo text-status-perigo',
  pendente: 'bg-status-pendente-fundo text-status-pendente',
  sucesso: 'bg-status-sucesso-fundo text-status-sucesso',
};

export interface EtiquetaVencimentoProps {
  apolice: Pick<Apolice, 'diasParaVencer' | 'venceEm60Dias'>;
}

/**
 * Status da apólice (desenho 13): ícone + texto ("Vence em breve (30 dias)", "Vigente", "Vence hoje", "Vencida há 5
 * dias"), nunca só cor (RF-41).
 */
export function EtiquetaVencimento({ apolice }: EtiquetaVencimentoProps) {
  const descricao = descreverVencimento(apolice);
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-lg px-2 py-1 text-legenda font-semibold ${CLASSES_TOM[descricao.tom]}`}
    >
      <Icone nome={descricao.icone} tamanho={14} />
      {descricao.texto}
    </span>
  );
}
