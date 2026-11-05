namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de entrada (POST e PUT) da categoria de veiculos. Estrutura plana, sem id
    /// e sem campos de auditoria. A validacao e feita somente pelo CategoriaVOValidator.
    /// </summary>
    public class CategoriaVO
    {
        /// <summary>Nome da categoria (obrigatorio, sem so espacos, ate 50 caracteres).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Descricao opcional da categoria (ate 200 caracteres).</summary>
        public string? Descricao { get; set; }

        /// <summary>Valor da diaria base, em reais; deve ser maior que zero.</summary>
        public decimal ValorDiariaBase { get; set; }
    }
}
