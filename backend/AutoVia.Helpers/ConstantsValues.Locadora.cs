namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do cadastro publico de locadora (multi-locadora, P10-BE-01): tamanhos da tabela empresa,
    /// chave de liga e desliga, politica de limite por IP do cadastro e matricula inicial do responsavel.
    /// </summary>
    public static partial class ConstantsValues
    {
        public const int TamanhoMaximoRazaoSocialLocadora = 150;

        public const int TamanhoMaximoNomeFantasiaLocadora = 100;

        public const int TamanhoMinimoTelefoneLocadora = 10;

        public const int TamanhoMaximoTelefoneLocadora = 11;

        public const int TamanhoMaximoEmailLocadora = 120;

        public const int TamanhoCepLocadora = 8;

        public const int TamanhoMaximoLogradouroLocadora = 120;

        public const int TamanhoMaximoNumeroEnderecoLocadora = 10;

        public const int TamanhoMaximoComplementoLocadora = 60;

        public const int TamanhoMaximoBairroLocadora = 60;

        public const int TamanhoMaximoCidadeLocadora = 60;

        public const int TamanhoUfLocadora = 2;

        public const int TamanhoMaximoResponsavelNome = 120;

        /// <summary>
        /// Teto do corpo do cadastro publico de locadora, em bytes. A soma dos campos no maximo e cerca de 1 KB em texto
        /// puro (ate ~7 KB se todos os caracteres vierem escapados como \uXXXX); 8 KB cobre o pior caso com folga.
        /// </summary>
        public const int TamanhoMaximoCorpoCadastroLocadora = 8192;

        /// <summary>Matricula do primeiro funcionario de cada empresa nova (o responsavel).</summary>
        public const string MatriculaInicialResponsavel = "MAT-0001";

        /// <summary>Politica de limite do cadastro publico de locadora (5 por hora por IP).</summary>
        public const string PoliticaLimiteCadastroEmpresa = "AutoViaCadastroEmpresa";

        /// <summary>Secao do appsettings com o cadastro publico de locadora.</summary>
        public const string SecaoLocadoras = "Locadoras";

        /// <summary>Chave que liga o cadastro publico (padrao false: 404 nos dois endpoints).</summary>
        public const string ChaveCadastroPublicoHabilitado = "Locadoras:CadastroPublicoHabilitado";

        /// <summary>Chave do limite de cadastros por IP por hora.</summary>
        public const string ChaveLimiteCadastroPorHora = "Locadoras:LimiteCadastroPorHora";

        /// <summary>Limite padrao de cadastros por IP por hora, quando a chave nao esta configurada ou nao e positiva.</summary>
        public const int LimiteCadastroEmpresaPorHoraPadrao = 5;
    }
}
