/** Textos fixos da gestão de seguros (P9-FE-01). As mensagens de erro do servidor chegam prontas (ErroApi). */
export const SEGUROS_TITULO_DOCUMENTO = 'Gestão de seguros';
export const SEGUROS_TITULO_PAGINA = 'Seguros';
export const SEGUROS_SUBTITULO = 'Apólices por veículo, com os vencimentos dos próximos 60 dias em destaque.';
export const SEGUROS_BOTAO_NOVA = 'Cadastrar apólice';
export const SEGUROS_RODAPE_ITENS = 'apólices';

/** Trilha do desenho 13: Operações > Gestão de risco e sinistros > Apólices de seguro. */
export const SEGUROS_TRILHA: readonly string[] = ['Operações', 'Gestão de risco e sinistros', 'Apólices de seguro'];
export const SEGUROS_TRILHA_ROTULO = 'Trilha de navegação';

export const SEGUROS_TITULO_LISTA = 'Apólices da Frota em Vigência';
export const SEGUROS_ITENS_POR_PAGINA = 'Itens por página:';
export const SEGUROS_SELO_RENOVACAO = 'Requer renovação imediata';
export const SEGUROS_FILTRO_VENCENDO = 'Vencendo em breve';
export const SEGUROS_STATUS_VIGENTE = 'Vigente';
export const SEGUROS_COLUNA_APOLICE = 'Apólice / Cobertura';
export const SEGUROS_COLUNA_VIGENCIA_PRAZOS = 'Vigência & Prazos';
export const SEGUROS_COLUNA_STATUS = 'Status';

export const SEGUROS_INDICADOR_CADASTRADAS = 'Apólices cadastradas';
export const SEGUROS_INDICADOR_CADASTRADAS_DETALHE = 'inclui as vencidas';
export const SEGUROS_INDICADOR_A_VENCER = 'Apólices a vencer (≤ 60 dias)';
export const SEGUROS_INDICADOR_A_VENCER_DETALHE = 'apólices';
export const SEGUROS_INDICADOR_A_VENCER_ACAO = 'Ver só as que vencem em até 60 dias';

export const SEGUROS_FILTRO_PLACA = 'Veículo';
export const SEGUROS_FILTRO_PLACA_EXEMPLO = 'Buscar por placa';
export const SEGUROS_FILTRO_SITUACAO = 'Situação';
export const SEGUROS_FILTRO_TODAS = 'Todas';
export const SEGUROS_FILTRO_REMOVER_PLACA = 'Remover filtro do veículo';
export const SEGUROS_FILTRO_PLACA_ETIQUETA = 'Placa';
export const SEGUROS_BUSCA_SEM_RESULTADO = 'Nenhum veículo com essa placa.';
export const SEGUROS_BUSCA_DIGITE = 'Digite pelo menos 2 caracteres da placa.';
export const SEGUROS_BUSCA_BUSCANDO = 'Buscando…';

export const SEGUROS_COLUNA_VEICULO = 'Veículo';
export const SEGUROS_COLUNA_SEGURADORA = 'Seguradora';
export const SEGUROS_COLUNA_NUMERO = 'Nº da apólice';
export const SEGUROS_COLUNA_VIGENCIA = 'Vigência';
export const SEGUROS_COLUNA_PREMIO = 'Prêmio';
export const SEGUROS_COLUNA_VENCIMENTO = 'Vencimento';
export const SEGUROS_COLUNA_ACOES = 'Ações';
export const SEGUROS_BOTAO_EDITAR = 'Editar';
export const SEGUROS_LEGENDA_TABELA = 'Apólices de seguro';
export const SEGUROS_FILTRAR_POR_VEICULO = 'Ver só as apólices deste veículo';

export const SEGUROS_VAZIO_TITULO = 'Nenhuma apólice cadastrada';
export const SEGUROS_VAZIO_TEXTO = 'Cadastre a primeira apólice da frota.';
export const SEGUROS_VAZIO_A_VENCER_TITULO = 'Nenhuma apólice vence nos próximos 60 dias';
export const SEGUROS_VAZIO_VEICULO_TITULO = 'Nenhuma apólice para este veículo';
export const SEGUROS_VAZIO_FILTRO_TEXTO = 'Ajuste os filtros para ver outras apólices.';
export const SEGUROS_VER_TODAS = 'Ver todas';
export const SEGUROS_ERRO_LISTA = 'Não foi possível carregar as apólices.';

