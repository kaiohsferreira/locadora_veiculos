/**
 * Mensagens do cadastro e da edição de veículo (P1-FE-03). Reexportadas por `@/lib/mensagens`.
 *
 * Três grupos:
 * - validação no navegador: mesma frase do servidor (VeiculoVOValidator / ConstantsMessage.Veiculo.cs), para o usuário
 *   ver o mesmo texto antes de enviar; o servidor continua sendo a palavra final;
 * - textos que o servidor manda e que o front procura para decidir o campo (TEXTO_SERVIDOR_*): não são mostrados
 *   como estão, só usados na comparação. O teste de `mapeamentoErroVeiculo` lê o arquivo do back-end e falha se a frase mudar;
 * - textos de interface escritos pelo front (MENSAGEM_VEICULO_DATA_INVALIDA), marcados como novos.
 */

// Validação no navegador (mesmas frases do servidor) ------------------------

export const MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA = 'Informe a categoria do veículo.';
export const MENSAGEM_VEICULO_PLACA_INVALIDA =
  'Informe uma placa válida, no padrão Mercosul (ABC1D23) ou antigo (ABC1234).';
export const MENSAGEM_VEICULO_CHASSI_INVALIDO =
  'O chassi deve ter 17 caracteres alfanuméricos, sem as letras I, O e Q.';
export const MENSAGEM_VEICULO_RENAVAM_INVALIDO = 'O renavam deve ter 11 dígitos numéricos.';
export const MENSAGEM_VEICULO_MARCA_OBRIGATORIA = 'Informe a marca do veículo.';
export const MENSAGEM_VEICULO_MODELO_OBRIGATORIO = 'Informe o modelo do veículo.';
export const MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR = 'O ano do modelo não pode ser anterior ao ano de fabricação.';
export const MENSAGEM_VEICULO_VALOR_DIARIA = 'O valor da diária deve ser maior que zero.';
export const MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO =
  'O valor da diária deve ter no máximo 8 dígitos inteiros e 2 casas decimais.';
export const MENSAGEM_VEICULO_VALOR_VENDA = 'O valor de venda deve ser maior que zero.';
export const MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO =
  'O valor de venda deve ter no máximo 10 dígitos inteiros e 2 casas decimais.';
export const MENSAGEM_VEICULO_DATA_FUTURA = 'A data de aquisição não pode ser posterior a hoje.';

/** "A marca deve ter no máximo 40 caracteres." */
export function mensagemVeiculoMarcaTamanho(maximo: number): string {
  return `A marca deve ter no máximo ${maximo} caracteres.`;
}

/** "O modelo deve ter no máximo 60 caracteres." */
export function mensagemVeiculoModeloTamanho(maximo: number): string {
  return `O modelo deve ter no máximo ${maximo} caracteres.`;
}

/** Odômetro de entrega obrigatório na tela (zero é valor válido). Texto novo do front: entra na lista de textos novos para aprovação do Kaio. */
export const MENSAGEM_VEICULO_KM_OBRIGATORIO = 'Informe o odômetro de entrega.';


/** "A cor deve ter no máximo 30 caracteres." */
export function mensagemVeiculoCorTamanho(maximo: number): string {
  return `A cor deve ter no máximo ${maximo} caracteres.`;
}

/** "O ano de fabricação deve estar entre 1980 e 2027." */
export function mensagemVeiculoAnoFabricacaoIntervalo(minimo: number, maximo: number): string {
  return `O ano de fabricação deve estar entre ${minimo} e ${maximo}.`;
}

/** "O ano do modelo não pode ser posterior a 2027." */
export function mensagemVeiculoAnoModeloMaximo(maximo: number): string {
  return `O ano do modelo não pode ser posterior a ${maximo}.`;
}

/** "O valor informado para o campo Marca é inválido." (caractere que o banco não aceita, como o servidor). */
export function mensagemVeiculoCampoInvalido(campo: string): string {
  return `O valor informado para o campo ${campo} é inválido.`;
}

// Texto novo do front ----------------------------------------------------------

/** Data de aquisição que não existe no calendário (o campo de data do navegador costuma impedir). */
export const MENSAGEM_VEICULO_DATA_INVALIDA = 'Informe uma data válida.';

// Textos que o servidor manda (só para comparar, nunca para mostrar como estão) --------

export const TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA = 'Já existe um veículo ativo com esta placa.';
export const TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO = 'Já existe um veículo ativo com este chassi.';
export const TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO = 'Já existe um veículo ativo com este renavam.';
export const TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO = 'O veículo informado não foi encontrado.';

