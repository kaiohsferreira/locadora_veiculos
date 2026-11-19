import { ROTULO_STATUS_VEICULO, type StatusVeiculo } from '@/types/enums';

/**
 * Cores da etiqueta de situação do desenho 11 (LOCADO em azul-marinho, MANUTENCAO em laranja). É uma etiqueta própria
 * desta tela: a `Etiqueta` das outras telas não muda. Reservado não aparece no desenho: usa a família steel.
 */
const ESTILO_SITUACAO: Readonly<Record<StatusVeiculo, { caixa: string; ponto: string }>> = {
  DISPONIVEL: { caixa: 'bg-frota-disponivel-fundo text-frota-disponivel', ponto: 'bg-frota-disponivel-ponto' },
  RESERVADO: { caixa: 'bg-steel-alto text-navy', ponto: 'bg-steel' },
  LOCADO: { caixa: 'bg-steel-suave text-navy', ponto: 'bg-orange-acao' },
  MANUTENCAO: { caixa: 'bg-frota-oficina-fundo text-frota-oficina', ponto: 'bg-frota-oficina' },
  VENDIDO: { caixa: 'bg-status-neutro-fundo text-status-neutro', ponto: 'bg-status-neutro' },
  BAIXADO: { caixa: 'bg-status-neutro-fundo text-status-neutro', ponto: 'bg-status-neutro' },
};

export interface EtiquetaStatusFrotaProps {
  status: StatusVeiculo;
}

/** Situação operacional do veículo: sempre com texto; a cor só reforça. */
export function EtiquetaStatusFrota({ status }: EtiquetaStatusFrotaProps) {
  const estilo = ESTILO_SITUACAO[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-sm px-2.5 py-1 text-rotulo font-bold ${estilo.caixa}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${estilo.ponto}`} aria-hidden="true" />
      {ROTULO_STATUS_VEICULO[status]}
    </span>
  );
}
