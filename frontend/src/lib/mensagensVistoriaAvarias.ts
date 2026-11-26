/**
 * Mensagens da vistoria de entrada com avarias (tela 19). Textos aprovados pelo Kaio em 26/11/2026 (anexo
 * aprovacao-eduardo-telas-26-11.md, "Aprovo como esta"). Reexportadas por `@/lib/mensagens`.
 * Textos de erro do servidor não entram aqui: a tela mostra só o texto do front, nunca o do backend.
 */

// Cabeçalho, trilha e vistoriador --------------------------------------------

/** Trilha: "Etapa 2 • Em Andamento" e "Etapa 1 • Concluído" usam os nomes da tela 16 (MENSAGENS_RETIRADA.passos). */
export const MENSAGEM_VISTORIA_EM_ANDAMENTO = 'Em Andamento';
export const MENSAGEM_VISTORIA_CONCLUIDO = 'Concluído';
export const MENSAGEM_VISTORIA_VISTORIADOR = 'Vistoriador Responsável';
export const MENSAGEM_VISTORIA_LOCACAO_ROTULO = 'Locação';
export const MENSAGEM_VISTORIA_LOCACAO_NUMERO = (idLocacao: number): string => `nº ${idLocacao}`;

// Diagrama -------------------------------------------------------------------

export const MENSAGEM_DIAGRAMA_DICA = 'Clique sobre o diagrama para posicionar um marcador no ponto da avaria.';
export const MENSAGEM_DIAGRAMA_SEM_AVARIAS = 'Sem Avarias';
export const MENSAGEM_DIAGRAMA_REDEFINIR = 'Redefinir Marcadores';
export const MENSAGEM_DIAGRAMA_LEGENDA_ATIVO = 'Ponto de Avaria Ativo';
export const MENSAGEM_DIAGRAMA_LEGENDA_CONFORME = 'Conforme / Sem Danos';
/** Título do mapa e rótulo da legenda (textos do desenho 19). */
export const MENSAGEM_DIAGRAMA_TITULO = 'Mapeamento de Avarias por Ângulos';
export const MENSAGEM_DIAGRAMA_LEGENDA_ROTULO = 'Legenda:';
/** Parágrafo do mapa com a vistoria registrada (aprovado pelo Kaio em 26/11/2026). */
export const MENSAGEM_DIAGRAMA_LEITURA = 'Mapa da vistoria registrada.';

/** Nome acessível do quadrante: "Marcar avaria na Lateral Direita". */
export const mensagemMarcarNoAngulo = (rotuloAngulo: string): string => `Marcar avaria na ${rotuloAngulo}`;
/** Legenda e nome acessível do marcador: "Avaria 2" e "Avaria 2, Amassado, Média". */
export const mensagemRotuloAvaria = (numero: number): string => `Avaria ${numero}`;
export const mensagemNomeAcessivelAvaria = (numero: number, descricao: string, gravidade: string): string =>
  `Avaria ${numero}, ${descricao}, ${gravidade}`;
/** Balão do marcador, linha 1: "Avaria 2 • Frente" (anexo, tela 19, TROCAR). */
export const mensagemBalaoAvariaTitulo = (numero: number, rotuloAngulo: string): string => `Avaria ${numero} • ${rotuloAngulo}`;
/** Balão do marcador, linha 2: "Amassado · Média" (anexo, tela 19, TROCAR). */
export const mensagemBalaoAvariaDetalhe = (descricao: string, gravidade: string): string => `${descricao} · ${gravidade}`;
/** Selo do marcador: "Marcador 2". */
export const mensagemSeloMarcador = (numero: number): string => `Marcador ${numero}`;
/** Régua do medidor de combustível, como no desenho 19 (texto fixo). */
export const REGUA_COMBUSTIVEL_VISTORIA = ['Vazio (0/8)', '1/4 (2/8)', '1/2 (4/8)', '3/4 (6/8)', 'Cheio (8/8)'] as const;

