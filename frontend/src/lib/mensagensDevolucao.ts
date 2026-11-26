/**
 * Textos da tela 06 (devolução do veículo: check-out e encerramento) e da lista de devoluções pendentes da página de entrada
 * da retirada. Os textos vêm do anexo aprovado pelo Kaio em 26/11/2026 (docs/aprovacao-eduardo-telas-26-11.md, seção "Tela 06"),
 * e das decisões N10 e N14 (docs/aprovacao-eduardo-finalizar-26-11.md). Os textos do formulário de saída já existiam e ficam
 * como estavam. Os avisos e erros do servidor não passam por aqui: vêm prontos da API.
 */
export const MENSAGENS_DEVOLUCAO = {
  titulo: 'Devolução do veículo',
  /** Aprovado (anexo, tela 06, "TROCAR"): substitui a descrição do desenho ("saída vs chegada"). */
  descricao: 'Conferência comparativa entre a retirada e a devolução, apuração de novas avarias e cálculo automático das cobranças extras.',
  idInvalido: 'Identificador da locação inválido.',
  km: 'Informe uma quilometragem inteira, igual ou superior à registrada na retirada e na entrada.',
  combustivel: 'Informe um valor entre 0 e 1, com até duas casas decimais.',
  descricaoInvalida: 'Informe uma descrição de 1 a 200 caracteres, sem caractere nulo.',
  observacoes: 'As observações não podem conter caractere nulo.',
  valor: 'Informe o valor estimado de cada nova avaria (use 0,00 quando não houver custo).',
  valorFaixa: 'Informe um valor entre 0 e 99999999,99 com até duas casas decimais.',
  confirmar: 'Confirme a conferência da comparação antes de registrar.',
  selecao: 'Selecione apenas avarias da entrada desta locação, sem repetir.',
  sucesso: 'Vistoria de saída registrada com sucesso.',
  invalido: 'Revise os campos indicados antes de registrar.',
  semEntrada: 'Registre a vistoria de entrada antes de realizar a vistoria de saída.',
  indisponivel: 'Esta locação não permite registrar uma vistoria de saída.',
  legado: 'Comparação não confirmada. Estas avarias não foram classificadas como novas ou preexistentes.',
  vazio: 'Nenhuma avaria neste grupo.',
  semValor: 'Valor não informado',
  gravidades: { LEVE: 'Leve', MEDIA: 'Média', GRAVE: 'Grave' },

  /** Estado sem vistoria de entrada (texto atual, mantido). */
  entradaNecessariaTitulo: 'Vistoria de entrada necessária',
  irParaVistoriaEntrada: 'Ir para vistoria de entrada',
  /** Estado sem vistoria de saída: aviso acima do formulário (NOVO, aprovado). */
  avisoSemSaida: 'Registre a vistoria de saída para ver a comparação, as cobranças extras e encerrar a locação.',

  /** Formulário da vistoria de saída (primeiro passo; textos atuais, mantidos). */
  formulario: {
    titulo: 'Registrar vistoria de saída',
    legenda: 'Conferência de saída',
    km: 'Quilometragem na devolução (km)',
    kmMinimo: (minimo: string) => `Mínimo: ${minimo}`,
    combustivel: 'Nível de combustível (0 a 1)',
    combustivelDica: 'Aceita vírgula e até duas casas decimais.',
    avariasEntradaLegenda: 'Avarias de entrada ainda presentes',
    avariasEntradaAjuda: 'Marque somente as avarias que continuam presentes. Os dados da entrada serão preservados.',
    novasTitulo: 'Novas avarias',
    novaLegenda: (posicao: number) => `Nova avaria ${posicao}`,
    descricao: 'Descrição',
    gravidade: 'Gravidade',
    valorEstimado: 'Valor estimado (R$)',
    removerAvaria: 'Remover avaria',
    removerAvariaNome: (posicao: number) => `Remover nova avaria ${posicao}`,
    adicionarAvaria: 'Adicionar nova avaria',
    observacoes: 'Observações (opcional)',
    conferenciaTitulo: 'Conferência da comparação',
    contagem: (existentes: string, novas: string) => `Já existiam: ${existentes} · Novas: ${novas}`,
    descricaoPendente: 'Descrição pendente',
    conferir: 'Conferi a comparação entre as avarias da entrada e as novas avarias. *',
    registrar: 'Registrar vistoria de saída',
    avariasRegistradasEntrada: 'Avarias registradas na entrada',
    referenciaConferencia: 'Referência para conferência',
  },

  /** Colunas da tela 06 (N10, item 1; aprovado). */
  colunaRetirada: 'Dados da Retirada (Check-in)',
  colunaDevolucao: 'Dados da Devolução (Check-out)',
  /** Linha de data e operador: "{dataHoraStr} • Operador: {nome}" (aprovado, anexo). */
  operador: (dataHora: string, nome: string) => `${dataHora} • Operador: ${nome}`,
  odometroInicial: '1. Odômetro Inicial',
  odometroFinal: '1. Odômetro Final Registrado',
  /** Mantido do desenho (sinal e unidade do delta). */
  delta: (quantidade: string) => `Delta: +${quantidade} km`,
  /** Mantido do desenho: a linha de cobrança excedente do odômetro final. */
  cobrancaExcedente: (valor: string) => `Cobrança Excedente: ${valor}`,
  /** Franquia (aprovado, anexo): mesma redação do ResultadoSimulacao. */
  franquiaContratada: (franquia: string) => `Franquia contratada: ${franquia}.`,
  /** Aprovado (anexo, TROCAR): sem ponto final, como no texto aprovado. */
  rodados: (quantidade: string, franquia: string) => `Rodados: ${quantidade} km • Franquia: ${franquia}`,
  /** Aprovado (26/11/2026): sem a franquia (simulação indisponível para o cargo), fica só "Rodados: {n} km". */
  rodadosSemFranquia: (quantidade: string) => `Rodados: ${quantidade} km`,
  franquia: (franquiaKm: number, baseTotal: boolean) => `${franquiaKm} km ${baseTotal ? 'no total' : 'por diária prevista'}`,
  combustivelTitulo: '2. Nível de Combustível',
  combustivelDivergenciaTitulo: '2. Nível de Combustível com Divergência',
  /** Aprovado (anexo): "Divergência de {k}/8 de tanque". */
  divergencia: (oitavos: number) => `Divergência de ${oitavos}/8 de tanque`,
  /** Aprovado pelo Kaio em 26/11/2026 ("Aprovo como está"): falta menor que um oitavo. */
  divergenciaAbaixoDeUmOitavo: 'Divergência abaixo de 1/8 de tanque',
  /** Aprovado (anexo, TROCAR): escala da régua na retirada, "Vazio (0/8)" e "4/8 (50%)". */
  escalaVazio: 'Vazio (0/8)',
  escalaMeio: '4/8 (50%)',
  reposicao: (valor: string) => `Cobrança Reposição: ${valor}`,
  avariasEntradaTitulo: '3. Avarias Pré-Existentes (Laudo Inicial)',
  /** Mantido do desenho; o singular "1 Registro" foi aprovado pelo Kaio em 26/11/2026. */
  registros: (quantidade: number) => (quantidade === 1 ? '1 Registro' : `${quantidade} Registros`),
  /** Aprovado (anexo, TROCAR): título do item de avaria da entrada. */
  avariaEntrada: (posicao: number, descricao: string) => `Avaria ${posicao}: ${descricao}`,
  /** Aprovado (anexo, CORTAR → "No lugar"): linha de apoio do item. */
  avariaApoio: (gravidade: string, valor: string) => `${gravidade} · ${valor}`,
  novasTitulo: '3. Novas Avarias Detectadas no Check-out',
  /** Mantido do desenho; o singular "1 Nova Irregularidade" foi aprovado pelo Kaio em 26/11/2026. */
  novasIrregularidades: (quantidade: number) => (quantidade === 1 ? '1 Nova Irregularidade' : `${quantidade} Novas Irregularidades`),
  /** Aprovado (anexo, TROCAR): "NOVA AVARIA #{n}: {descrição}", com a contagem depois das pré-existentes (N10, item 5). */
  novaAvaria: (numero: number, descricao: string) => `NOVA AVARIA #${numero}: ${descricao}`,
  gravidadeRotulo: 'Gravidade:',
  /** Aprovado (anexo, TROCAR): "{descrição}. Custo Estimado Reparo: {valor}" (mesma expressão da tela 19). */
  custoReparo: (descricao: string, valor: string) => `${descricao}. Custo Estimado Reparo: ${valor}`,
  valorACobrar: (valor: string) => `Valor a cobrar: ${valor}`,
  semNovasAvarias: 'Nenhuma nova avaria.',

  /** Cartão das cobranças extras. */
  cobrancasTitulo: 'Cobranças Extras',
  /** Aprovado (anexo, TROCAR): substitui "Cálculo em Tempo Real". */
  calculoAutomatico: 'Cálculo automático',
  kmExcedenteTitulo: 'Quilometragem excedente',
  kmExcedenteApoio: (rodados: string, franquiaKm: number) => `${rodados} km rodados (franquia ${franquiaKm} km)`,
  combustivelItem: 'Reabastecimento de combustível',
  combustivelApoio: (oitavos: number, valorTanque: string) => `${oitavos}/8 de tanque faltando x tanque completo ${valorTanque}`,
  /** Aprovado (anexo, TROCAR): o título deixa de ser "Tolerância de devolução". */
  atrasoTitulo: 'Atraso na devolução',
  atrasoApoio: (dataSaida: string, minutos: number) => `Devolvido em ${dataSaida} (tolerância de ${minutos} min)`,
  avariasTitulo: 'Avarias adicionais apuradas',
  avariasApoio: (quantidade: number) => `${quantidade} nova(s) avaria(s), estimativas da vistoria`,
  subtotal: 'Subtotal de Cobranças Extras',
  /** Caução (desenho mantido; o trecho do valor liberado é TROCAR aprovado). */
  caucaoTitulo: 'Caução Pré-autorizada',
  retencaoExtras: 'Retenção para quitação de extras:',
  /** Mantido do desenho: o valor retido aparece com sinal de desconto. */
  retencaoValor: (valor: string) => `- ${valor}`,
  caucaoLiberadaAoCliente: 'Valor da caução liberado ao cliente:',
  /** Frases por caso (anexo, NOVO aprovado). */
  caucaoSemExtras: (valor: string) => `Sem cobranças extras: a caução de ${valor} será liberada por inteiro.`,
  caucaoRetida: (retido: string, liberado: string) => `Serão retidos ${retido} da caução para quitar as cobranças extras. O restante, ${liberado}, será liberado.`,
  caucaoAcimaDaCaucao: (total: string, valor: string) => `As cobranças extras (${total}) passam do valor da caução. A caução de ${valor} será liberada por inteiro e o total será cobrado do cliente por PIX.`,
  semCaucao: 'Esta locação não tem caução. As cobranças extras serão cobradas do cliente por PIX.',
  /** Selo do total (anexo, NOVO aprovado). */
  seloLiquidado: 'Liquidado na Caução',
  seloAcobrarPix: 'A cobrar por PIX',
  seloSemCobrancas: 'Sem cobranças extras',
  totalTitulo: 'Total a Deduzir / Pagar',
  calculando: 'Calculando cobranças extras…',
  /** Aprovado (anexo, TROCAR → texto depois da seta): texto do ponto de estado do cabeçalho. */
  estadoAguardandoSaida: 'Aguardando vistoria de saída',
  estadoAguardandoEncerramento: 'Aguardando encerramento',
  estadoEncerrada: 'Locação encerrada',
  /** Botão e caixa de confirmação (anexo, NOVO aprovado). */
  encerrarBotao: 'Encerrar locação',
  encerrarNota: 'Ao encerrar, a locação é finalizada, a caução é liberada ou retida, a diária e as cobranças extras são lançadas e o veículo volta a ficar Disponível. Não é possível reabrir.',
  encerrando: 'Encerrando a locação…',
  semPermissaoEncerrar: 'Só atendente ou administrador apura as cobranças extras e encerra a locação.',
  semPolitica: 'Cadastre uma configuração ativa de cobranças extras para a categoria desta locação.',
  cancelada: 'Esta locação foi cancelada e não pode ser encerrada.',
  caixa: {
    titulo: 'Confirmar encerramento da locação',
    diarias: (quantidade: number, valorDiaria: string, total: string) => `Diárias (${quantidade} x ${valorDiaria}): ${total}`,
    extras: (total: string) => `Cobranças extras: ${total}`,
    total: (valor: string) => `Total da locação: ${valor}`,
    confirmo: 'Conferi a comparação, as cobranças extras e a caução. Confirmo o encerramento desta locação. Esta ação não pode ser desfeita.',
    confirmar: 'Confirmar encerramento',
    cancelar: 'Cancelar',
  },
  /** Complementos de erro (anexo, NOVO aprovado). */
  complementoFalha: 'A locação não foi encerrada. Confira os dados e tente de novo.',
  complemento409: 'Os dados da locação mudaram. Os dados foram atualizados; confira de novo antes de encerrar.',
  tentarDeNovo: 'Tentar de novo',

  /** Estado final, só leitura (anexo, NOVO aprovado). */
  finalizadaSelo: 'Locação encerrada',
  finalizadaTitulo: (data: string) => `Locação encerrada em ${data}`,
  valorTotalLocacao: (valor: string) => `Valor total da locação: ${valor}`,
  veiculoEstado: (disponivel: boolean, km: string) => `Veículo: ${disponivel ? 'Disponível' : 'Reservado'} • Quilometragem atual: ${km}`,
  caucaoLiberadaInteira: 'Caução liberada por inteiro.',
  caucaoRetidaLiberada: (retido: string, liberado: string) => `Caução retida: ${retido}. Liberado ao cliente: ${liberado}.`,
  caucaoLiberadaComPix: (total: string) => `Caução liberada. Cobrança por PIX: ${total}.`,

  /** Lista "Devoluções pendentes" na página de entrada da retirada (locacao.contract.json, v7, aprovado). */
  pendentes: {
    titulo: 'Devoluções pendentes',
    botao: 'Registrar devolução',
    atrasada: 'Atrasada',
    vazio: 'Nenhuma devolução pendente.',
    erroFiltro: 'O filtro de devoluções é inválido. Escolha em andamento ou atrasada.',
    colunas: {
      locacao: 'Locação',
      cliente: 'Cliente',
      veiculo: 'Veículo',
      placa: 'Placa',
      devolucaoPrevista: 'Devolução prevista',
      situacao: 'Situação',
      acao: 'Ação',
    },
  },
} as const;
