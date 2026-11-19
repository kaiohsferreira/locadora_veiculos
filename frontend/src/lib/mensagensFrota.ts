/**
 * Textos da tela Gestão de Frota (P1-FE-01, tela 11). Reexportados por `@/lib/mensagens`.
 *
 * Os textos vêm do desenho D:/Estudo/TI/telas/11_back_office_gest_o_de_frota_1.html, com as trocas do contrato
 * frota-tela11 v1: sem pátio, sem "Km Atual & Motor", sem a dica "Histórico do Veículo", com "Reservados" no lugar de
 * "Bloqueado / Sinistro" e de "Aguardando Laudo". Os textos marcados como "de interface" não estão no desenho.
 */

// Cabeçalho ------------------------------------------------------------------

export const MENSAGEM_FROTA_TITULO_PAGINA = 'Gestão de Frota';
/** Texto pequeno acima do título (sem o local: a tela não mostra pátio). */
export const MENSAGEM_FROTA_KICKER = 'Ativos Operacionais';
export const MENSAGEM_FROTA_DESCRICAO =
  'Controle geral de veículos, status operacional, manutenções e disponibilidade.';
/** Sem o "+" do desenho (que estava duplicado no botão): o ícone de mais já aparece à esquerda. */
export const MENSAGEM_FROTA_BOTAO_CADASTRAR = 'Cadastrar Novo Veículo';

// Cartões --------------------------------------------------------------------

export const MENSAGEM_FROTA_CARREGANDO_RESUMO = 'Carregando o resumo da frota';
export const MENSAGEM_FROTA_CARTAO_TOTAL = 'Total da Frota';
export const MENSAGEM_FROTA_CARTAO_DISPONIVEIS = 'Disponíveis';
export const MENSAGEM_FROTA_CARTAO_LOCADOS = 'Locados em Trânsito';
export const MENSAGEM_FROTA_CARTAO_OFICINA = 'Oficina / Revisão';
export const MENSAGEM_FROTA_CARTAO_RESERVADOS = 'Reservados';

export const MENSAGEM_FROTA_UNIDADES = 'unidades';
export const MENSAGEM_FROTA_VEICULOS = 'veículos';
export const MENSAGEM_FROTA_EM_USO = 'em uso';
export const MENSAGEM_FROTA_ATIVOS = 'ativos';
export const MENSAGEM_FROTA_NOVOS_NO_MES = 'novos neste mês';

/** "63,6% de disponibilidade". O percentual já vem formatado. */
export function mensagemFrotaDisponibilidade(percentual: string): string {
  return `${percentual} de disponibilidade`;
}

/** "28,9% taxa ocupação" (texto do desenho). */
export function mensagemFrotaTaxaOcupacao(percentual: string): string {
  return `${percentual} taxa ocupação`;
}

/** "5,0% em serviço" (texto do desenho). */
export function mensagemFrotaEmServico(percentual: string): string {
  return `${percentual} em serviço`;
}

/** "2,5% em reserva" (de interface: o cartão de reservados não existe no desenho). */
export function mensagemFrotaEmReserva(percentual: string): string {
  return `${percentual} em reserva`;
}

/** Chips dos cartões, com os textos do desenho (o de reservados é de interface). */
export const MENSAGEM_FROTA_CHIP_DISPONIVEL = 'Disponível';
export const MENSAGEM_FROTA_CHIP_LOCADO = 'Em Contrato';
export const MENSAGEM_FROTA_CHIP_OFICINA = 'Em Revisão';
export const MENSAGEM_FROTA_CHIP_RESERVADO = 'Reservado';

// Situação (pílulas) ---------------------------------------------------------

export const MENSAGEM_FROTA_FILTRO_SITUACAO = 'Filtrar por situação';
export const MENSAGEM_FROTA_PILULA_TODOS = 'Todos';
export const MENSAGEM_FROTA_PILULA_DISPONIVEIS = 'Disponíveis';
export const MENSAGEM_FROTA_PILULA_LOCADOS = 'Locados';
export const MENSAGEM_FROTA_PILULA_MANUTENCAO = 'Manutenção';
export const MENSAGEM_FROTA_PILULA_RESERVADOS = 'Reservados';

// Busca e categoria ----------------------------------------------------------