// Seletor de categoria -------------------------------------------------------------

/** Rótulo de uma categoria no seletor: "Grupo G - SUV Compacto", ou só o nome quando não há descrição. */
export function mensagemVeiculoRotuloCategoria(nome: string, descricao: string | null | undefined): string {
  const descricaoAparada = descricao?.trim() ?? '';
  return descricaoAparada ? `${nome} - ${descricaoAparada}` : nome;
}

// Campos novos da tela 05 (decisão do Kaio, 16/11/2026): os mesmos textos do servidor (ConstantsMessage.Veiculo) ---

/** "O fornecedor deve ter no máximo 100 caracteres." */
export function mensagemVeiculoFornecedorTamanho(maximo: number): string {
  return `O fornecedor deve ter no máximo ${maximo} caracteres.`;
}

/** "O número da nota fiscal de compra deve ter no máximo 20 caracteres." */
export function mensagemVeiculoNotaFiscalTamanho(maximo: number): string {
  return `O número da nota fiscal de compra deve ter no máximo ${maximo} caracteres.`;
}

/** Nome do campo na mensagem de valor inválido, como o servidor escreve (NomeCampoNotaFiscalCompraVeiculo). */
export const MENSAGEM_VEICULO_NOME_CAMPO_NOTA_FISCAL = 'Nota fiscal de compra';

// Tela de cadastro e edição de veículo (tela 05): textos do desenho, escritos literalmente ---------------------------

export const MENSAGEM_VEICULO_TRILHA_NAVEGACAO = 'Navegação estrutural';
export const MENSAGEM_VEICULO_TRILHA_FROTA = 'Frota';
export const MENSAGEM_VEICULO_TRILHA_CADASTRAR = 'Cadastrar Veículo';
export const MENSAGEM_VEICULO_TRILHA_EDITAR = 'Editar Veículo';
export const MENSAGEM_VEICULO_TITULO_CADASTRAR = 'Cadastrar Novo Veículo';
export const MENSAGEM_VEICULO_TITULO_EDITAR = 'Editar Veículo';
export const MENSAGEM_VEICULO_APOIO_CADASTRAR =
  'Preencha os dados técnicos, comerciais e de aquisição para integrar o ativo à frota operacional.';
/** Texto de apoio na edição. Texto novo do front (decisão do Kaio, 17/11/2026): entra na lista de textos para aprovação. */
export const MENSAGEM_VEICULO_APOIO_EDITAR = 'Atualize os dados técnicos, comerciais e de aquisição do veículo.';

export const MENSAGEM_VEICULO_SECAO_IDENTIFICACAO = '1. Identificação Técnica do Veículo';
export const MENSAGEM_VEICULO_SECAO_COMERCIAL = '2. Parâmetros Comerciais e Tarifação';
export const MENSAGEM_VEICULO_SECAO_AQUISICAO = '3. Registro de Aquisição e Fornecedor';
export const MENSAGEM_VEICULO_ETIQUETA_OBRIGATORIOS = 'Dados Obrigatórios';
export const MENSAGEM_VEICULO_ETIQUETA_RECEITA = 'Matriz de Receita';
export const MENSAGEM_VEICULO_ETIQUETA_AUDITORIA = 'Auditoria Contábil';

