import { formatarPlaca } from '@/lib/formatters';
import { MENSAGEM_FROTA_BRASIL } from '@/lib/mensagensFrota';

export interface PlacaRetiradaProps { placa: string; }

/**
 * Placa Mercosul do desenho 16: faixa azul "BRASIL" à esquerda e a placa à direita. Não reaproveita a PlacaMercosul da tela 11,
 * que tem outro formato (faixa em cima).
 */
export function PlacaRetirada({ placa }: PlacaRetiradaProps) {
  return (
    <div className="inline-flex items-center self-start overflow-hidden rounded-sm shadow-sm">
      <div className="flex items-center justify-center bg-retirada-placa-faixa px-1.5 py-0.5">
        <span className="text-emblema font-bold leading-none tracking-widest text-branco">{MENSAGEM_FROTA_BRASIL}</span>
      </div>
      <div className="flex items-center justify-center bg-branco px-2 py-0.5">
        <span className="text-dado-tabular font-black uppercase tracking-widest text-texto-desenho">{formatarPlaca(placa)}</span>
      </div>
    </div>
  );
}
