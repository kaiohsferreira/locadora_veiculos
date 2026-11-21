import { Icone } from '@/components/ui';
import { formatarQuilometragem } from '@/lib/formatters';
import { textoAnos } from '@/features/vendas/proposta/textosProposta';
import type { VeiculoAVenda } from '@/types/vendas';

export interface DetalhesVeiculoVendaProps {
  veiculo: Pick<VeiculoAVenda, 'anoFabricacao' | 'anoModelo' | 'kmAtual' | 'categoriaNome' | 'cor'>;
  /** Mostra a cor (a proposta mostra; o cartão do catálogo, mais compacto, não). Padrão: true. */
  mostrarCor?: boolean;
}

/** Etiqueta pequena com ícone. */
function Detalhe({ icone, texto }: { icone: string; texto: string }) {
  return (
    <li className="inline-flex items-center gap-1 rounded-md bg-steel-claro px-2 py-1 text-legenda font-medium text-navy">
      <Icone nome={icone} tamanho={16} />
      {texto}
    </li>
  );
}

/** Etiquetas de ano, km, categoria e cor de um seminovo, usadas na proposta e no catálogo. */
export function DetalhesVeiculoVenda({ veiculo, mostrarCor = true }: DetalhesVeiculoVendaProps) {
  return (
    <ul className="flex flex-wrap gap-2" aria-label="Características">
      <Detalhe icone="calendar_month" texto={textoAnos(veiculo.anoFabricacao, veiculo.anoModelo)} />
      <Detalhe icone="speed" texto={formatarQuilometragem(veiculo.kmAtual)} />
      {veiculo.categoriaNome && <Detalhe icone="category" texto={veiculo.categoriaNome} />}
      {mostrarCor && veiculo.cor && <Detalhe icone="palette" texto={veiculo.cor} />}
    </ul>
  );
}
