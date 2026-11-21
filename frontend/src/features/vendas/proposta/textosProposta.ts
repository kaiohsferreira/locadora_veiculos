/** Funções puras da tela de proposta: textos do cartão do carro e leitura do id da rota. */

/** Ano de fabricação e do modelo: "2022/2023", ou só um ano quando são iguais. */
export function textoAnos(anoFabricacao: number, anoModelo: number): string {
  return anoFabricacao === anoModelo ? String(anoModelo) : `${anoFabricacao}/${anoModelo}`;
}

/** Cidade e UF da locadora, sem barra solta quando um dos dois falta. */
export function textoCidadeUf(cidade: string, uf: string): string {
  return [cidade, uf].filter((parte) => parte.trim().length > 0).join('/');
}

/** Id do veículo pela rota: só inteiro positivo; qualquer outra coisa vira nulo (a tela mostra "não está à venda"). */
export function lerIdVeiculo(parametro: string | undefined): number | null {
  if (parametro === undefined || !/^\d+$/.test(parametro)) return null;
  const id = Number(parametro);
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
