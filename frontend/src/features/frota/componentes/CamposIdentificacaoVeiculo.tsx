import { Campo, Icone, Select } from '@/components/ui';
import {
  normalizarAnoDigitado,
  normalizarChassiDigitado,
  normalizarPlacaDigitada,
  normalizarRenavamDigitado,
  normalizarTextoDigitado,
} from '@/lib/normalizacaoVeiculo';
import { opcoesMarcaVeiculo } from '@/lib/listasVeiculo';
import {
  MENSAGEM_VEICULO_APOIO_PLACA,
  MENSAGEM_VEICULO_EMBLEMA_PLACA,
  MENSAGEM_VEICULO_ETIQUETA_OBRIGATORIOS,
  MENSAGEM_VEICULO_PLACEHOLDER_PLACA,
  MENSAGEM_VEICULO_ROTULO_ANO_FABRICACAO,
  MENSAGEM_VEICULO_ROTULO_ANO_MODELO,
  MENSAGEM_VEICULO_ROTULO_CHASSI,
  MENSAGEM_VEICULO_ROTULO_MARCA,
  MENSAGEM_VEICULO_ROTULO_MODELO,
  MENSAGEM_VEICULO_ROTULO_PLACA,
  MENSAGEM_VEICULO_ROTULO_RENAVAM,
  MENSAGEM_VEICULO_SECAO_IDENTIFICACAO,
} from '@/lib/mensagensVeiculos';
import { CartaoSecaoVeiculo } from './CartaoSecaoVeiculo';
import { SeletorCorVeiculo } from './SeletorCorVeiculo';
import type { PropsCamposVeiculo } from './tiposBlocosVeiculo';

/** Emblema "BR" da placa (Mercosul), à esquerda dentro do campo. Decorativo: o rótulo já diz o padrão. */
function EmblemaBrasil() {
  return (
    <span
      aria-hidden="true"
      className="-ml-1 flex items-center rounded-sm bg-navy px-1.5 py-0.5 text-emblema font-bold uppercase tracking-widest text-branco select-none"
    >
      {MENSAGEM_VEICULO_EMBLEMA_PLACA}
    </span>
  );
}

/**
 * Seção 1 da tela 05, "Identificação Técnica do Veículo": placa, chassi, renavam, marca, modelo, anos e cor, na grade
 * de 12 colunas do desenho (4, 4, 4, 4, 8, 3, 3 e 6). Os textos de apoio que o sistema não cumpre saíram (decisão 5).
 */
export function CamposIdentificacaoVeiculo({ valores, erros, aoMudar }: PropsCamposVeiculo) {
  return (
    <CartaoSecaoVeiculo
      icone="badge"
      titulo={MENSAGEM_VEICULO_SECAO_IDENTIFICACAO}
      etiqueta={MENSAGEM_VEICULO_ETIQUETA_OBRIGATORIOS}
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-12">
        <div className="md:col-span-4">
          <Campo
            variante="desenho"
            name="placa"
            rotulo={MENSAGEM_VEICULO_ROTULO_PLACA}
            apoioRotulo={MENSAGEM_VEICULO_APOIO_PLACA}
            obrigatorio
            ocultarAsterisco
            prefixo={<EmblemaBrasil />}
            prefixoLargo
            tipografia="placa"
            placeholder={MENSAGEM_VEICULO_PLACEHOLDER_PLACA}
            maxLength={7}
            autoComplete="off"
            mascara={normalizarPlacaDigitada}
            value={valores.placa}
            onChange={(evento) => aoMudar('placa', evento.target.value)}
            erro={erros.placa}
          />
        </div>

        <div className="md:col-span-4">
          <Campo
            variante="desenho"
            name="chassi"
            rotulo={MENSAGEM_VEICULO_ROTULO_CHASSI}
            obrigatorio
            ocultarAsterisco
            tipografia="dado"
            className="uppercase tracking-wider"
            sufixo={<Icone nome="verified" tamanho={18} className="text-contorno" />}
            sufixoSemRecuo
            maxLength={17}
            autoComplete="off"
            mascara={normalizarChassiDigitado}
            value={valores.chassi}
            onChange={(evento) => aoMudar('chassi', evento.target.value)}
            erro={erros.chassi}
          />
        </div>

        <div className="md:col-span-4">
          <Campo
            variante="desenho"
            name="renavam"
            rotulo={MENSAGEM_VEICULO_ROTULO_RENAVAM}
            obrigatorio
            ocultarAsterisco
            tipografia="dado"
            maxLength={11}
            inputMode="numeric"
            autoComplete="off"
            mascara={normalizarRenavamDigitado}
            value={valores.renavam}
            onChange={(evento) => aoMudar('renavam', evento.target.value)}
            erro={erros.renavam}
          />
        </div>

        <div className="md:col-span-4">
          <Select
            variante="desenho"
            name="marca"
            rotulo={MENSAGEM_VEICULO_ROTULO_MARCA}
            opcoes={opcoesMarcaVeiculo(valores.marca)}
            obrigatorio
            ocultarAsterisco
            value={valores.marca}
            onChange={(evento) => aoMudar('marca', evento.target.value)}
            erro={erros.marca}
          />
        </div>

        <div className="md:col-span-8">
          <Campo
            variante="desenho"
            name="modelo"
            rotulo={MENSAGEM_VEICULO_ROTULO_MODELO}
            obrigatorio
            ocultarAsterisco
            autoComplete="off"
            mascara={normalizarTextoDigitado}
            value={valores.modelo}
            onChange={(evento) => aoMudar('modelo', evento.target.value)}
            erro={erros.modelo}
          />
        </div>

        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="anoFabricacao"
            type="number"
            rotulo={MENSAGEM_VEICULO_ROTULO_ANO_FABRICACAO}
            obrigatorio
            ocultarAsterisco
            tipografia="dado"
            inputMode="numeric"
            mascara={normalizarAnoDigitado}
            value={valores.anoFabricacao}
            onChange={(evento) => aoMudar('anoFabricacao', evento.target.value)}
            erro={erros.anoFabricacao}
          />
        </div>

        <div className="md:col-span-3">
          <Campo
            variante="desenho"
            name="anoModelo"
            type="number"
            rotulo={MENSAGEM_VEICULO_ROTULO_ANO_MODELO}
            obrigatorio
            ocultarAsterisco
            tipografia="dado"
            inputMode="numeric"
            mascara={normalizarAnoDigitado}
            value={valores.anoModelo}
            onChange={(evento) => aoMudar('anoModelo', evento.target.value)}
            erro={erros.anoModelo}
          />
        </div>

        <div className="md:col-span-6">
          <SeletorCorVeiculo
            valor={valores.cor}
            erro={erros.cor}
            aoMudar={(cor) => aoMudar('cor', cor)}
          />
        </div>
      </div>
    </CartaoSecaoVeiculo>
  );
}
