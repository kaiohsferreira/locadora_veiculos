import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import { DetalhesVeiculoVenda } from '@/features/vendas/componentes/DetalhesVeiculoVenda';
import { FotoCapaVeiculoVenda } from '@/features/vendas/componentes/FotoCapaVeiculoVenda';
import { textoCidadeUf } from '@/features/vendas/proposta/textosProposta';
import { formatarMoeda } from '@/lib/formatters';
import { CATALOGO_BOTAO_CARTAO } from '@/lib/mensagensCatalogo';
import type { VeiculoAVenda } from '@/types/vendas';
import { caminhoProposta } from './enderecoCatalogo';

export interface CartaoSeminovoProps {
  veiculo: VeiculoAVenda;
  /** Chamado quando a imagem da capa falha ao carregar (a tela refaz a consulta uma vez). */
  aoFalharFoto?: () => void;
}

/**
 * Cartão de um seminovo no catálogo: capa (a mesma da proposta, com o tratamento de fotoCapa/erroFotoCapa), nome,
 * ano, km, categoria, cidade da locadora e preço. O cartão inteiro é um link para a proposta de compra; o "botão" de
 * baixo é só a aparência do link (um único destino por cartão, para teclado e leitor de tela).
 */
export function CartaoSeminovo({ veiculo, aoFalharFoto }: CartaoSeminovoProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  const cidadeUf = textoCidadeUf(veiculo.locadoraCidade, veiculo.locadoraUf);

  return (
    <article className="h-full">
      <Link
        to={caminhoProposta(veiculo.idVeiculo)}
        aria-label={`${nome}, ${formatarMoeda(veiculo.valorVenda)}. ${CATALOGO_BOTAO_CARTAO}`}
        className="group flex h-full flex-col overflow-hidden rounded-cartao bg-branco shadow-cartao transition-shadow hover:shadow-elevado focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
      >
        <FotoCapaVeiculoVenda veiculo={veiculo} className="aspect-[16/10] w-full" tamanhoIcone={56} aoFalhar={aoFalharFoto} />
        <div className="flex flex-1 flex-col gap-3 p-4">
          <h2 className="text-corpo font-semibold text-navy">{nome}</h2>
          <DetalhesVeiculoVenda veiculo={veiculo} mostrarCor={false} />
          {cidadeUf && (
            <p className="flex items-center gap-1 text-legenda text-texto-suave">
              <Icone nome="location_on" tamanho={14} />
              {cidadeUf}
            </p>
          )}
          <p className="mt-auto pt-1 text-secao text-navy tabular-nums">{formatarMoeda(veiculo.valorVenda)}</p>
          <span className="inline-flex items-center justify-center gap-2 rounded-md bg-orange px-4 py-2 text-corpo-denso font-semibold text-branco group-hover:bg-orange-escuro">
            {CATALOGO_BOTAO_CARTAO}
            <Icone nome="arrow_forward" tamanho={18} />
          </span>
        </div>
      </Link>
    </article>
  );
}
