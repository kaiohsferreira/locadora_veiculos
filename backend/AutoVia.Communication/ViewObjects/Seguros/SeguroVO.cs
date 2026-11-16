namespace AutoVia.Communication.ViewObjects.Seguros
{
    /// <summary>
    /// VO de entrada (POST e PUT) da apolice de seguro. Estrutura plana: o veiculo vem
    /// so pelo id, sem id da apolice e sem campos de auditoria. A validacao e feita
    /// somente pelo SeguroVOValidator.
    /// </summary>
    public class SeguroVO
    {
        /// <summary>Id do veiculo segurado (obrigatorio, maior que zero).</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Nome da seguradora (obrigatorio, ate 100 caracteres).</summary>
        public string Seguradora { get; set; } = string.Empty;

        /// <summary>Numero da apolice (obrigatorio, ate 30 caracteres, unico entre as ativas).</summary>
        public string NumeroApolice { get; set; } = string.Empty;

        /// <summary>Inicio da vigencia (yyyy-MM-dd).</summary>
        public DateOnly DataInicio { get; set; }

        /// <summary>Fim da vigencia (yyyy-MM-dd); deve ser posterior ao inicio.</summary>
        public DateOnly DataFim { get; set; }

        /// <summary>Valor do premio, em reais; maior que zero.</summary>
        public decimal ValorPremio { get; set; }

        /// <summary>Descricao opcional da cobertura (ate 200 caracteres).</summary>
        public string? Cobertura { get; set; }
    }
}