/** Contagem no quadrante: "1 Avaria" (desenho), "Sem Avarias" (desenho) e "{n} Avarias" (aprovado em 26/11/2026). */
export const mensagemContagemQuadrante = (quantidade: number): string => {
  if (quantidade === 0) return MENSAGEM_DIAGRAMA_SEM_AVARIAS;
  return quantidade === 1 ? '1 Avaria' : `${quantidade} Avarias`;
};

// Lista de avarias -----------------------------------------------------------

/** "3 Avarias Mapeadas" (desenho). Um item: "1 Avaria Mapeada" (aprovado em 26/11/2026). */
export const mensagemAvariasMapeadas = (quantidade: number): string =>
  quantidade === 1 ? '1 Avaria Mapeada' : `${quantidade} Avarias Mapeadas`;
export const MENSAGEM_AVARIA_ADICIONAR = 'Adicionar Avaria';
export const MENSAGEM_AVARIA_LISTA_VAZIA = 'Nenhuma avaria mapeada ainda. Clique no diagrama ou use Adicionar Avaria.';
/** Leitura (vistoria registrada) sem avarias (aprovado em 26/11/2026). */
export const MENSAGEM_AVARIA_LISTA_VAZIA_LEITURA = 'Nenhuma avaria registrada nesta vistoria.';
export const MENSAGEM_AVARIA_CARREGANDO = 'Carregando avarias…';
export const MENSAGEM_AVARIA_ERRO_LISTA = 'Não foi possível carregar as avarias.';
export const MENSAGEM_AVARIA_TOTAL = 'Total Estimado de Avarias';
export const MENSAGEM_AVARIA_CUSTO = 'Custo Estimado Reparo:';
/** Título do cartão da avaria: o rótulo do ângulo. Sem ângulo (avaria antiga) (aprovado em 26/11/2026). */
export const MENSAGEM_AVARIA_SEM_ANGULO = 'Sem ângulo';
export const MENSAGEM_AVARIA_AVISO_NAO_REMOVE = 'Avarias já registradas não podem ser removidas.';
/** Foto com erro (erroFoto do servidor): o texto é o do front, aprovado para a tela 19 (mesmo da tela 05). */
export const MENSAGEM_AVARIA_FOTO_COM_ERRO = 'Não foi possível carregar esta foto. Se o problema continuar, remova a foto e envie de novo.';
/** Registro de avaria local sem foto enviada depois de a avaria ser gravada (502 do POST). */
export const MENSAGEM_AVARIA_FOTO_PENDENTE = 'A avaria foi registrada, mas a foto não foi enviada. Envie a foto de novo.';
/** Progresso da vistoria: "Vistoria registrada. 2 de 3 avarias enviadas." */
export const mensagemProgressoAvarias = (enviadas: number, total: number): string =>
  `Vistoria registrada. ${enviadas} de ${total} avarias enviadas.`;
export const MENSAGEM_AVARIA_ENVIO_PARCIAL = 'Algumas avarias não foram enviadas. Tente de novo.';
/** Situação "Enviando…" de uma avaria local durante o envio (aprovado em 26/11/2026). */
export const MENSAGEM_AVARIA_ENVIANDO = 'Enviando…';
/** Ação de enviar a foto de uma avaria gravada sem arquivo (aprovado em 26/11/2026). */
export const MENSAGEM_AVARIA_ENVIAR_FOTO = 'Enviar foto';

/** Quilometragem acima da retirada: "Quilometragem 18 km acima da registrada na retirada (12.000 km)." */
export const mensagemKmAcimaDaRetirada = (diferenca: string, kmSaida: string): string =>
  `Quilometragem ${diferenca} km acima da registrada na retirada (${kmSaida}).`;

// Formulário da avaria -------------------------------------------------------

