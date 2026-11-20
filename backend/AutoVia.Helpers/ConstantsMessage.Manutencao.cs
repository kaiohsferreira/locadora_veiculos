namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03): abertura e encerramento da ordem e alertas de revisao.
    /// Arquivo parcial da ConstantsMessage. Tambem traz a recusa nova da troca manual de status com ordem aberta.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Manutencao: validacao da abertura (AbrirManutencaoVO) ---
        public const string ErroManutencaoVeiculoObrigatorio = "Informe o veículo.";
        public const string ErroManutencaoTipoObrigatorio = "Informe o tipo da manutenção.";
        public const string ErroManutencaoTipoInvalido = "Tipo de manutenção inválido. Use PREVENTIVA ou CORRETIVA.";
        public const string ErroManutencaoDataEntradaFutura = "A data de entrada não pode ser futura.";
        public const string ErroManutencaoOficinaTamanho = "A oficina deve ter no máximo {0} caracteres.";
        public const string ErroManutencaoDescricaoTamanho = "A descrição deve ter no máximo {0} caracteres.";

        // --- Manutencao: abertura e encerramento (regras do repositorio e do servico) ---
        public const string ErroManutencaoJaAberta = "O veículo já tem uma ordem de manutenção aberta.";
        public const string ErroManutencaoNaoEncontrada = "Ordem de manutenção não encontrada.";
        public const string ErroManutencaoJaEncerrada = "Esta ordem de manutenção já foi encerrada.";
        public const string ErroManutencaoDataSaidaAnterior = "A data de saída não pode ser anterior à data de entrada.";
        public const string ErroManutencaoDataSaidaFutura = "A data de saída não pode ser futura.";
        public const string ErroManutencaoKmObrigatorio = "Informe a quilometragem do veículo na saída.";
        public const string ErroManutencaoKmMenor = "A quilometragem informada não pode ser menor que a atual do veículo ({0} km).";
        public const string ErroManutencaoCustoInvalido = "O custo não pode ser negativo.";

        /// <summary>Recusa da troca manual MANUTENCAO para DISPONIVEL enquanto a ordem de manutencao estiver aberta (decisao 1 de 19/11/2026).</summary>
        public const string ErroVeiculoStatusComOrdemAberta = "O veículo tem ordem de manutenção aberta. Encerre a ordem para liberar o veículo.";

        // --- Manutencao: textos novos, criados pelo BE (aprovacao pendente do Kaio) ---
        public const string ErroManutencaoCustoPrecisao = "O custo deve ter no máximo {0} dígitos inteiros e {1} casas decimais.";
        public const string ErroAbrirManutencao = "Não foi possível abrir a ordem de manutenção.";
        public const string ErroEncerrarManutencao = "Não foi possível encerrar a ordem de manutenção.";
        public const string ErroConsultarManutencao = "Não foi possível consultar a ordem de manutenção.";
        public const string ErroListarAlertasRevisao = "Não foi possível listar os alertas de revisão.";
        public const string InfoManutencaoAberta = "Ordem de manutenção {0} aberta para o veículo {1} pelo funcionário {2}.";
        public const string InfoManutencaoEncerrada = "Ordem de manutenção {0} encerrada para o veículo {1} pelo funcionário {2}.";

        // --- Manutencao: lista e resumo (semana 4; textos aprovados pelo Kaio em 20/11/2026, contrato manutencao v2) ---
        public const string ErroManutencaoBuscaTamanho = "A busca deve ter no máximo {0} caracteres.";
        public const string ErroManutencaoSituacaoInvalida = "Situação inválida. Use ABERTA ou ENCERRADA.";
        public const string ErroManutencaoVeiculoFiltroInvalido = "O veículo do filtro deve ser um número maior que zero.";
        public const string ErroListarManutencoes = "Não foi possível listar as ordens de manutenção.";
        public const string ErroResumoManutencao = "Não foi possível calcular o resumo da manutenção.";
    }
}
