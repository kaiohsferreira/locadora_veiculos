namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// VO de entrada da abertura de ordem de manutencao (POST /api/manutencoes). Estrutura plana: o veiculo vem pelo id e o
    /// tipo pelo nome. A quilometragem nao vem do cliente: e a do veiculo no momento da abertura. A validacao e feita
    /// somente pelo AbrirManutencaoVOValidator.
    /// </summary>
    public class AbrirManutencaoVO
    {
        /// <summary>Id do veiculo (obrigatorio, maior que zero).</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Nome do tipo: PREVENTIVA ou CORRETIVA (obrigatorio). Numero e minusculas sao recusados.</summary>
        public string? Tipo { get; set; }

        /// <summary>Oficina onde o veiculo esta (opcional, ate 100 caracteres).</summary>
        public string? Oficina { get; set; }

        /// <summary>Descricao do servico (opcional, ate 200 caracteres).</summary>
        public string? Descricao { get; set; }

        /// <summary>Data de entrada (yyyy-MM-dd). Ausente = hoje; data futura e recusada pelo servico.</summary>
        public DateOnly? DataEntrada { get; set; }
    }
}
