/**
 * Limites e formatos do cadastro de veículo. Espelham o VeiculoVOValidator e as constantes do servidor
 * (ConstantsValues.Veiculo.cs e StaticMethods.IsPlacaValida). É o único lugar do front onde estes números existem:
 * o servidor valida de novo e é a palavra final.
 */

/** Tamanhos em caracteres (depois de aparar os espaços, quando o campo é texto livre). */
export const LIMITES_VEICULO = {
  /** Placa sem hífen: 7 caracteres nos dois padrões (ABC1234 e ABC1D23). */
  placa: 7,
  /** Chassi: 17 caracteres depois de maiúsculas. */
  chassi: 17,
  /** Renavam: 11 dígitos. */
  renavam: 11,
  /** Marca, em caracteres sem os espaços das pontas. */
  marca: 40,
  /** Modelo, em caracteres sem os espaços das pontas (60 desde a decisão do Kaio de 16/11/2026). */
  modelo: 60,
  /** Cor, em caracteres sem os espaços das pontas. Opcional. */
  cor: 30,
  /** Fornecedor ou concessionária de compra, em caracteres sem os espaços das pontas. Opcional; dado interno. */
  fornecedor: 100,
  /** Número da nota fiscal de compra (texto livre, com pontos, traços ou série). Opcional; dado interno. */
  numeroNotaFiscalCompra: 20,
  /** Dígitos de um ano digitado. */
  digitosAno: 4,
  /** Ano de fabricação mínimo aceito pelo servidor. */
  anoFabricacaoMinimo: 1980,
  /** Quantos anos além do ano atual o veículo pode ter (ano de fabricação e ano do modelo). */
  anosAdiantamento: 1,
  /** Casas decimais dos valores em reais. */
  casasDecimaisValor: 2,
  /** Dígitos inteiros da diária (coluna NUMERIC(10,2)). */
  digitosInteirosValorDiaria: 8,
  /** Dígitos inteiros do valor de venda (coluna NUMERIC(12,2)). */
  digitosInteirosValorVenda: 10,
} as const;

/** Placa depois de normalizada (sem hífen, maiúsculas): três letras, um dígito, uma letra ou dígito e dois dígitos. */
export const PADRAO_PLACA_VEICULO = /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/;

/** Chassi depois de normalizado: 17 caracteres sem I, O e Q. */
export const PADRAO_CHASSI_VEICULO = new RegExp(`^[A-HJ-NPR-Z0-9]{${LIMITES_VEICULO.chassi}}$`);

/** Renavam: exatamente 11 dígitos, como texto (zeros à esquerda contam). */
export const PADRAO_RENAVAM_VEICULO = new RegExp(`^[0-9]{${LIMITES_VEICULO.renavam}}$`);

/** Maior ano aceito para fabricação e modelo, calculado a partir do ano da data de referência. */
export function anoLimiteVeiculo(hoje: Date): number {
  return hoje.getFullYear() + LIMITES_VEICULO.anosAdiantamento;
}