export const MENSAGEM_VEICULO_ROTULO_PLACA = 'Placa (Mercosul)';
export const MENSAGEM_VEICULO_APOIO_PLACA = 'Padrão Brasil';
export const MENSAGEM_VEICULO_PLACEHOLDER_PLACA = 'ABC1D23';
export const MENSAGEM_VEICULO_EMBLEMA_PLACA = 'BR';
export const MENSAGEM_VEICULO_ROTULO_CHASSI = 'Chassi (VIN - 17 Dígitos)';
export const MENSAGEM_VEICULO_ROTULO_RENAVAM = 'Renavam (11 Dígitos)';
export const MENSAGEM_VEICULO_ROTULO_MARCA = 'Marca Fabricante';
export const MENSAGEM_VEICULO_ROTULO_MODELO = 'Modelo e Versão Completa';
export const MENSAGEM_VEICULO_ROTULO_ANO_FABRICACAO = 'Ano Fabricação';
export const MENSAGEM_VEICULO_ROTULO_ANO_MODELO = 'Ano Modelo';
export const MENSAGEM_VEICULO_ROTULO_COR = 'Cor Externa / Acabamento';
export const MENSAGEM_VEICULO_ROTULO_CATEGORIA = 'Grupo Operacional / Categoria';
export const MENSAGEM_VEICULO_ROTULO_STATUS_INICIAL = 'Status Inicial de Entrada';
export const MENSAGEM_VEICULO_ROTULO_DIARIA = 'Valor Base da Diária (Locação)';
export const MENSAGEM_VEICULO_ROTULO_VALOR_SEMINOVO = 'Valor Referência Seminovos (Desmobilização)';
export const MENSAGEM_VEICULO_ROTULO_DATA_COMPRA = 'Data da Compra';
export const MENSAGEM_VEICULO_ROTULO_ODOMETRO = 'Odômetro de Entrega';
export const MENSAGEM_VEICULO_ROTULO_FORNECEDOR = 'Fornecedor / Concessionária';
export const MENSAGEM_VEICULO_ROTULO_NOTA_FISCAL = 'Nota Fiscal (NF-e)';
export const MENSAGEM_VEICULO_PREFIXO_REAIS = 'R$';
export const MENSAGEM_VEICULO_SUFIXO_KM = 'KM';

/** Opção do seletor de status inicial travado: o único status que o servidor aceita no cadastro. */
export const MENSAGEM_VEICULO_STATUS_INICIAL_DISPONIVEL = '● Disponível para Locação Imediata';

/** Textos das cores da tela 05 (as quatro amostras do desenho). O valor enviado é o da coluna da lista de cores. */
export const MENSAGEM_VEICULO_COR_PRETO = 'Preto Carbon';
export const MENSAGEM_VEICULO_COR_BRANCO = 'Branco Polar';
export const MENSAGEM_VEICULO_COR_PRATA = 'Prata Shark';
export const MENSAGEM_VEICULO_COR_CINZA = 'Cinza Silver';

export const MENSAGEM_VEICULO_BOTAO_CANCELAR = 'Cancelar / Voltar';
export const MENSAGEM_VEICULO_BOTAO_RASCUNHO = 'Salvar como Rascunho';
export const MENSAGEM_VEICULO_BOTAO_SALVAR_ATIVAR = 'Salvar e Ativar Veículo';
export const MENSAGEM_VEICULO_BOTAO_SALVAR_EDICAO = 'Salvar Alterações';
export const MENSAGEM_VEICULO_BOTAO_GRAVANDO = 'Gravando...';
export const MENSAGEM_VEICULO_BOTAO_ATIVADO = 'Veículo Ativado!';
export const MENSAGEM_VEICULO_ACOES_ARIA = 'Ações de cadastro';

// Fluxo de cadastro e edição (decisões do Kaio, 16/11/2026): textos novos do front, listados para aprovação -----------

export const MENSAGEM_VEICULO_TENTAR_DE_NOVO = 'Tentar de novo';
export const MENSAGEM_VEICULO_CATEGORIAS_CARREGANDO = 'Carregando categorias...';
export const MENSAGEM_VEICULO_CATEGORIAS_ERRO = 'Não foi possível carregar as categorias.';
export const MENSAGEM_VEICULO_CATEGORIAS_VAZIO = 'Nenhuma categoria ativa cadastrada.';
export const MENSAGEM_VEICULO_NAO_ENCONTRADO = 'Veículo não encontrado.';
export const MENSAGEM_VEICULO_NAO_ENCONTRADO_DESCRICAO = 'Ele não existe ou não pertence à sua locadora.';
export const MENSAGEM_VEICULO_EDICAO_CONCLUIDA = 'Alterações salvas com sucesso.';
export const MENSAGEM_VEICULO_FOTOS_COM_FALHA =
  'Algumas fotos não foram enviadas. Use "Tentar de novo" em cada uma delas; o veículo continua cadastrado.';
export const MENSAGEM_VEICULO_RASCUNHO_RECUPERADO =
  'Encontramos um rascunho salvo neste navegador e preenchemos o formulário com ele. As fotos não ficam no rascunho.';
export const MENSAGEM_VEICULO_RASCUNHO_DESCARTAR = 'Descartar rascunho';
export const MENSAGEM_VEICULO_RASCUNHO_SALVO = 'Rascunho salvo neste navegador.';
export const MENSAGEM_VEICULO_RASCUNHO_FALHOU = 'Não foi possível salvar o rascunho neste navegador.';