export const MENSAGEM_FORM_AVARIA_TITULO = 'Adicionar Avaria';
export const MENSAGEM_FORM_AVARIA_ENVIAR = 'Adicionar avaria';
export const MENSAGEM_FORM_AVARIA_CANCELAR = 'Cancelar';
export const MENSAGEM_FORM_AVARIA_ANGULO = 'Ângulo';
export const MENSAGEM_FORM_AVARIA_SEM_ANGULO = 'Sem ângulo';
export const MENSAGEM_FORM_AVARIA_DESCRICAO = 'Descrição';
export const MENSAGEM_FORM_AVARIA_GRAVIDADE = 'Gravidade';
export const MENSAGEM_FORM_AVARIA_VALOR = 'Custo Estimado Reparo (R$)';
export const MENSAGEM_FORM_AVARIA_FOTO = 'Foto da avaria';
export const MENSAGEM_FORM_AVARIA_DICA_POSICAO = 'Clique no diagrama para marcar o ponto exato.';
export const MENSAGEM_FORM_AVARIA_POSICAO_X = 'Posição X (%)';
export const MENSAGEM_FORM_AVARIA_POSICAO_Y = 'Posição Y (%)';
export const MENSAGEM_FORM_AVARIA_ESCOLHA_FOTO = 'Escolha a foto da avaria.';
export const MENSAGEM_FORM_AVARIA_ESCOLHA_GRAVIDADE = 'Escolha a gravidade da avaria.';
/** Posição: textos aprovados em 26/11/2026 (anexo de contratos N8, "Aprovo como esta"), iguais aos do servidor. */
export const MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA = 'Informe o ângulo e as posições X e Y da avaria, ou deixe os três em branco.';
export const MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA = 'A posição da avaria deve ser um número de 0 a 100, com até 2 casas decimais e ponto como separador.';

// Vistoria registrada e envio ------------------------------------------------

export const MENSAGEM_VISTORIA_REGISTRAR = 'Registrar vistoria de entrada';
export const MENSAGEM_VISTORIA_AVANCAR = 'Avançar para Emissão de Contrato';
export const MENSAGEM_VISTORIA_VOLTAR = 'Voltar para Conferência';
export const MENSAGEM_VISTORIA_REGISTRO_FOTOS_TITULO = 'Registro Fotográfico da Inspeção';
export const MENSAGEM_VISTORIA_REGISTRO_FOTOS_APOIO = 'Cada avaria registrada leva uma foto.';
export const MENSAGEM_VISTORIA_ODOMETRO_TITULO = 'Odômetro e Combustível';
export const MENSAGEM_VISTORIA_ODOMETRO_ROTULO = 'Quilometragem Atual (Odômetro)';
export const MENSAGEM_VISTORIA_COMBUSTIVEL_ROTULO = 'Nível de Combustível (Tanque)';
export const MENSAGEM_VISTORIA_COMBUSTIVEL_CAMPO = 'Nível de combustível (0 a 1)';
export const MENSAGEM_VISTORIA_STATUS_ATIVA = 'Inspeção Ativa';
export const MENSAGEM_VISTORIA_TITULO = 'Vistoria e Inspeção do Veículo';
export const MENSAGEM_VISTORIA_MIGALHA = 'Vistoria';
export const MENSAGEM_VISTORIA_PLACA = 'Placa:';
/** Link de volta à lista de locações (aprovado pelo Kaio em 26/11/2026). */
export const MENSAGEM_VISTORIA_VOLTAR_LOCACOES = 'Voltar às locações';
export const MENSAGEM_VISTORIA_TRILHA_ROTULO = 'Etapas da locação';
export const MENSAGEM_VISTORIA_DADOS_LEGENDA = 'Dados da vistoria de entrada';
export const MENSAGEM_VISTORIA_OBSERVACOES_ROTULO = 'Observações (opcional)';
export const MENSAGEM_VISTORIA_STATUS_REGISTRADA = 'Entrada registrada';
export const MENSAGEM_VISTORIA_REGISTRADA_EM = 'Registrada em';
export const MENSAGEM_VISTORIA_FUNCIONARIO = 'Funcionário responsável';
export const MENSAGEM_VISTORIA_KM_REGISTRADO = 'Quilometragem registrada';
export const MENSAGEM_VISTORIA_COMBUSTIVEL_LEITURA = 'Nível de combustível (0 a 1)';
export const MENSAGEM_VISTORIA_OBSERVACOES = 'Observações';
