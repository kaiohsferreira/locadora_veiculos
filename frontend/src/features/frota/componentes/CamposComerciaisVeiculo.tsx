import type { ReactNode } from 'react';
import { Campo, Select, type OpcaoSelect } from '@/components/ui';
import { mascararMoedaDigitada } from '@/lib/formatters';
import {
  MENSAGEM_VEICULO_ETIQUETA_RECEITA,
  MENSAGEM_VEICULO_PREFIXO_REAIS,
  MENSAGEM_VEICULO_ROTULO_CATEGORIA,
  MENSAGEM_VEICULO_ROTULO_DIARIA,
  MENSAGEM_VEICULO_ROTULO_VALOR_SEMINOVO,
  MENSAGEM_VEICULO_SECAO_COMERCIAL,
} from '@/lib/mensagensVeiculos';
import { CartaoSecaoVeiculo } from './CartaoSecaoVeiculo';
import { SeletorStatusInicialVeiculo } from './SeletorStatusInicialVeiculo';
import type { PropsCamposVeiculo } from './tiposBlocosVeiculo';

export interface CamposComerciaisVeiculoProps extends PropsCamposVeiculo {
  /**
   * Categorias do seletor (a tela monta com a lista da API, o nome e a descrição). Quem chama inclui a categoria
   * gravada do veículo, mesmo desativada, para a edição não perder o valor.
   */
  opcoesCategoria: readonly OpcaoSelect[];
  /** Aviso abaixo do seletor de categoria (carregando, erro com "Tentar de novo" ou lista vazia). Opcional. */
  apoioCategoria?: ReactNode;
  /** Trava o seletor de categoria enquanto a lista não está pronta. */
  categoriaIndisponivel?: boolean;
}

/**
 * Seção 2 da tela 05, "Parâmetros Comerciais e Tarifação": categoria, status inicial travado, diária e valor de
 * referência de seminovo, na grade do desenho (6, 6, 6 e 6). Sem "Tarifa padrão" nem "Tabela FIPE" (decisão 5).
 */
export function CamposComerciaisVeiculo({
  valores,
  erros,
  aoMudar,
  opcoesCategoria,
  apoioCategoria,
  categoriaIndisponivel = false,
}: CamposComerciaisVeiculoProps) {
  return (
    <CartaoSecaoVeiculo
      icone="currency_exchange"
      titulo={MENSAGEM_VEICULO_SECAO_COMERCIAL}
      etiqueta={MENSAGEM_VEICULO_ETIQUETA_RECEITA}
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-12">
        <div className="flex flex-col gap-2 md:col-span-6">
          <Select
            variante="desenho"
            name="idCategoria"
            rotulo={MENSAGEM_VEICULO_ROTULO_CATEGORIA}
            opcoes={opcoesCategoria}
            obrigatorio
            ocultarAsterisco
            disabled={categoriaIndisponivel}
            value={valores.idCategoria}
            onChange={(evento) => aoMudar('idCategoria', evento.target.value)}
            erro={erros.idCategoria}
          />
          {apoioCategoria}
        </div>

        <div className="md:col-span-6">
          <SeletorStatusInicialVeiculo />
        </div>

        <div className="md:col-span-6">
          <Campo
            variante="desenho"
            name="valorDiaria"
            rotulo={MENSAGEM_VEICULO_ROTULO_DIARIA}
            prefixo={MENSAGEM_VEICULO_PREFIXO_REAIS}
            tipografia="valor"
            inputMode="decimal"
            obrigatorio
            ocultarAsterisco
            autoComplete="off"
            mascara={mascararMoedaDigitada}
            value={valores.valorDiaria}
            onChange={(evento) => aoMudar('valorDiaria', evento.target.value)}
            erro={erros.valorDiaria}
          />
        </div>

        <div className="md:col-span-6">
          <Campo
            variante="desenho"
            name="valorVenda"
            rotulo={MENSAGEM_VEICULO_ROTULO_VALOR_SEMINOVO}
            prefixo={MENSAGEM_VEICULO_PREFIXO_REAIS}
            tipografia="valor"
            inputMode="decimal"
            autoComplete="off"
            mascara={mascararMoedaDigitada}
            value={valores.valorVenda}
            onChange={(evento) => aoMudar('valorVenda', evento.target.value)}
            erro={erros.valorVenda}
          />
        </div>
      </div>
    </CartaoSecaoVeiculo>
  );
}
