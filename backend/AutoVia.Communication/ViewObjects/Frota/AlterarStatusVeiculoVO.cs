namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de entrada da troca manual de status do veiculo (PATCH /api/veiculos/{id}/status). Estrutura plana, so o nome do
    /// status. O texto e validado por AlterarStatusVeiculoVOValidator (nome exato, depois do Trim).
    /// </summary>
    public class AlterarStatusVeiculoVO
    {
        /// <summary>Nome de StatusVeiculo pedido (obrigatorio). RESERVADO, LOCADO e VENDIDO passam pelo validador e devolvem 409.</summary>
        public string? Status { get; set; }
    }
}
