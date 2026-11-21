import { Icone, ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { formatarMoeda, formatarQuilometragem } from '@/lib/formatters';
import { PROPOSTA_ROTULO_PRECO, PROPOSTA_VENDIDO_POR } from '@/lib/mensagensProposta';
import type { VeiculoAVenda } from '@/types/vendas';
import { textoAnos, textoCidadeUf } from './textosProposta';

export interface CartaoVeiculoPropostaProps {
  veiculo: VeiculoAVenda;
  /** Chamado quando a imagem da capa falha ao carregar (a tela pode refazer a consulta uma vez). */
  aoFalharFoto?: () => void;
}

/** Etiqueta pequena com ícone, para ano, km, categoria e cor. */
function Detalhe({ icone, texto }: { icone: string; texto: string }) {
  return (
    <li className="inline-flex items-center gap-1 rounded-md bg-steel-claro px-2 py-1 text-legenda font-medium text-navy">
      <Icone nome={icone} tamanho={16} />
      {texto}
    </li>
  );
}

/**
 * Cartão do carro da proposta: capa (ícone de carro quando não há foto; aviso com o texto do servidor quando
 * `erroFotoCapa` vem preenchido), nome, ano, km, categoria, cor, locadora e preço.
 */
export function CartaoVeiculoProposta({ veiculo, aoFalharFoto }: CartaoVeiculoPropostaProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  const cidadeUf = textoCidadeUf(veiculo.locadoraCidade, veiculo.locadoraUf);

  return (
    <section aria-label={nome} className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
      <div className="bg-offwhite">
        <ImagemVeiculo
          url={veiculo.fotoCapa ? urlDaImagem(veiculo.fotoCapa) : null}
          alt={nome}
          ajuste="cover"
          className="aspect-[16/9] w-full"
          tamanhoIcone={72}
          aoFalhar={aoFalharFoto}
        />
        {veiculo.erroFotoCapa && (
          <output className="flex items-center gap-2 bg-status-pendente-fundo px-4 py-2 text-corpo-denso text-status-pendente">
            <Icone nome="image_not_supported" tamanho={18} />
            <span>{veiculo.erroFotoCapa}</span>
          </output>
        )}
      </div>

      <div className="flex flex-col gap-4 p-6 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex min-w-0 flex-col gap-2">
          <h2 className="text-secao text-navy">{nome}</h2>
          <ul className="flex flex-wrap gap-2" aria-label="Características">
            <Detalhe icone="calendar_month" texto={textoAnos(veiculo.anoFabricacao, veiculo.anoModelo)} />
            <Detalhe icone="speed" texto={formatarQuilometragem(veiculo.kmAtual)} />
            {veiculo.categoriaNome && <Detalhe icone="category" texto={veiculo.categoriaNome} />}
            {veiculo.cor && <Detalhe icone="palette" texto={veiculo.cor} />}
          </ul>
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
