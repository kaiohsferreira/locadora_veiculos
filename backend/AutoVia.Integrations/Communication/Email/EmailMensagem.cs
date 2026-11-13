namespace AutoVia.Integrations.Communication.Email
{
    /// <summary>
    /// Mensagem de e-mail transacional, chegando PRONTA: assunto, texto e HTML sao montados por quem
    /// chama (ex.: RecuperacaoSenhaService). O gateway so valida e entrega.
    /// Etiqueta serve so para o log (ex.: recuperacao-senha). Nunca logar destinatario, assunto ou corpo.
    /// </summary>
    public class EmailMensagem
    {
        /// <summary>E-mail do destinatario (obrigatorio, valido, ate 254 caracteres).</summary>
        public string DestinatarioEmail { get; set; } = string.Empty;

        /// <summary>Nome do destinatario (opcional, ate 120 caracteres).</summary>
        public string? DestinatarioNome { get; set; }

        /// <summary>Assunto (1 a 200 caracteres, sem quebra de linha).</summary>
        public string Assunto { get; set; } = string.Empty;

        /// <summary>Corpo em texto puro (obrigatorio).</summary>
        public string CorpoTexto { get; set; } = string.Empty;

        /// <summary>Corpo em HTML, ja escapado por quem monta (opcional).</summary>
        public string? CorpoHtml { get; set; }

        /// <summary>Etiqueta de origem, so para log (ex.: recuperacao-senha).</summary>
        public string? Etiqueta { get; set; }
    }
}
