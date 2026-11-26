namespace AutoVia.Communication.ViewObjects.Contratos
{
    /// <summary>
    /// Documento do contrato (a copia que o snapshot guarda em documento_json). Datas e valores vem como texto pronto
    /// (sufixo Str), formatados por StaticMethods; CPF e CNH vem mascarados. Nao ha IP nem dado de cartao.
    /// </summary>
    public sealed class ContratoDocumentoVO
    {
        public int VersaoDocumento { get; set; }

        public ContratoLocadoraVO Locadora { get; set; } = new ContratoLocadoraVO();

        public ContratoLocatarioVO Locatario { get; set; } = new ContratoLocatarioVO();

        public ContratoVeiculoVO Veiculo { get; set; } = new ContratoVeiculoVO();

        public ContratoLocacaoDocumentoVO Locacao { get; set; } = new ContratoLocacaoDocumentoVO();

        public ContratoVistoriaEntradaVO VistoriaEntrada { get; set; } = new ContratoVistoriaEntradaVO();

        /// <summary>Tabela de cobrancas vigente da categoria; nula quando nao ha configuracao vigente (RN-12).</summary>
        public ContratoTabelaCobrancasVO? TabelaCobrancas { get; set; }

        /// <summary>Clausulas com lastro, numeradas em sequencia (RN-6 e RN-12).</summary>
        public List<ContratoClausulaVO> Clausulas { get; set; } = new List<ContratoClausulaVO>();
    }

    /// <summary>Dados da locadora (razao social, contato e endereco), da tabela empresa.</summary>
    public sealed class ContratoLocadoraVO
    {
        public string RazaoSocial { get; set; } = string.Empty;

        public string NomeFantasia { get; set; } = string.Empty;

        public string Cnpj { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Cep { get; set; } = string.Empty;

        public string Logradouro { get; set; } = string.Empty;

        public string Numero { get; set; } = string.Empty;

        public string? Complemento { get; set; }

        public string Bairro { get; set; } = string.Empty;

        public string Cidade { get; set; } = string.Empty;

        public string Uf { get; set; } = string.Empty;
    }

    /// <summary>Dados do locatario, com CPF e CNH mascarados (StaticMethods.MascararCpf e MascararCnh).</summary>
    public sealed class ContratoLocatarioVO
    {
        public string Nome { get; set; } = string.Empty;

        public string CpfMascarado { get; set; } = string.Empty;

        public string CnhMascarada { get; set; } = string.Empty;

        /// <summary>Categoria da CNH como texto.</summary>
        public string CnhCategoria { get; set; } = string.Empty;

        public string CnhValidadeStr { get; set; } = string.Empty;
    }

    /// <summary>Dados do veiculo locado.</summary>
    public sealed class ContratoVeiculoVO
    {
        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string Chassi { get; set; } = string.Empty;

        public string Renavam { get; set; } = string.Empty;

        public int AnoFabricacao { get; set; }

        public int AnoModelo { get; set; }

        public string? Cor { get; set; }
    }

    /// <summary>Dados da locacao: retirada, devolucao prevista, diaria, caucao e o previsto (O-12).</summary>
    public sealed class ContratoLocacaoDocumentoVO
    {
        /// <summary>Numero da locacao (id).</summary>
        public int Numero { get; set; }

        public string DataRetiradaStr { get; set; } = string.Empty;

        public string DataDevolucaoPrevistaStr { get; set; } = string.Empty;

        public string ValorDiariaStr { get; set; } = string.Empty;

        /// <summary>Caucao formatada; nula quando a caucao e zero (RN-6).</summary>
        public string? ValorCaucaoStr { get; set; }

        /// <summary>Diarias previstas entre a retirada e a devolucao prevista (RegrasDevolucao.CalcularDiarias).</summary>
        public int DiariasPrevistas { get; set; }

        /// <summary>Total previsto so de diarias (RegrasDevolucao.ValorDaDiaria), formatado; sem extras, caucao ou taxas.</summary>
        public string ValorTotalPrevistoStr { get; set; } = string.Empty;
    }

    /// <summary>Vistoria de entrada que sustenta o contrato (RP-06). O nivel de combustivel vai como numero de 0 a 1.</summary>
    public sealed class ContratoVistoriaEntradaVO
    {
        public string DataHoraStr { get; set; } = string.Empty;

        public int KmRegistrado { get; set; }

        /// <summary>Nivel de 0 a 1 com 2 casas (excecao registrada a regra camposStr; a tela formata).</summary>
        public decimal? NivelCombustivel { get; set; }
    }

    /// <summary>Tabela de cobrancas extras da categoria (configuracao vigente, RN-12).</summary>
    public sealed class ContratoTabelaCobrancasVO
    {
        public int VersaoConfiguracao { get; set; }

        public int FranquiaKm { get; set; }

        public string BaseFranquia { get; set; } = string.Empty;

        public string ValorKmExcedenteStr { get; set; } = string.Empty;

        public string ValorTanqueCompletoStr { get; set; } = string.Empty;

        public int ToleranciaAtrasoMinutos { get; set; }

        public string UnidadeAtraso { get; set; } = string.Empty;

        public string FracionamentoAtraso { get; set; } = string.Empty;

        public string ValorUnidadeAtrasoStr { get; set; } = string.Empty;

        public string BaseAvarias { get; set; } = string.Empty;
    }

    /// <summary>Uma clausula com lastro, ja numerada.</summary>
    public sealed class ContratoClausulaVO
    {
        public int Numero { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Texto { get; set; } = string.Empty;
    }
}