/** Rótulo para leitor de tela (o campo mostra só o ícone e o texto de exemplo). */
export const MENSAGEM_FROTA_BUSCA_ROTULO = 'Buscar veículo';
/** Texto de exemplo: a busca vale para placa, chassi, modelo e marca (o desenho dizia só placa, chassi ou modelo). */
export const MENSAGEM_FROTA_BUSCA_PLACEHOLDER = 'Filtrar por placa, chassi, modelo ou marca...';
export const MENSAGEM_FROTA_CATEGORIA_ROTULO = 'Categoria de veículo';
export const MENSAGEM_FROTA_CATEGORIA_TODAS = 'Todas as Categorias (Grupos)';

// Tabela ---------------------------------------------------------------------

export const MENSAGEM_FROTA_TABELA_LEGENDA = 'Veículos da frota';
export const MENSAGEM_FROTA_COLUNA_VEICULO = 'Veículo & Grupo';
export const MENSAGEM_FROTA_COLUNA_PLACA = 'Placa Mercosul';
export const MENSAGEM_FROTA_COLUNA_CHASSI = 'Chassi & Ano';
/** Sem "& Motor": câmbio e motor entram quando o cadastro tiver esses campos. */
export const MENSAGEM_FROTA_COLUNA_KM = 'Km Atual';
export const MENSAGEM_FROTA_COLUNA_STATUS = 'Status Operacional';
export const MENSAGEM_FROTA_COLUNA_ACOES = 'Ações';
export const MENSAGEM_FROTA_BOTAO_DETALHES = 'Detalhes';
/** Nome acessível do botão Detalhes: o texto visível é igual em todas as linhas. */
export function mensagemFrotaDetalhesDoVeiculo(placa: string): string {
  return `do veículo ${placa}`;
}
export const MENSAGEM_FROTA_BRASIL = 'BRASIL';

// Rodapé ---------------------------------------------------------------------

export const MENSAGEM_FROTA_RODAPE_ITENS = 'veículos cadastrados';
export const MENSAGEM_FROTA_LINHAS_POR_PAGINA = 'Linhas por página:';

// Estados --------------------------------------------------------------------

export const MENSAGEM_FROTA_VAZIA_SEM_FILTRO_TITULO = 'Nenhum veículo cadastrado.';
export const MENSAGEM_FROTA_VAZIA_SEM_FILTRO_DESCRICAO =
  'Cadastre o primeiro veículo para ele aparecer nesta lista.';
export const MENSAGEM_FROTA_VAZIA_COM_FILTRO_TITULO = 'Nenhum veículo encontrado.';
export const MENSAGEM_FROTA_VAZIA_COM_FILTRO_DESCRICAO = 'Ajuste a busca ou os filtros e tente de novo.';
export const MENSAGEM_FROTA_LIMPAR_FILTROS = 'Limpar filtros';
export const MENSAGEM_FROTA_ERRO_LISTA = 'Não foi possível carregar a frota.';
/** Aviso acima dos cartões quando o resumo (visão geral) não carrega. Texto de interface, a aprovar pelo Kaio. */
export const MENSAGEM_FROTA_ERRO_RESUMO = 'Não foi possível carregar o resumo da frota.';
/** Botão de repetir a consulta do resumo ou da lista. Texto de interface, igual ao do EstadoErro. */
export const MENSAGEM_FROTA_TENTAR_NOVAMENTE = 'Tentar novamente';

// Menu operacional (etapa 2: troca de status pela tela) ----------------------

/** Dica do botão de três pontos (texto do desenho 11, título do botão). */
export const MENSAGEM_FROTA_MENU_TITULO = 'Menu Operacional';
/** Nome acessível do botão do menu: a placa diz de qual linha se trata (texto só para leitor de tela). */
export function mensagemFrotaMenuDoVeiculo(placa: string): string {
  return `Menu operacional do veículo ${placa}`;
}
/** Itens do menu (de interface: o desenho mostra só o ícone). */
export const MENSAGEM_FROTA_ITEM_MANUTENCAO = 'Enviar para manutenção';
export const MENSAGEM_FROTA_ITEM_DISPONIVEL = 'Voltar para disponível';
export const MENSAGEM_FROTA_ITEM_BAIXA = 'Dar baixa';
/** Frase do menu para RESERVADO e LOCADO (a mesma frase do servidor, já aprovada). */
export const MENSAGEM_FROTA_FRASE_STATUS_POR_FLUXO = 'O status atual do veículo só muda pela reserva ou pela locação.';
/** Frase do menu para VENDIDO e BAIXADO (a mesma frase do servidor, já aprovada). */
export const MENSAGEM_FROTA_FRASE_VENDIDO_OU_BAIXADO = 'Veículo vendido ou baixado não pode ter o status alterado.';

