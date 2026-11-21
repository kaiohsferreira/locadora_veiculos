import { Icone } from '@/components/ui';
import { DetalhesVeiculoVenda } from '@/features/vendas/componentes/DetalhesVeiculoVenda';
import { FotoCapaVeiculoVenda } from '@/features/vendas/componentes/FotoCapaVeiculoVenda';
import { formatarMoeda } from '@/lib/formatters';
import { PROPOSTA_ROTULO_PRECO, PROPOSTA_VENDIDO_POR } from '@/lib/mensagensProposta';
import type { VeiculoAVenda } from '@/types/vendas';
import { textoCidadeUf } from './textosProposta';

export interface CartaoVeiculoPropostaProps {
  veiculo: VeiculoAVenda;
  /** Chamado quando a imagem da capa falha ao carregar (a tela pode refazer a consulta uma vez). */
  aoFalharFoto?: () => void;
}

/**
 * Cartão do carro da proposta: capa (FotoCapaVeiculoVenda, a mesma do catálogo), nome, ano, km, categoria, cor,
 * locadora e preço.
 */
export function CartaoVeiculoProposta({ veiculo, aoFalharFoto }: CartaoVeiculoPropostaProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  const cidadeUf = textoCidadeUf(veiculo.locadoraCidade, veiculo.locadoraUf);

  return (
    <section aria-label={nome} className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
      <FotoCapaVeiculoVenda veiculo={veiculo} aoFalhar={aoFalharFoto} />

      <div className="flex flex-col gap-4 p-6 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex min-w-0 flex-col gap-2">
          <h2 className="text-secao text-navy">{nome}</h2>
          <DetalhesVeiculoVenda veiculo={veiculo} />
          {veiculo.locadoraNomeFantasia && (
            <p className="flex items-center gap-1 text-corpo-denso text-texto-suave">
              <Icone nome="storefront" tamanho={16} />
              {PROPOSTA_VENDIDO_POR} {veiculo.locadoraNomeFantasia}
              {cidadeUf && ` · ${cidadeUf}`}
            </p>
          )}
        </div>
        <div className="flex flex-col sm:items-end">
          <span className="text-legenda text-texto-suave">{PROPOSTA_ROTULO_PRECO}</span>
          <span className="text-titulo text-navy">{formatarMoeda(veiculo.valorVenda)}</span>
        </div>
      </div>
    </section>
  );
}
