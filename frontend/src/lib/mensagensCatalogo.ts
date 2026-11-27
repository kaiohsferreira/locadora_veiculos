/** Textos fixos da tela do catálogo de seminovos (P6-FE-01, desenho 24). */
export const CATALOGO_TITULO_DOCUMENTO = 'Seminovos';
export const CATALOGO_TITULO_PAGINA = 'Seminovos Exclusivos da Frota AutoVia';
export const CATALOGO_SUBTITULO =
  'Carros da nossa frota, revisados e prontos para você. Simule o financiamento e envie sua proposta.';
export const CATALOGO_ROTULO_ESTOQUE = 'Veículos em estoque';
export const CATALOGO_ROTULO_ITENS = 'seminovos disponíveis';
export const CATALOGO_ORDENADO_POR = 'Ordenado por:';
export const CATALOGO_ORDEM_MENOR_PRECO = 'Menor Preço';
export const CATALOGO_ROTULO_LISTA = 'Seminovos à venda';

export const CATALOGO_TITULO_FILTROS = 'Filtros';
export const CATALOGO_ROTULO_MARCA = 'Marca';
export const CATALOGO_EXEMPLO_MARCA = 'Ex.: Jeep';
export const CATALOGO_ROTULO_FAIXA_PRECO = 'Faixa de preço (R$)';
export const CATALOGO_ROTULO_ANO = 'Ano do modelo';
export const CATALOGO_ROTULO_KM = 'Quilometragem máxima (km)';
export const CATALOGO_ROTULO_DE = 'De';
export const CATALOGO_ROTULO_ATE = 'Até';
export const CATALOGO_EXEMPLO_KM = 'Ex.: 50000';
export const CATALOGO_BOTAO_APLICAR = 'Aplicar filtros';
export const CATALOGO_BOTAO_LIMPAR = 'Limpar filtros';
export const CATALOGO_BOTAO_FILTROS = 'Filtros Avançados';

export const CATALOGO_ERRO_MARCA_TAMANHO = 'A busca por marca deve ter no máximo 40 caracteres.';
export const CATALOGO_ERRO_PRECO_INVALIDO = 'Informe um valor em reais.';
export const CATALOGO_ERRO_FAIXA_PRECO = 'O preço mínimo não pode ser maior que o preço máximo.';
export const CATALOGO_ERRO_ANO_INVALIDO = 'Informe o ano com 4 dígitos.';
export const CATALOGO_ERRO_FAIXA_ANO = 'O ano mínimo não pode ser maior que o ano máximo.';
export const CATALOGO_ERRO_KM_INVALIDO = 'Informe a quilometragem só com números.';

export const CATALOGO_ETIQUETA_MARCA = 'Marca';
export const CATALOGO_ETIQUETA_DE = 'A partir de';
export const CATALOGO_ETIQUETA_ATE = 'Até';
export const CATALOGO_ETIQUETA_ANO_DE = 'Ano a partir de';
export const CATALOGO_ETIQUETA_ANO_ATE = 'Ano até';
export const CATALOGO_ETIQUETA_KM = 'Até';
export const CATALOGO_REMOVER_FILTRO = 'Remover filtro';

export const CATALOGO_BOTAO_CARTAO = 'Simular e fazer proposta';
export const CATALOGO_ROTULO_PRECO = 'Preço à vista';
export const CATALOGO_ROTULO_COR = 'Cor';
export const CATALOGO_VAZIO_FILTRADO_TITULO = 'Nenhum seminovo encontrado com esses filtros';
export const CATALOGO_VAZIO_FILTRADO_TEXTO = 'Tente ampliar a faixa de preço, o ano ou a quilometragem.';
export const CATALOGO_VAZIO_TITULO = 'Nenhum seminovo à venda no momento';
export const CATALOGO_VAZIO_TEXTO = 'Volte em breve: novos carros da frota entram à venda com frequência.';

/** "1 veículo em destaque" ou "N veículos em destaque": os carros da página que está na tela. */
export function textoEmDestaque(quantidade: number): string {
  return quantidade === 1 ? '1 veículo em destaque' : `${quantidade.toLocaleString('pt-BR')} veículos em destaque`;
}

/** Total do catálogo na faixa de destaque ("148"). */
export function textoTotalEstoque(total: number): string {
  return total.toLocaleString('pt-BR');
}
