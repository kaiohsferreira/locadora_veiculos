namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Log padrao do projeto (regra: todo catch de repositorio, servico e
    /// controller registra o erro com _log.Write antes de devolver a mensagem
    /// amigavel).
    ///
    /// Nunca passe valor de chave protegida de integracao para o log.
    /// </summary>
    public interface ILogService
    {
        /// <summary>Registra um erro: mensagem tecnica, mensagem amigavel devolvida ao usuario e origem (this.GetType().ToString()).</summary>
        void Write(string message, string friendlyMessage, string source);

        /// <summary>Registra um erro a partir da excecao, preservando o stack trace.</summary>
        void Write(Exception exception, string friendlyMessage, string source);

        /// <summary>Registra uma informacao operacional (sem dado sensivel).</summary>
        void WriteInfo(string message, string source);
    }
}
