/**
 * Textos fixos da tela de proposta de compra (P6-FE-02, desenho 31). As mensagens de erro do servidor chegam prontas
 * (ErroApi) e são exibidas por `obterMensagemErro`; aqui ficam só os textos da própria tela.
 */
export const PROPOSTA_TITULO_DOCUMENTO = 'Proposta de compra';
export const PROPOSTA_TITULO_PAGINA = 'Proposta de Compra & Simulação';
export const PROPOSTA_SUBTITULO = 'Ajuste as condições de entrada, simule o parcelamento e envie sua proposta à locadora.';

/** Trilha do topo: Início > Carros à venda > (carro) > Enviar Proposta de Compra. */
export const PROPOSTA_TRILHA_INICIO = 'Início';
export const PROPOSTA_TRILHA_CATALOGO = 'Carros à venda';
export const PROPOSTA_TRILHA_ATUAL = 'Enviar Proposta de Compra';
export const PROPOSTA_TRILHA_ROTULO = 'Trilha de navegação';

/** Cartão do carro (coluna da esquerda). */
export const PROPOSTA_ROTULO_ANO = 'Ano';
export const PROPOSTA_ROTULO_PRECO = 'Valor anunciado à vista';
export const PROPOSTA_TITULO_FICHA = 'Ficha Técnica do Veículo';
export const PROPOSTA_FICHA_ANO_MODELO = 'Ano / Modelo';
export const PROPOSTA_FICHA_KM = 'Odômetro Aferido';
export const PROPOSTA_FICHA_COR = 'Cor Externa';

/** Simulador (coluna da direita). */
export const PROPOSTA_SIMULADOR_SOBRETITULO = 'Simulador Inteligente';
export const PROPOSTA_SIMULADOR_TITULO = 'Personalize suas Condições de Financiamento';
export const PROPOSTA_ROTULO_VALOR_NEGOCIADO = 'Valor Negociado do Veículo';
export const PROPOSTA_VALOR_TRAVADO = 'Valor do anúncio, não muda na simulação';
export const PROPOSTA_ROTULO_ENTRADA = 'Valor de Entrada';
export const PROPOSTA_ROTULO_ATALHOS_ENTRADA = 'Entrada pela porcentagem do valor do veículo';
export const PROPOSTA_ROTULO_BARRA_ENTRADA = 'Ajuste da entrada';
export const PROPOSTA_BARRA_SEM_ENTRADA = '(Sem entrada)';
export const PROPOSTA_BARRA_MAXIMO = '(Máx)';
export const PROPOSTA_ROTULO_PARCELAS = 'Número de Parcelas Desejadas';
export const PROPOSTA_ERRO_ENTRADA_VAZIA = 'Informe o valor da entrada (pode ser R$ 0,00).';
export const PROPOSTA_ERRO_ENTRADA_INVALIDA = 'Informe um valor em reais.';
export const PROPOSTA_ERRO_ENTRADA_MAIOR = 'A entrada deve ser menor que o valor de venda do veículo.';
export const PROPOSTA_ERRO_PARCELAS = 'O número de parcelas deve ficar entre 1 e 60.';

/** Resultado da simulação. */
export const PROPOSTA_RESULTADO_TITULO = 'Resultado da Simulação';
export const PROPOSTA_RESULTADO_SEM_JUROS = 'Parcelamento sem juros, direto com a locadora.';
export const PROPOSTA_RESUMO_ENTRADA = 'Entrada';
export const PROPOSTA_RESUMO_FINANCIADO = 'Saldo Financiado';
export const PROPOSTA_RESUMO_PARCELA = 'Valor Estimado da Parcela';
export const PROPOSTA_RESUMO_ULTIMA_PARCELA = 'Última parcela';
export const PROPOSTA_RESUMO_CALCULANDO = 'Calculando…';
export const PROPOSTA_RESUMO_AGUARDANDO = 'Preencha a entrada e as parcelas corretamente para ver a simulação.';
export const PROPOSTA_RESUMO_NOTA = 'Sem juros. A locadora entra em contato para seguir com a compra.';
export const PROPOSTA_BOTAO_ENVIAR = 'Enviar Proposta de Compra';
export const PROPOSTA_BOTAO_ENVIANDO = 'Enviando…';
export const PROPOSTA_ENTRAR_PARA_ENVIAR = 'Você vai entrar na sua conta e voltar para esta proposta.';
export const PROPOSTA_SO_CLIENTE = 'Só clientes podem enviar propostas. Entre com uma conta de cliente.';

/** Confirmação depois do envio. */
export const PROPOSTA_CONFIRMACAO_TITULO = 'Proposta enviada!';
export const PROPOSTA_CONFIRMACAO_TEXTO = 'vai analisar sua proposta e entrar em contato.';
export const PROPOSTA_CONFIRMACAO_LOCADORA_PADRAO = 'A locadora';
export const PROPOSTA_CONFIRMACAO_CODIGO = 'Código da proposta';
export const PROPOSTA_CONFIRMACAO_VEICULO = 'Veículo';
export const PROPOSTA_CONFIRMACAO_PARCELAS = 'Parcelas';
export const PROPOSTA_CONFIRMACAO_DATA = 'Enviada em';
export const PROPOSTA_CONFIRMACAO_SITUACAO = 'Situação';
export const PROPOSTA_CONFIRMACAO_VER_SEMINOVOS = 'Ver outros seminovos';
export const PROPOSTA_CONFIRMACAO_INICIO = 'Voltar ao início';

export const PROPOSTA_VEICULO_INDISPONIVEL_TITULO = 'Este veículo não está mais à venda';
export const PROPOSTA_VEICULO_INDISPONIVEL_TEXTO = 'Ele pode ter sido vendido ou retirado do catálogo. Veja os outros seminovos disponíveis.';
export const PROPOSTA_VER_CATALOGO = 'Ver catálogo';

/** Rótulos da situação da proposta. */
export const PROPOSTA_ROTULO_STATUS: Readonly<Record<'ENVIADA' | 'ACEITA' | 'RECUSADA' | 'CANCELADA', string>> = {
  ENVIADA: 'Enviada',
  ACEITA: 'Aceita',
  RECUSADA: 'Recusada',
  CANCELADA: 'Cancelada',
};

/** Código da proposta como a pessoa vê: "#11". */
export function textoCodigoProposta(idPropostaCompra: number): string {
  return `#${idPropostaCompra}`;
}

/** Subtítulo de cada cartão de parcelas (12x → "1 ano"; 48x → "4 anos"). */
export function textoPrazoParcelas(numeroParcelas: number): string {
  const anos = numeroParcelas / 12;
  return anos === 1 ? '1 ano' : `${anos} anos`;
}
