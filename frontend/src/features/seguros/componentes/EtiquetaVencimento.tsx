import { Icone } from '@/components/ui';
import { descreverVencimento, type TomVencimento } from '@/features/seguros/regras/vencimento';
import type { Apolice } from '@/types/seguros';

const CLASSES_TOM: Readonly<Record<TomVencimento, string>> = {
  perigo: 'bg-status-perigo-fundo text-status-perigo',
  pendente: 'bg-status-pendente-fundo text-status-pendente',
  sucesso: 'bg-status-sucesso-fundo text-status-sucesso',
};

export interface EtiquetaVencimentoProps {
  apolice: Pick<Apolice, 'diasParaVencer' | 'venceEm60Dias' | 'dataFimStr'>;
}

/**
 * Etiqueta do vencimento da apólice: ícone + texto ("Vencida há 5 dias", "Vence hoje", "Vence em 30 dias", "Em dia"),
 * nunca só cor (RF-41). No "Em dia", mostra embaixo até quando vale.
 */
export function EtiquetaVencimento({ apolice }: EtiquetaVencimentoProps) {
  const descricao = descreverVencimento(apolice);
  return (
    <span className="inline-flex flex-col items-start gap-1">
      <span
        className={`inline-flex items-center gap-1 whitespace-nowrap rounded-full px-2 py-0.5 text-legenda font-semibold ${CLASSES_TOM[descricao.tom]}`}
      >
        <Icone nome={descricao.icone} tamanho={14} />
        {descricao.texto}
      </span>
      {descricao.detalhe && <span className="text-legenda text-texto-suave">{descricao.detalhe}</span>}
    </span>
  );
}
