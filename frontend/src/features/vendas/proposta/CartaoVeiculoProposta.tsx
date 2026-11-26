import { Icone } from '@/components/ui';
import { FotoCapaVeiculoVenda } from '@/features/vendas/componentes/FotoCapaVeiculoVenda';
import { formatarQuilometragem } from '@/lib/formatters';
import {
  PROPOSTA_FICHA_ANO_MODELO,
  PROPOSTA_FICHA_COR,
  PROPOSTA_FICHA_KM,
  PROPOSTA_ROTULO_ANO,
  PROPOSTA_ROTULO_PRECO,
  PROPOSTA_TITULO_FICHA,
} from '@/lib/mensagensProposta';
import type { VeiculoAVenda } from '@/types/vendas';
import { separarCentavos, textoAnos, textoCidadeUf } from './textosProposta';

export interface CartaoVeiculoPropostaProps {
  veiculo: VeiculoAVenda;
  /** Chamado quando a imagem da capa falha ao carregar (a tela pode refazer a consulta uma vez). */
  aoFalharFoto?: () => void;
}

/** Item da ficha técnica: rótulo pequeno e valor. */
function ItemFicha({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div className="flex flex-col rounded-lg bg-offwhite p-2">
      <dt className="text-legenda text-texto-suave">{rotulo}</dt>
      <dd className="text-corpo-denso font-semibold text-texto tabular-nums">{valor}</dd>
    </div>
  );
}

/**
 * Coluna do carro (desenho 31): foto com a km por cima, nome, ano e cor, "Valor anunciado à vista" com o texto do
 * servidor, ficha técnica (ano/modelo, km e cor) e o cartão da locadora com nome e cidade/UF.
 *
 * Fora do desenho por não existirem no sistema: placa, selo "Laudo Dekra", "Único dono", "Veículo Certificado", câmbio,
 * FIPE e economia, motorização, transmissão, combustível, a lista "Incluso nesta Aquisição" e endereço/test-drive do pátio.
 */
export function CartaoVeiculoProposta({ veiculo, aoFalharFoto }: CartaoVeiculoPropostaProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  const cidadeUf = textoCidadeUf(veiculo.locadoraCidade, veiculo.locadoraUf);
  const anos = textoAnos(veiculo.anoFabricacao, veiculo.anoModelo, ' / ');
  const km = formatarQuilometragem(veiculo.kmAtual);
  const preco = separarCentavos(veiculo.valorVendaStr);
  const linhaApoio = [`${PROPOSTA_ROTULO_ANO} ${anos}`, veiculo.cor].filter(Boolean).join(' • ');

  return (
    <section aria-label={nome} className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
      <div className="relative">
        <FotoCapaVeiculoVenda veiculo={veiculo} className="aspect-[16/10] w-full" aoFalhar={aoFalharFoto} />
        <span
          className="absolute bottom-3 right-3 rounded bg-navy px-2 py-0.5 text-legenda font-semibold text-branco tabular-nums"
          data-testid="km-sobre-foto"
        >
          {km}
        </span>
      </div>

      <div className="flex flex-col gap-4 p-6">
        <div>
          <h2 className="text-secao font-bold text-navy">{nome}</h2>
          <p className="text-corpo-denso text-texto-suave">{linhaApoio}</p>
        </div>

        <div className="rounded-lg bg-steel-fundo p-4">
          <span className="block text-legenda uppercase tracking-wider text-texto-suave">{PROPOSTA_ROTULO_PRECO}</span>
          <p className="text-titulo font-bold text-navy tabular-nums" data-testid="preco-anunciado">
            {preco.inteiro}
            <span className="text-secao font-normal text-texto-suave">{preco.centavos}</span>
          </p>
        </div>

        <div>
          <h3 className="mb-2 flex items-center gap-1.5 text-corpo-denso font-bold text-navy">
            <Icone nome="tune" tamanho={18} className="text-orange-escuro" />
            {PROPOSTA_TITULO_FICHA}
          </h3>
          <dl className="grid grid-cols-2 gap-1">
            <ItemFicha rotulo={PROPOSTA_FICHA_ANO_MODELO} valor={anos} />
            <ItemFicha rotulo={PROPOSTA_FICHA_KM} valor={km} />
            {veiculo.cor && <ItemFicha rotulo={PROPOSTA_FICHA_COR} valor={veiculo.cor} />}
          </dl>
        </div>

        {veiculo.locadoraNomeFantasia && (
          <div className="flex items-start gap-2 rounded-cartao bg-offwhite p-4" data-testid="cartao-locadora">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-navy text-branco">
              <Icone nome="pin_drop" tamanho={18} />
            </span>
            <div className="text-corpo-denso">
              <p className="font-bold text-navy">{veiculo.locadoraNomeFantasia}</p>
              {cidadeUf && <p className="text-texto-suave">{cidadeUf}</p>}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
