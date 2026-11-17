import { Campo } from '@/components/ui';
import { normalizarKmDigitado, normalizarTextoDigitado } from '@/lib/normalizacaoVeiculo';
import {
  MENSAGEM_VEICULO_ETIQUETA_AUDITORIA,
  MENSAGEM_VEICULO_ROTULO_DATA_COMPRA,
  MENSAGEM_VEICULO_ROTULO_FORNECEDOR,
  MENSAGEM_VEICULO_ROTULO_NOTA_FISCAL,
  MENSAGEM_VEICULO_ROTULO_ODOMETRO,
  MENSAGEM_VEICULO_SECAO_AQUISICAO,
  MENSAGEM_VEICULO_SUFIXO_KM,
} from '@/lib/mensagensVeiculos';
import { CartaoSecaoVeiculo } from './CartaoSecaoVeiculo';
import type { PropsCamposVeiculo } from './tiposBlocosVeiculo';

/**
 * Seção 3 da tela 05, "Registro de Aquisição e Fornecedor": data da compra (opcional), odômetro de entrega (obrigatório,
 * aceita zero), fornecedor e nota fiscal de compra (opcionais, dados internos da locadora), na grade 3, 3, 3 e 3.
 */
export function CamposAquisicaoVeiculo({ valores, erros, aoMudar }: PropsCamposVeiculo) {
  return (
    <CartaoSecaoVeiculo
      icone="receipt_long"
      titulo={MENSAGEM_VEICULO_SECAO_AQUISICAO}
      etiqueta={MENSAGEM_VEICULO_ETIQUETA_AUDITORIA}
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-12">
        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="dataAquisicao"
            type="date"
            rotulo={MENSAGEM_VEICULO_ROTULO_DATA_COMPRA}
            value={valores.dataAquisicao}
            onChange={(evento) => aoMudar('dataAquisicao', evento.target.value)}
            erro={erros.dataAquisicao}
          />
        </div>

        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="kmAtual"
            type="number"
            rotulo={MENSAGEM_VEICULO_ROTULO_ODOMETRO}
            sufixo={MENSAGEM_VEICULO_SUFIXO_KM}
            tipografia="dado"
            inputMode="numeric"
            obrigatorio
            ocultarAsterisco
            mascara={normalizarKmDigitado}
            value={valores.kmAtual}
            onChange={(evento) => aoMudar('kmAtual', evento.target.value)}
            erro={erros.kmAtual}
          />
        </div>

        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="fornecedor"
            rotulo={MENSAGEM_VEICULO_ROTULO_FORNECEDOR}
            autoComplete="off"
            mascara={normalizarTextoDigitado}
            value={valores.fornecedor}
            onChange={(evento) => aoMudar('fornecedor', evento.target.value)}
            erro={erros.fornecedor}
          />
        </div>

        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="numeroNotaFiscalCompra"
            rotulo={MENSAGEM_VEICULO_ROTULO_NOTA_FISCAL}
            tipografia="dado"
            autoComplete="off"
            mascara={normalizarTextoDigitado}
            value={valores.numeroNotaFiscalCompra}
            onChange={(evento) => aoMudar('numeroNotaFiscalCompra', evento.target.value)}
            erro={erros.numeroNotaFiscalCompra}
          />
        </div>
      </div>
    </CartaoSecaoVeiculo>
  );
}
