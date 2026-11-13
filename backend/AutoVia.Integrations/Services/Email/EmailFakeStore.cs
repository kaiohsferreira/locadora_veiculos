using AutoVia.Integrations.Helpers.Email;

namespace AutoVia.Integrations.Services.Email
{
    /// <summary>
    /// Registro em memoria dos e-mails gravados pelo provedor FAKE (singleton). Usado pelos testes para
    /// conferir o envio. Guarda no maximo ConstantsEmail.LimiteRegistrosFake; o mais antigo sai primeiro.
    /// </summary>
    public class EmailFakeStore
    {
        private readonly List<EmailFakeEnviado> _enviados = new List<EmailFakeEnviado>();
        private readonly object _trava = new object();

        /// <summary>Quantidade de e-mails registrados.</summary>
        public int Quantidade
        {
            get
            {
                lock (_trava)
                {
                    return _enviados.Count;
                }
            }
        }

        /// <summary>Registra um e-mail gravado pelo FAKE.</summary>
        public void Registrar(EmailFakeEnviado enviado)
        {
            lock (_trava)
            {
                _enviados.Add(enviado);

                if (_enviados.Count > ConstantsEmail.LimiteRegistrosFake)
                {
                    _enviados.RemoveAt(0);
                }
            }
        }

        /// <summary>Copia da lista de registros, do mais antigo para o mais novo.</summary>
        public List<EmailFakeEnviado> Enviados()
        {
            lock (_trava)
            {
                return new List<EmailFakeEnviado>(_enviados);
            }
        }
    }

    /// <summary>Um e-mail gravado pelo FAKE: identificador do arquivo e os campos da mensagem.</summary>
    public sealed record EmailFakeEnviado(
        string Identificador,
        string RemetenteEmail,
        string RemetenteNome,
        string DestinatarioEmail,
        string? DestinatarioNome,
        string Assunto,
        string CorpoTexto,
        string? CorpoHtml,
        string? Etiqueta);
}
