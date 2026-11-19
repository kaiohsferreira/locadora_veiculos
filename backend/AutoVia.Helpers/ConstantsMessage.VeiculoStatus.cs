namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da troca manual de status do veiculo (P1-BE-04, RF-04). Arquivo parcial: as mensagens da listagem
    /// ficam em ConstantsMessage.VeiculoListagem.cs e as do cadastro em ConstantsMessage.Veiculo.cs.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Veiculo: troca manual de status (P1-BE-04) ---
        public const string ErroVeiculoStatusObrigatorio = "Informe o status do veículo.";
        public const string ErroVeiculoStatusSomentePorFluxo = "Este status não pode ser definido manualmente: ele muda pela reserva, pela locação ou pela venda do veículo.";
        public const string ErroVeiculoStatusComReservaEmAberto = "O veículo tem reserva em aberto. O status só pode ser alterado depois que a reserva for cancelada ou concluída.";
        public const string ErroVeiculoStatusComLocacaoEmAberto = "O veículo tem locação em andamento. O status só pode ser alterado depois da devolução.";
        public const string ErroVeiculoStatusVendidoOuBaixado = "Veículo vendido ou baixado não pode ter o status alterado.";
        public const string ErroVeiculoStatusOrigemPorFluxo = "O status atual do veículo só muda pela reserva ou pela locação.";
        public const string ErroAlterarStatusVeiculo = "Não foi possível alterar o status do veículo.";
        public const string InfoVeiculoStatusAlterado = "Status do veículo {0} alterado de {1} para {2} pelo funcionário {3}.";
    }
}
