/**
 * Textos da tela 08 (emissão e assinatura do contrato digital, P4-FE-03). Os textos de erro do servidor não ficam aqui:
 * a tela mostra o texto que o servidor manda (obterMensagemErro). Os desta lista vêm do anexo
 * docs/aprovacao-eduardo-telas-26-11.md (tela 08) e das decisões N9 e N12 de 26/11/2026, aprovados pelo Kaio ("Aprovo como
 * esta", e a troca "Marque os aceites para assinar."). "Não foi possível mostrar a assinatura guardada." também foi aprovado em 26/11/2026.
 * Reexportado por `@/lib/mensagens`.
 */
export const MENSAGENS_CONTRATO_LOCACAO = {
  /** desenho: título da página (mantido). */
  titulo: 'Emissão de Contrato Digital',
  /** desenho: última migalha da trilha. */
  migalha: 'Contrato Digital',
  /** TROCAR (anexo tela 08): subtítulo sem "revisão jurídica" e sem "assinatura eletrônica". */
  subtitulo: 'Revisão das cláusulas de locação, aceite dos termos e coleta da assinatura do cliente.',
  /** desenho: chip do locatário (mantido). */
  rotuloLocatario: 'Locatário',
  /** desenho: chip do número (mantido); o valor vira "Locação nº {id}". */
  rotuloContrato: 'Contrato',
  numeroLocacao: (numero: number) => `Locação nº ${numero}`,
  /** NOVO (anexo tela 08): etapa concluída, sem horário (a locação aberta prova a retirada conferida). */
  etapaConcluida: 'Concluída',
  etapaConcluidaComHora: (hora: string) => `Concluída ${hora}`,
  /** Pendente: texto da barra do documento (no lugar de "Minuta Validada pelo Jurídico"). */
  tituloPapelPendente: 'Minuta do contrato',
  /** Assinado: título da barra do documento. */
  tituloPapelAssinado: 'Contrato assinado',
  /** TROCAR (anexo): nome acessível e title do botão de imprimir. */
  imprimir: 'Imprimir contrato',
  /** desenho: valor do cabeçalho do documento (CNPJ), sem a inscrição estadual (cortada). */
  cnpj: (cnpj: string) => `CNPJ: ${cnpj}`,
  /** TROCAR (anexo): título do papel, com o número da locação. */
  tituloContrato: (numero: number) => `CONTRATO DE LOCAÇÃO DE VEÍCULOS Nº ${numero}`,
  /** TROCAR (anexo): preâmbulo com as partes reais. */
  preambulo: (razaoSocial: string, cnpj: string, nome: string, cpfMascarado: string, categoria: string) =>
    `Pelo presente instrumento particular, de um lado ${razaoSocial}, CNPJ ${cnpj}, e de outro lado, como locatário(a), ${nome}, CPF ${cpfMascarado}, CNH categoria ${categoria}, celebram o presente contrato sob as cláusulas a seguir:`,
  /** TROCAR (anexo): título do cabeçalho de cada cláusula. */
  tituloClausula: (numero: number, titulo: string) => `CLÁUSULA ${numero}ª – ${titulo}`,
  /** Cláusula 1: rótulos dos dados do veículo (Placa é TROCAR: "Placa:"). */
  veiculoModelo: 'Modelo:',
  veiculoPlaca: 'Placa:',
  veiculoChassi: 'Chassi:',
  veiculoRenavam: 'Renavam:',
  /** TROCAR (anexo): rótulo do resumo "Valores Fechados". */
  valoresPrevistos: 'Valores previstos',
  /** NOVO (anexo, N12): cartão "Diárias", com o texto "{n} x R$ {valor da diária}". */
  diariasRotulo: 'Diárias',
  diariasCartao: (quantidade: number, valorDiariaStr: string) => `${quantidade} x ${valorDiariaStr}`,
  /** TROCAR (anexo): frase antes do quadro de valores da cláusula de valores. */
  valoresPrevistosFrase: (totalStr: string) => `O valor previsto da locação é de ${totalStr}, assim discriminado:`,
  /** NOVO (anexo): frase depois do quadro de valores; as extras ficam fora do total previsto. */
  cobrancasForaDoTotal:
    'Cobranças extras (quilometragem excedente, combustível, atraso e avarias) são apuradas na devolução e não estão incluídas no total previsto.',
  /** desenho: título do resumo (mantido). */
  resumoTitulo: 'Resumo Executivo dos Termos',
  /** TROCAR (anexo): cartão "Quilometragem" vira "Franquia de km". */
  franquiaRotulo: 'Franquia de km',
  franquiaTotal: (km: string) => `${km} km (total)`,
  franquiaPorDiaria: (km: string) => `${km} km por diária`,
  franquiaNaoInformada: 'Não informada',
  /** desenho: cartão de combustível (mantido); valor TROCAR. */
  combustivelRotulo: 'Combustível',
  combustivelNaRetirada: (percentual: number) => `${percentual}% na retirada`,
  /** NOVO (anexo, N12): cartão de caução. */
  caucaoRotulo: 'Caução',
  semCaucao: 'Sem caução',
  /** desenho: bloco dos termos (mantido). */
  termosTitulo: 'Termos e Condições Obrigatórios',
  /** desenho: título do primeiro aceite (mantido); o texto vem do contrato (ver aceites). */
  aceiteCondicoesTitulo: 'Condições Gerais de Locação',
  /** desenho: título do segundo aceite (mantido). */
  aceiteCombustivelTitulo: 'Política de Nível de Combustível',
  /** Textos dos aceites aprovados no contrato (aprovacao-eduardo-finalizar-26-11.md, seção 3). */
  aceiteCondicoesTexto: 'Li e aceito as condições gerais deste contrato.',
  aceiteCombustivelTexto: 'Li e aceito a política de combustível deste contrato.',
  /** desenho: título da área de assinatura (mantido). */
  assinaturaTitulo: 'Área de Assinatura do Cliente',
  /** TROCAR (anexo): rótulo do quadro antes e depois de assinado. */
  assinaturaRotuloAntes: 'Assinatura do cliente',
  assinaturaRotuloDepois: 'Assinatura coletada',
  /** NOVO (anexo): dica do quadro e nome acessível. */
  quadroDica: 'Assine aqui com o dedo, a caneta ou o mouse.',
  quadroNomeAcessivel: 'Quadro de assinatura do cliente',
  /** Rótulo do campo de nome (anexo tela 08, aprovado pelo Kaio em 26/11/2026). */
  nomeAssinanteRotulo: 'Nome de quem assina',
  /** Rótulos das linhas de dado que cada cláusula indica ("indicada neste contrato"), aprovados pelo Kaio em 26/11/2026. */
  dadosClausulas: {
    retirada: 'Retirada',
    devolucaoPrevista: 'Devolução prevista',
    diaria: 'Diária',
    caucao: 'Caução',
    tanqueCompleto: 'Tanque completo',
    franquia: 'Franquia',
    kmExcedente: 'Km excedente',
  },
  /** Usado com o nome do assinante e o CPF mascarado, abaixo do quadro (desenho). */
  assinanteCpf: (cpfMascarado: string) => `CPF: ${cpfMascarado}`,
  /** TROCAR (anexo): título do registro, só depois de assinado. */
  registroTitulo: 'Registro da assinatura',
  /** Linha do bloco "Registro da assinatura" (anexo tela 08, "Estado assinado"; aprovada pelo Kaio em 26/11/2026). */
  registroLinha: (dataHora: string, nomeAssinante: string, funcionario: string) =>
    `Assinado em ${dataHora} por ${nomeAssinante} • coletado por ${funcionario}`,
  /** Carimbo logo abaixo da imagem da assinatura, só depois de assinado (anexo tela 08, aprovado em 26/11/2026). */
  carimboAssinatura: (dataHora: string, funcionario: string) =>
    `Assinado na tela em ${dataHora} • Coletado por ${funcionario}.`,
  /** desenho: botão de limpar o quadro (mantido). */
  limparAssinatura: 'Limpar Assinatura',
  /** TROCAR (anexo): botão principal. */
  assinarContrato: 'Assinar contrato',
  /** NOVO (anexo): botão enquanto envia. */
  assinando: 'Assinando...',
  /** NOVO (anexo, validação antes de assinar). */
  aceitesParaAssinar: 'Marque os aceites para assinar.',
  assinaturaParaAssinar: 'Desenhe a assinatura no quadro antes de assinar.',
  /** Mesmo texto do servidor (ContratoLocacao.ErroContratoNomeAssinante), para o validador local. */
  nomeVazio: 'Informe o nome de quem assina o contrato.',
  nomeTamanho: 'O nome do assinante deve ter no máximo 100 caracteres.',
  nomeInvalido: 'O nome do assinante contém caracteres inválidos.',
  /** Mesmo texto do servidor (ContratoLocacao.ErroContratoAssinaturaTamanho). */
  assinaturaTamanho: 'A assinatura excede o tamanho máximo permitido.',
  /** Aprovado pelo Kaio em 26/11/2026: a assinatura guardada não é um PNG em base64 válido, então a imagem não é mostrada. */
  assinaturaIndisponivel: 'Não foi possível mostrar a assinatura guardada.',
  /** NOVO (anexo): aviso de sucesso. */
  assinadoComSucesso: 'Contrato assinado com sucesso.',
  /** NOVO (anexo): estado assinado. */
  seloAssinado: 'Contrato assinado',
  hashRotulo: 'Hash SHA-256 do documento',
  /** TROCAR (anexo): rodapé do papel, só com o contrato assinado. */
  hashPapel: (hash: string) => `Hash SHA-256: ${hash}`,
  aceito: 'Aceito',
  voltarLocacoes: 'Voltar às locações',
  /** NOVO (anexo): erro ao carregar. */
  erroCarregar: 'Não foi possível carregar o contrato.',
  tentarNovamente: 'Tentar novamente',
  /** NOVO (anexo): 409 sem vistoria de entrada (RP-06). */
  contratoIndisponivel: 'Contrato indisponível.',
  registrarVistoriaEntrada: 'Registrar vistoria de entrada',
  /** Texto do servidor da RP-06 (ContratoLocacao.ErroContratoSemVistoriaEntrada); a tela o reconhece pela citação. */
  citacaoSemVistoria: '(RP-06)',
  /** Botão do 404 (anexo). */
  voltarRetiradas: 'Voltar às retiradas',
} as const;
