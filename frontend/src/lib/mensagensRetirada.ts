/**
 * Textos da tela 16 (retirada do veículo). Os que vêm do desenho D:/Estudo/TI/telas/16 estão marcados como "desenho";
 * os novos e os ajustados estão marcados como "novo" ou "ajustado" (locacao.contract.json, tela_16.textos), e foram
 * aprovados pelo Kaio em 24/11/2026 ("Aprovo como está"). Os da caução (marcados "caução") só aparecem com a caução
 * exigida (locacao.contract.json v4, caucao_na_retirada_v4.tela_16_v4.textos) e também foram aprovados pelo Kaio em
 * 24/11/2026 ("Aprovo como está"), inclusive os dos ajustes A1 e A4 e o complemento encurtado do 409.
 */
export const MENSAGENS_RETIRADA = {
  titulo: 'Retirada do veículo',
  /** novo: subtítulo SEM caução (e nos estados sem dados), sem "garantias". */
  subtitulo: 'Conferência inicial de documentação e validação de reserva para entrega das chaves.',
  /** caução, desenho (html :19-21): subtítulo com a caução exigida, com "garantias". */
  subtituloComCaucao: 'Conferência inicial de documentação, garantias e validação de reserva para entrega das chaves.',
  /** desenho: trilha. "Locações" leva à página de entrada; "Despacho" não tem página. */
  trilhaRotulo: 'Caminho da tela',
  trilhaLocacoes: 'Locações',
  trilhaDespacho: 'Despacho',
  atualizarDados: 'Atualizar dados',
  /** ajustado: "volte às retiradas" (antes "volte às reservas"). */
  idInvalido: 'A reserva informada é inválida. Volte às retiradas e informe o código de novo.',
  /** novo, aprovado pelo Kaio em 24/11/2026 (acerto C): botão dos estados de erro da tela 16, leva à página de entrada. */
  voltarRetiradas: 'Voltar às retiradas',
  /** desenho: os quatro passos. Só o primeiro pode ficar ativo. */
  passos: ['Conferência', 'Vistoria', 'Contrato', 'Entrega'] as const,
  etapa: (numero: number) => `Etapa ${numero}`,
  emAndamento: 'Em andamento',
  codigoReserva: 'Código da Reserva',
  /** novo: prefixo do selo da situação; o resto vem do rótulo real do status (Ativa, Confirmada...). */
  situacaoReserva: 'Reserva',
  veiculoAtribuido: 'Veículo Atribuído',
  grupo: (categoria: string) => `Grupo ${categoria}`,
  quilometragemAtual: 'Quilometragem atual',
  dadosCondutor: 'Dados do Condutor',
  cpf: 'CPF:',
  cnh: 'CNH:',
  categoria: 'Categoria:',
  validade: 'Validade:',
  cadastroNaLocadora: 'Cadastro nesta locadora',
  semCadastro: 'Sem cadastro ativo nesta locadora',
  periodo: 'Período & Itinerário',
  diariaSingular: '1 diária completa contratada',
  diarias: (quantidade: number) => `${quantidade} diárias completas contratadas`,
  tarifacao: 'Tarifação da Reserva',
  diaria: 'Diária:',
  totalPrevisto: 'Total previsto',
  /** caução, desenho (html :178): rótulo da caixa da caução, dentro da Tarifação. */
  caucaoRotulo: 'Garantia / Caução',
  /** caução, novo, aprovado: situação da caução antes do clique (no lugar de "Pré-autorizado no Mastercard final 4018"). */
  caucaoSituacaoAoIniciar: 'Pré-autorização no cartão do cliente ao iniciar a locação.',
  /**
   * caução, novo, aprovado: a caução é exigida, mas a tela não pode tentar (provedor sem simulação liberada). Texto inteiro,
   * só no item da pré-autorização (ajuste A4).
   */
  caucaoIndisponivel: 'Pré-autorização indisponível: o provedor de pagamento não está liberado para a caução. A locação não pode ser iniciada.',
  /** caução, novo, aprovado (ajuste A4): o mesmo estado, curto, só na caixa "Garantia / Caução" da coluna da esquerda. */
  caucaoIndisponivelCurto: 'Pré-autorização indisponível.',
  checklist: 'Checklist de Verificação Documental e Presencial',
  /** desenho (html :210): o total é 3 sem caução e 4 com a caução exigida. */
  contadorConferidos: (conferidos: number, total: number) => `${conferidos}/${total} Confirmados`,
  instrucao: 'Instrução ao operador: Certifique-se de validar todos os itens presencialmente antes de liberar o termo para assinatura do cliente.',
  legendaConferencia: 'Documentos apresentados pelo cliente',
  /** novo: aviso de CNH vencida. Só informa; não bloqueia nada. */
  cnhVencida: (validade: string) => `CNH vencida: a validade terminou em ${validade}. O sistema não bloqueia a retirada; confira o documento com o cliente.`,
  itens: {
    cnhOriginal: {
      titulo: 'CNH original física ou digital compatível',
      /** ajustado: sem a frase "Categoria B regularizada.", que o sistema não sabe. */
      descricao: 'Conferência visual via app CNH Digital ou documento impresso oficial válido.',
      /** novo: linha com os dados reais da CNH. */
      dados: (categoria: string, validade: string) => `Categoria ${categoria}, validade ${validade}.`,
      /** novo: linha do envio pelo site (com data e hora, ou sem envio). */
      envioComData: (data: string) => `Enviada pelo cliente em ${data}.`,
      semEnvio: 'O cliente não enviou a CNH pelo site.',
    },
    /** caução, desenho (html :247-250): quarto atestado, só com a caução exigida. */
    titularidadeCartao: {
      titulo: 'Titularidade do cartão de crédito',
      descricao: 'Titular do cartão corresponde ao condutor principal ou responsável financeiro com procuração reconhecida.',
    },
    comprovanteResidencia: {
      titulo: 'Comprovante de residência atualizado',
      descricao: 'Emitido nos últimos 90 dias em nome do locatário (contas de luz, água ou fatura bancária).',
      envioComData: (data: string) => `Enviado pelo cliente em ${data}.`,
      semEnvio: 'O cliente não enviou o comprovante pelo site.',
    },
    termoMultas: {
      titulo: 'Termo de responsabilidade sobre multas',
      descricao: 'Ciência do repasse automático e indicação do condutor para eventuais infrações de trânsito em período de locação.',
    },
  },
  /** novo: linha verde do item marcado, com o nome de quem conferiu nesta tela (nada é gravado). */
  conferidoPor: (nome: string) => `Conferido nesta tela por ${nome}`,
  /** novo: variante sem nome, para quando o nome do funcionário não está disponível. */
  conferidoPeloAtendente: 'Conferido nesta tela pelo atendente',
  /** caução, desenho (html :352, com o valor do servidor): último item da lista, sem interruptor. */
  preAutorizacaoTitulo: (valorCaucaoStr: string) => `Pré-autorização da caução (${valorCaucaoStr})`,
  /** caução, ajustado do desenho (html :354, sem "AutoVia Pay"), aprovado. */
  preAutorizacaoDescricao: 'Bloqueio de garantia financeira no cartão de crédito do cliente, feito quando a locação é iniciada.',
  /** caução, novo, aprovado: legenda do painel de simulação do cartão (grupo de rádio). */
  simulacaoTitulo: 'Simulação do cartão',
  /** caução, novo, aprovado: aviso do painel de simulação. */
  simulacaoAviso: 'Ambiente de simulação: nenhum valor é reservado em cartão de verdade. Escolha o resultado da pré-autorização.',
  /** caução, novo, aprovado: as três saídas da simulação. */
  simulacaoAprovar: 'Aprovar',
  simulacaoRecusar: 'Recusar',
  simulacaoAnalise: 'Deixar em análise',
  /**
   * caução, novo, aprovado (ajuste A1): no item da pré-autorização, no lugar do painel, quando o cargo da sessão não inicia
   * a locação com caução (só ATENDENTE e ADMINISTRADOR iniciam).
   */
  caucaoCargoSemPermissao: 'Com caução, só atendente ou administrador inicia a locação.',
  /**
   * caução, novo, aprovado (encurtado em 24/11/2026: o "A locação não foi iniciada" já vem nos textos do servidor):
   * complemento do erro 409 com a caução exigida e a retirada ainda disponível.
   */
  caucaoTenteNovamente: 'Escolha de novo o resultado da simulação e tente outra vez.',
  /** desenho: aparece só com a retirada pronta. */
  prontoParaDespacho: 'Pronto para despacho de pátio',
  /** Texto de hoje (mantido, aprovado pelo Kaio em 24/11/2026): diz o que o clique faz. */
  botaoPrincipal: 'Iniciar locação e ir para vistoria',
  locacaoJaIniciada: 'Locação já iniciada',
  continuarVistoria: 'Continuar para vistoria',
  existente: 'Esta reserva já possui uma locação. Continue para a vistoria.',
  /** ajustado: "volte às retiradas" (antes "volte às reservas"). */
  indisponivel: 'A retirada exige uma reserva ativa ou confirmada e um veículo reservado. Atualize os dados ou volte às retiradas.',
  conferir: 'Confira os documentos originais antes de continuar.',
  sucesso: 'Locação iniciada. Continue com a vistoria de entrada.',
  conflito: 'Os dados da retirada mudaram. Confira os dados atualizados antes de continuar.',
  recuperar: 'Não foi possível confirmar a abertura. Os dados da reserva foram consultados novamente. Se a locação já foi criada, continue para a vistoria.',
  /** Página de entrada da retirada (locacao.contract.json, pagina_de_entrada_da_retirada.textos_novos): novos, aprovados pelo Kaio em 24/11/2026. */
  entradaTitulo: 'Retirada de veículos',
  entradaSubtitulo: 'Abra a retirada pelo código da reserva, pela lista de hoje ou pela lista de devoluções pendentes.',
  entradaCampoRotulo: 'Código da reserva',
  entradaCampoDica: 'Exemplo: AV-00042 ou 42.',
  entradaCampoBotao: 'Abrir retirada',
  entradaCampoErro: 'Informe um código de reserva válido, como AV-00042.',
  entradaListaTitulo: (dataStr: string) => `Retiradas previstas para hoje (${dataStr})`,
  entradaColunaReserva: 'Reserva',
  entradaColunaCliente: 'Cliente',
  entradaColunaVeiculo: 'Veículo',
  entradaColunaPlaca: 'Placa',
  entradaColunaSituacao: 'Situação',
  entradaColunaAcao: 'Ação',
  /** Ação da linha de origem RESERVA (leva à tela 16 daquela reserva). */
  entradaAcaoReserva: 'Iniciar retirada',
  entradaListaVazia: 'Nenhuma retirada prevista para hoje.',
  /** Já existe em outra tela (mensagensPagamentosCliente): usado só quando a linha de locação não tem código de reserva. */
  locacaoNumero: (idLocacao: number) => `Locação nº ${idLocacao}`,
} as const;

/**
 * Nome da ação de cada linha da lista de hoje para o leitor de tela (acerto D1, aprovado pelo Kaio em 24/11/2026): o texto
 * visível do botão e a referência da linha (o código da reserva ou "Locação nº {id}"), como "Iniciar retirada AV-00042".
 * Sem referência, devolve undefined e o link fica só com o texto visível.
 */
export function mensagemAcaoLinhaRetirada(textoAcao: string, referencia: string): string | undefined {
  const referenciaLimpa = referencia.trim();
  return referenciaLimpa === '' ? undefined : `${textoAcao} ${referenciaLimpa}`;
}
