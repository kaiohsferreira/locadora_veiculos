using AutoVia.Helpers;

namespace AutoVia.Core.Options
{
    /// <summary>
    /// Configuracao da recuperacao de senha por link (secao Autenticacao:Recuperacao). Validada na subida
    /// da API (ValidateOnStart): URL publica do front absoluta http/https, validade e limite positivos.
    /// </summary>
    public class RecuperacaoSenhaOptions
    {
        /// <summary>URL publica do front (ex.: http://localhost:5173). O link de redefinicao sai daqui.</summary>
        public string UrlPublicaFront { get; set; } = string.Empty;

        /// <summary>Validade do link, em minutos, contada a partir de DateTime.Now.</summary>
        public int ValidadeMinutos { get; set; } = ConstantsValues.ValidadeMinutosPadrao;

        /// <summary>Quantas solicitacoes por conta na ultima hora. Acima disso nada e enviado (resposta igual).</summary>
        public int LimitePorHora { get; set; } = ConstantsValues.LimitePorHoraPadrao;

        /// <summary>Tempo minimo, em ms, da resposta do pedido. Iguala o tempo de envio e de nao envio.</summary>
        public int TempoMinimoRespostaMs { get; set; } = ConstantsValues.TempoMinimoRespostaMsPadrao;
    }
}
