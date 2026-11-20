namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03). Arquivo parcial da ConstantsValues: intervalo de
    /// revisao, margem de aviso, teto da lista de alertas, tamanhos e precisao dos campos da ordem e os nomes das situacoes.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Intervalo fixo entre revisoes, em km (decisao do Kaio de 19/11/2026).</summary>
        public const int IntervaloRevisaoKm = 10000;

        /// <summary>Faltando ate este tanto de km para a proxima revisao, o veiculo entra como PROXIMA (decisao do Kaio de 19/11/2026).</summary>
        public const int MargemRevisaoProximaKm = 1500;

        /// <summary>Teto de itens na lista de alertas de revisao.</summary>
        public const int TetoAlertasRevisao = 100;

        /// <summary>Oficina da ordem de manutencao (VARCHAR(100)), opcional.</summary>
        public const int TamanhoMaximoOficinaManutencao = 100;

        /// <summary>Descricao da ordem de manutencao (VARCHAR(200)), opcional.</summary>
        public const int TamanhoMaximoDescricaoManutencao = 200;

        /// <summary>Precisao total do custo da ordem (NUMERIC(10,2)).</summary>
        public const int PrecisaoCustoManutencao = 10;

        /// <summary>Casas decimais do custo da ordem (NUMERIC(10,2)).</summary>
        public const int EscalaCustoManutencao = 2;

        /// <summary>Base do percentual do ciclo de revisao (cem por cento).</summary>
        public const int PercentualBaseRevisao = 100;

        /// <summary>Casas decimais do percentual do ciclo de revisao.</summary>
        public const int CasasPercentualCicloRevisao = 1;

        /// <summary>Situacao de revisao: faltam zero km ou menos.</summary>
        public const string SituacaoRevisaoVencida = "VENCIDA";

        /// <summary>Situacao de revisao: faltam ate a margem de aviso.</summary>
        public const string SituacaoRevisaoProxima = "PROXIMA";

        /// <summary>Situacao de revisao: longe da proxima revisao (nao entra na lista de alertas).</summary>
        public const string SituacaoRevisaoEmDia = "EM_DIA";

        /// <summary>Nome do campo da data de saida na recusa de validacao (400) da ordem de manutencao.</summary>
        public const string CampoDataSaidaManutencao = "DataSaida";

        /// <summary>Nome do campo da quilometragem na recusa de validacao (400) do encerramento da ordem de manutencao.</summary>
        public const string CampoKmAtualManutencao = "KmAtual";

        /// <summary>Tamanho maximo do texto de busca da lista de manutencao, depois do Trim (igual ao da busca de reservas).</summary>
        public const int TamanhoMaximoBuscaManutencao = 60;

        /// <summary>Valor do filtro de situacao da lista para ordens abertas (sem data de saida).</summary>
        public const string SituacaoManutencaoAberta = "ABERTA";

        /// <summary>Valor do filtro de situacao da lista para ordens encerradas (com data de saida).</summary>
        public const string SituacaoManutencaoEncerrada = "ENCERRADA";

        /// <summary>Prefixo do codigo que a tela mostra para a ordem ("OS #id"); aceito na busca, sem diferenciar maiusculas.</summary>
        public const string PrefixoCodigoOsManutencao = "OS";

        /// <summary>Sinal que pode vir antes do codigo da ordem na busca ("OS #id" ou "#id").</summary>
        public const char SinalCodigoManutencao = '#';
    }
}
