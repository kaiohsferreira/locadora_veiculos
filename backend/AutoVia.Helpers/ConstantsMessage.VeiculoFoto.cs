namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de fotos do veiculo. As seis primeiras chegam ao usuario (400, 404 e 409) e a de processamento ocupado (429); as demais so vao para o log.
    /// ErroVeiculoFotoLimiteAtingido tem {0}, preenchido com ConstantsValues.MaximoFotosPorVeiculo (CultureInfo.InvariantCulture).
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Mensagem do 400 quando o campo foto vem vazio ou ausente.</summary>
        public const string ErroVeiculoFotoObrigatoria = "Envie uma foto do veículo em JPG ou PNG.";

        /// <summary>Mensagem do 400 quando o arquivo nao e JPEG ou PNG valido.</summary>
        public const string ErroVeiculoFotoFormatoInvalido = "A foto deve ser um arquivo JPG ou PNG válido.";

        /// <summary>Mensagem do 400 quando o arquivo passa de 10 MiB.</summary>
        public const string ErroVeiculoFotoTamanho = "A foto deve ter no máximo 10 MB.";

        /// <summary>Mensagem do 400 quando a imagem passa dos limites de lado ou de pixels totais.</summary>
        public const string ErroVeiculoFotoDimensoes = "A foto é grande demais. Envie uma imagem com até 8192 pixels de lado e até 16 megapixels.";

        /// <summary>Mensagem do 409 quando o veiculo ja tem o maximo de fotos ativas; {0} = ConstantsValues.MaximoFotosPorVeiculo.</summary>
        public const string ErroVeiculoFotoLimiteAtingido = "Este veículo já tem o máximo de {0} fotos. Remova uma foto para enviar outra.";

        /// <summary>Mensagem do 404 quando a foto nao existe, ja foi removida ou e de outro veiculo.</summary>
        public const string ErroVeiculoFotoNaoEncontrada = "A foto informada não foi encontrada.";

        /// <summary>Mensagem do 429 quando a fila de processamento de fotos esta cheia ou a espera pela vaga estoura o tempo limite.</summary>
        public const string ErroVeiculoFotoProcessamentoOcupado = "Há muitas fotos em processamento no momento. Tente novamente em instantes.";

        /// <summary>Mensagem do log ao falhar o envio de foto do veiculo (servico).</summary>
        public const string ErroEnviarFotoVeiculo = "Não foi possível enviar a foto do veículo.";

        /// <summary>Mensagem do log ao falhar a listagem das fotos do veiculo (servico).</summary>
        public const string ErroListarFotosVeiculo = "Não foi possível listar as fotos do veículo.";

        /// <summary>Mensagem do log ao falhar a remocao da foto do veiculo (servico).</summary>
        public const string ErroRemoverFotoVeiculo = "Não foi possível remover a foto do veículo.";

        /// <summary>Mensagem do log ao falhar uma consulta de fotos do veiculo (repositorio).</summary>
        public const string ErroConsultarFotoVeiculo = "Não foi possível consultar as fotos do veículo.";

        /// <summary>Mensagem do log ao falhar uma gravacao de foto do veiculo (repositorio).</summary>
        public const string ErroGravarFotoVeiculo = "Não foi possível gravar a foto do veículo.";

        /// <summary>Mensagem do log ao falhar o processamento da imagem (processador).</summary>
        public const string ErroProcessarFotoVeiculo = "Não foi possível processar a foto do veículo.";

        /// <summary>Mensagem do log ao falhar a montagem da foto de capa nas listas (servico de capa).</summary>
        public const string ErroMontarCapaVeiculo = "Não foi possível montar a foto de capa do veículo.";
    }
}
