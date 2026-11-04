using AutoVia.Core.ServicesInterface;
using Microsoft.Extensions.Logging;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Implementacao do ILogService sobre o log padrao do ASP.NET Core
    /// (configurado na secao "Logging" do appsettings.json). Nao grava em tabela:
    /// a especificacao do banco nao preve tabela de log.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
        }

        public void Write(string message, string friendlyMessage, string source)
        {
            try
            {
                _logger.LogError("[{Source}] {FriendlyMessage} | {Message}", source, friendlyMessage, message);
            }
            catch (Exception)
            {
                // Falha do proprio log nao pode derrubar a requisicao.
            }
        }

        public void Write(Exception exception, string friendlyMessage, string source)
        {
            try
            {
                _logger.LogError(exception, "[{Source}] {FriendlyMessage}", source, friendlyMessage);
            }
            catch (Exception)
            {
                // Falha do proprio log nao pode derrubar a requisicao.
            }
        }

        public void WriteInfo(string message, string source)
        {
            try
            {
                _logger.LogInformation("[{Source}] {Message}", source, message);
            }
            catch (Exception)
            {
                // Falha do proprio log nao pode derrubar a requisicao.
            }
        }
    }
}
