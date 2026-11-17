/**
 * Categoria de veículo (contrato categoria.contract.json v3, ReturnCategoriaVO). A categoria é global, sem empresa:
 * a lista é pública, e a tela de veículo só usa o id e o nome dela.
 */
export interface CategoriaVeiculo {
  idCategoria: number;
  nome: string;
  descricao: string | null;
  /** Diária base em reais, como número. */
  valorDiariaBase: number;
  /** Diária base formatada pelo servidor (ex.: "R$ 150,00"). */
  valorDiariaBaseStr: string;
}