// Confirmação da baixa (de interface) ----------------------------------------

/** Título da confirmação da baixa, com a placa do veículo. */
export function mensagemFrotaBaixaTitulo(placa: string): string {
  return `Dar baixa no veículo ${placa}?`;
}
export const MENSAGEM_FROTA_BAIXA_TEXTO =
  'A baixa não pode ser desfeita pelo sistema. O veículo sai da frota em operação, e placa, chassi e renavam continuam ocupados.';
export const MENSAGEM_FROTA_CANCELAR = 'Cancelar';

// Avisos da troca de status (de interface) -----------------------------------

export const MENSAGEM_FROTA_SUCESSO_MANUTENCAO = 'Veículo enviado para manutenção.';
export const MENSAGEM_FROTA_SUCESSO_DISPONIVEL = 'Veículo de volta para disponível.';
export const MENSAGEM_FROTA_SUCESSO_BAIXA = 'Baixa registrada.';

// Alternância Painel | Lista (cabeçalho das telas 11 e 12; texto novo, aprovado no plano T) -----

export const MENSAGEM_FROTA_ALTERNANCIA_GRUPO = 'Visão da frota';
export const MENSAGEM_FROTA_ALTERNANCIA_PAINEL = 'Painel';
export const MENSAGEM_FROTA_ALTERNANCIA_LISTA = 'Lista';

// Listagem da frota (tela 12, P1-FE-02) ------------------------------------------------------------

/** Cartões do desenho 12 (textos novos; o título da tela 11 para "Locados" é outro). */
export const MENSAGEM_FROTA_RESUMO_DISPONIVEIS = 'Veículos Disponíveis';
export const MENSAGEM_FROTA_RESUMO_LOCADOS = 'Locados';
export const MENSAGEM_FROTA_RESUMO_EM_MANUTENCAO = 'Em Manutenção';
export const MENSAGEM_FROTA_RESUMO_TAXA_OCUPACAO = 'Taxa de Ocupação';
export const MENSAGEM_FROTA_RESUMO_CONTRATOS_VIGENTES = 'Contratos vigentes';
export const MENSAGEM_FROTA_NENHUMA_ENCERRA_HOJE = 'Nenhuma encerra hoje';
export const MENSAGEM_FROTA_UMA_ENCERRA_HOJE = '1 encerra hoje';

/** "31 encerram hoje" (o número já vem formatado por `@/lib/formatters`). */
export function mensagemFrotaEncerramHoje(numeroFormatado: string): string {
  return `${numeroFormatado} encerram hoje`;
}

/** Filtro de status da tela 12 (lista suspensa). */
export const MENSAGEM_FROTA_STATUS_ROTULO = 'Status do veículo';
export const MENSAGEM_FROTA_STATUS_TODOS = 'Todos os Status';
export const MENSAGEM_FROTA_LIMPAR_BOTAO = 'Limpar';
/** Título e nome acessível do botão Limpar (o texto some em telas pequenas). */
export const MENSAGEM_FROTA_LIMPAR_TITULO = 'Limpar Filtros';

/** Colunas da tela 12 (as de status e ações reaproveitam as constantes da tela 11). */
export const MENSAGEM_FROTA_LISTAGEM_COLUNA_PLACA = 'Identificação / Placa';
export const MENSAGEM_FROTA_LISTAGEM_COLUNA_MODELO = 'Modelo & Especificação';
export const MENSAGEM_FROTA_LISTAGEM_COLUNA_CATEGORIA = 'Categoria';
export const MENSAGEM_FROTA_LISTAGEM_COLUNA_ODOMETRO = 'Odômetro';

export const MENSAGEM_FROTA_CHASSI_ROTULO = 'Chassi:';
/** "2024 • Chassi: 9BR58...7821" (linha de baixo do modelo; o chassi já vem abreviado). */
export function mensagemFrotaAnoEChassi(ano: number, chassi: string): string {
  return `${ano} • ${MENSAGEM_FROTA_CHASSI_ROTULO} ${chassi}`;
}

/** Botão de lápis: abre o cadastro do veículo (todo funcionário). Nome acessível com a placa. */
export const MENSAGEM_FROTA_EDITAR = 'Editar';
export function mensagemFrotaEditarDoVeiculo(placa: string): string {
  return `Editar veículo ${placa}`;
}
