/**
 * Textos fixos da tela de proposta de compra (P6-FE-02). As mensagens de erro do servidor chegam prontas (ErroApi) e
 * são exibidas por `obterMensagemErro`; aqui ficam só os textos da própria tela.
 */
export const PROPOSTA_TITULO_PAGINA = 'Proposta de compra';
export const PROPOSTA_SUBTITULO = 'Simule a entrada e as parcelas e envie sua proposta à locadora.';
export const PROPOSTA_VOLTAR_CATALOGO = 'Voltar ao catálogo de seminovos';

export const PROPOSTA_VENDIDO_POR = 'Vendido por';
export const PROPOSTA_ROTULO_PRECO = 'Valor à vista';

export const PROPOSTA_TITULO_SIMULACAO = 'Simule seu financiamento';
export const PROPOSTA_ROTULO_ENTRADA = 'Entrada (R$)';
export const PROPOSTA_DICA_ENTRADA = 'Menor que o valor do veículo.';
export const PROPOSTA_ROTULO_PARCELAS = 'Parcelas';
export const PROPOSTA_DICA_PARCELAS = 'De 1 a 60 parcelas.';
export const PROPOSTA_ERRO_ENTRADA_VAZIA = 'Informe o valor da entrada (pode ser R$ 0,00).';
export const PROPOSTA_ERRO_ENTRADA_INVALIDA = 'Informe um valor em reais.';
export const PROPOSTA_ERRO_ENTRADA_MAIOR = 'A entrada deve ser menor que o valor de venda do veículo.';
export const PROPOSTA_ERRO_PARCELAS = 'O número de parcelas deve ficar entre 1 e 60.';

export const PROPOSTA_TITULO_RESUMO = 'Resumo da simulação';
export const PROPOSTA_RESUMO_VALOR_VEICULO = 'Valor do veículo';
export const PROPOSTA_RESUMO_ENTRADA = 'Entrada';
export const PROPOSTA_RESUMO_FINANCIADO = 'Valor financiado';
export const PROPOSTA_RESUMO_ULTIMA_PARCELA = 'Última parcela';
export const PROPOSTA_RESUMO_CALCULANDO = 'Calculando…';
export const PROPOSTA_RESUMO_AGUARDANDO = 'Preencha a entrada e as parcelas corretamente para ver a simulação.';
export const PROPOSTA_RESUMO_NOTA = 'Sem juros. A locadora entra em contato para seguir com a compra.';
export const PROPOSTA_BOTAO_ENVIAR = 'Enviar proposta';
export const PROPOSTA_BOTAO_ENVIANDO = 'Enviando…';
export const PROPOSTA_ENTRAR_PARA_ENVIAR = 'Você vai entrar na sua conta e voltar para esta proposta.';
export const PROPOSTA_SO_CLIENTE = 'Só clientes podem enviar propostas. Entre com uma conta de cliente.';

export const PROPOSTA_CONFIRMACAO_TITULO = 'Proposta enviada!';
export const PROPOSTA_CONFIRMACAO_TEXTO = 'vai analisar sua proposta e entrar em contato.';
export const PROPOSTA_CONFIRMACAO_LOCADORA_PADRAO = 'A locadora';
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