export const SEGUROS_GAVETA_NOVA = 'Nova apólice';
export const SEGUROS_GAVETA_EDITAR = 'Editar apólice';
export const SEGUROS_CARREGANDO_APOLICE = 'Carregando a apólice…';
export const SEGUROS_ERRO_CONSULTA = 'Não foi possível abrir a apólice.';
export const SEGUROS_ROTULO_VEICULO = 'Veículo';
export const SEGUROS_ROTULO_SEGURADORA = 'Seguradora';
export const SEGUROS_EXEMPLO_SEGURADORA = 'Ex.: Porto Seguro';
export const SEGUROS_ROTULO_NUMERO = 'Número da apólice';
export const SEGUROS_ROTULO_INICIO = 'Início da vigência';
export const SEGUROS_ROTULO_FIM = 'Fim da vigência';
export const SEGUROS_ROTULO_PREMIO = 'Valor do prêmio (R$)';
export const SEGUROS_ROTULO_COBERTURA = 'Cobertura';
export const SEGUROS_EXEMPLO_COBERTURA = 'Ex.: Compreensiva, terceiros R$ 100 mil';
export const SEGUROS_TROCAR_VEICULO = 'Trocar veículo';
export const SEGUROS_BOTAO_CANCELAR = 'Cancelar';
export const SEGUROS_BOTAO_SALVAR = 'Salvar apólice';
export const SEGUROS_SUCESSO_CADASTRO = 'Apólice cadastrada.';
export const SEGUROS_SUCESSO_EDICAO = 'Apólice atualizada.';

export const SEGUROS_ERRO_VEICULO = 'Escolha o veículo da apólice.';
export const SEGUROS_ERRO_SEGURADORA = 'Informe a seguradora.';
export const SEGUROS_ERRO_SEGURADORA_TAMANHO = 'A seguradora deve ter no máximo 100 caracteres.';
export const SEGUROS_ERRO_NUMERO = 'Informe o número da apólice.';
export const SEGUROS_ERRO_NUMERO_TAMANHO = 'O número da apólice deve ter no máximo 30 caracteres.';
export const SEGUROS_ERRO_INICIO = 'Informe a data de início da vigência.';
export const SEGUROS_ERRO_FIM = 'Informe a data de fim da vigência.';
export const SEGUROS_ERRO_VIGENCIA = 'A data de fim da vigência deve ser posterior à data de início.';
export const SEGUROS_ERRO_PREMIO = 'O valor do prêmio deve ser maior que zero.';
export const SEGUROS_ERRO_PREMIO_PRECISAO = 'O valor do prêmio deve ter no máximo 8 dígitos inteiros e 2 casas decimais.';
export const SEGUROS_ERRO_COBERTURA_TAMANHO = 'A cobertura deve ter no máximo 200 caracteres.';

export const SEGUROS_VENCE_HOJE = 'Vence hoje';

/** Pílula com a contagem: "Todas (37)". Sem a contagem (carregando ou falhou), só o rótulo. */
export function textoPilula(rotulo: string, quantidade: number | undefined): string {
  return quantidade === undefined ? rotulo : `${rotulo} (${quantidade.toLocaleString('pt-BR')})`;
}

/** Selo do título da lista: "1 apólice" / "N apólices". */
export function textoQuantidadeApolices(quantidade: number): string {
  return quantidade === 1 ? '1 apólice' : `${quantidade.toLocaleString('pt-BR')} apólices`;
}

/** "Falta 1 dia" / "Faltam N dias" (embaixo da vigência). */
export function textoFaltam(dias: number): string {
  return dias === 1 ? 'Falta 1 dia' : `Faltam ${dias} dias`;
}

/** "Vence em breve (N dias)" (status das que vencem em até 60 dias). */
export function textoVenceEmBreve(dias: number): string {
  return dias === 1 ? 'Vence em breve (1 dia)' : `Vence em breve (${dias} dias)`;
}

/** "Vencida há 1 dia" / "Vencida há N dias". */
export function textoVencidaHa(dias: number): string {
  return dias === 1 ? 'Vencida há 1 dia' : `Vencida há ${dias} dias`;
}

