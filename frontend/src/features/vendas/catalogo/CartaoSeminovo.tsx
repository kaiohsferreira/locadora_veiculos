import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import { FotoCapaVeiculoVenda } from '@/features/vendas/componentes/FotoCapaVeiculoVenda';
import { textoAnos } from '@/features/vendas/proposta/textosProposta';
import { formatarQuilometragem } from '@/lib/formatters';
import { CATALOGO_BOTAO_CARTAO, CATALOGO_ROTULO_COR, CATALOGO_ROTULO_PRECO } from '@/lib/mensagensCatalogo';
import type { VeiculoAVenda } from '@/types/vendas';
import { caminhoProposta } from './enderecoCatalogo';

export interface CartaoSeminovoProps {
  veiculo: VeiculoAVenda;
  /** Chamado quando a imagem da capa falha ao carregar (a tela refaz a consulta uma vez). */
  aoFalharFoto?: () => void;
}

/**
 * Cartão de um seminovo no catálogo (desenho 24): capa, marca em caixa alta acima do modelo, ano sempre como
 * fabricação/modelo, km, cor, "Preço à vista" sobre o preço (texto do servidor) e o botão laranja. O cartão inteiro é um
 * link para a proposta de compra; o "botão" de baixo é só a aparência do link (um único destino por cartão, para teclado
 * e leitor de tela).
 *
 * Fora do desenho por não existirem no sistema: placa mascarada, selos da foto ("Único Dono", "Laudo 100% Dekra"),
 * câmbio, combustível, opcionais, parcelas no cartão e os botões "Ver Detalhes" e "Test-Drive".
 */
export function CartaoSeminovo({ veiculo, aoFalharFoto }: CartaoSeminovoProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;

  return (
    <article className="h-full">
      <Link
        to={caminhoProposta(veiculo.idVeiculo)}
        aria-label={`${nome}, ${veiculo.valorVendaStr}. ${CATALOGO_BOTAO_CARTAO}`}
        className="group flex h-full flex-col overflow-hidden rounded-cartao bg-branco shadow-cartao transition-shadow hover:shadow-elevado focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
      >
        <FotoCapaVeiculoVenda veiculo={veiculo} className="h-56 w-full" tamanhoIcone={56} aoFalhar={aoFalharFoto} />
        <div className="flex flex-1 flex-col justify-between p-6">
          <div>
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0">
                <span className="block text-legenda font-semibold uppercase text-contorno" data-testid="marca-cartao">
                  {veiculo.marca}
                </span>
                <h2 className="text-secao font-bold tracking-tight text-texto">{veiculo.modelo}</h2>
              </div>
              <span className="shrink-0 rounded bg-steel-suave px-2 py-1 text-corpo-denso font-bold text-texto-suave tabular-nums">
                {textoAnos(veiculo.anoFabricacao, veiculo.anoModelo)}
              </span>
            </div>

            <div className="grid grid-cols-3 gap-2 py-4 text-center">
              <div className="rounded bg-steel-fundo p-2">
                <Icone nome="speed" tamanho={18} className="mx-auto block text-contorno" />
                <span className="mt-0.5 block text-corpo-denso font-semibold text-texto tabular-nums">
                  {formatarQuilometragem(veiculo.kmAtual)}
                </span>
              </div>
            </div>

            {veiculo.cor && (
              <p className="flex items-center gap-1 text-corpo-denso text-contorno">
                <span className="inline-block h-2.5 w-2.5 rounded-full bg-contorno-suave" aria-hidden="true" />
                {CATALOGO_ROTULO_COR} {veiculo.cor}
              </p>
            )}
          </div>

          <div className="mt-4 pt-4">
            <span className="block text-legenda uppercase text-contorno">{CATALOGO_ROTULO_PRECO}</span>
            <p className="text-titulo font-bold text-navy tabular-nums">{veiculo.valorVendaStr}</p>
            <span className="mt-4 flex h-10 items-center justify-center gap-1 rounded-lg bg-orange px-3 text-corpo-denso font-semibold text-branco group-hover:bg-orange-escuro">
              {CATALOGO_BOTAO_CARTAO}
              <Icone nome="arrow_forward" tamanho={18} />
            </span>
          </div>
        </div>
      </Link>
    </article>
  );
}
