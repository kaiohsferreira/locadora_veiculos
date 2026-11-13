using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace AutoVia.Integrations.Services.Email
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede: grava a mensagem em App_Data/email-fake (pasta do
    /// conteudo da API, criada se nao existir), com nome de arquivo so de data/hora e identificador aleatorio,
    /// e registra no EmailFakeStore.
    ///
    /// TRAVA: so grava com IHostEnvironment.IsDevelopment() E ambiente da integracao SANDBOX. Fora disso
    /// recusa com IntegrationException. (O seed põe tudo em SANDBOX, por isso a checagem de Development e obrigatoria.)
    /// </summary>
    public class FakeEmailProvider : IEmailProvider
    {
        private readonly EmailFakeStore _store;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogService _logService;

        public FakeEmailProvider(EmailFakeStore store, IHostEnvironment hostEnvironment, ILogService logService)
        {
            _store = store;
            _hostEnvironment = hostEnvironment;
            _logService = logService;
        }

        public string Nome => ConstantsEmail.ProvedorFake;

        /// <summary>Grava a mensagem na caixa de teste e no registro em memoria. Recusa fora de Development + SANDBOX.</summary>
        public async Task EnviarAsync(ConfiguracaoIntegracao configuracao, EmailMensagem mensagem, string remetenteEmail, string remetenteNome, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarTrava(configuracao);

                string identificador = GerarIdentificador();
                string pasta = Path.Combine(_hostEnvironment.ContentRootPath, ConstantsEmail.PastaAppData, ConstantsEmail.PastaEmailFake);
                string caminho = Path.Combine(pasta, identificador + ConstantsEmail.ExtensaoArquivoFake);

                Directory.CreateDirectory(pasta);
                await File.WriteAllTextAsync(caminho, MontarConteudo(mensagem, remetenteEmail, remetenteNome), Encoding.UTF8, cancellationToken);

                _store.Registrar(new EmailFakeEnviado(
                    identificador,
                    remetenteEmail,
                    remetenteNome,
                    mensagem.DestinatarioEmail,
                    mensagem.DestinatarioNome,
                    mensagem.Assunto,
                    mensagem.CorpoTexto,
                    mensagem.CorpoHtml,
                    mensagem.Etiqueta));
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha da caixa de teste.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailGravarCaixaFake, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroEmailGravarCaixaFake, ex);
            }
        }

        /// <summary>Trava: Development e SANDBOX ao mesmo tempo, senao recusa.</summary>
        private void ValidarTrava(ConfiguracaoIntegracao configuracao)
        {
            if (!_hostEnvironment.IsDevelopment() || configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsEmail.ChaveProvedor, ConstantsMessage.ErroEmailFakeForaDoDesenvolvimento);
            }
        }

        /// <summary>Data/hora (yyyyMMddHHmmssfff) + identificador aleatorio. Nao carrega dado pessoal.</summary>
        private static string GerarIdentificador()
        {
            return DateTime.Now.ToString(ConstantsEmail.FormatoIdentificadorData) + ConstantsEmail.SeparadorIdentificador + Guid.NewGuid().ToString("N");
        }

        /// <summary>Conteudo do arquivo da caixa de teste: cabecalhos, texto e HTML.</summary>
        private static string MontarConteudo(EmailMensagem mensagem, string remetenteEmail, string remetenteNome)
        {
            StringBuilder conteudo = new StringBuilder();

            conteudo.AppendLine(ConstantsEmail.RotuloDe + remetenteNome + ConstantsEmail.AberturaEndereco + remetenteEmail + ConstantsEmail.FechamentoEndereco);
            conteudo.AppendLine(ConstantsEmail.RotuloPara + (mensagem.DestinatarioNome ?? string.Empty) + ConstantsEmail.AberturaEndereco + mensagem.DestinatarioEmail + ConstantsEmail.FechamentoEndereco);
            conteudo.AppendLine(ConstantsEmail.RotuloAssunto + mensagem.Assunto);
            conteudo.AppendLine();
            conteudo.AppendLine(ConstantsEmail.RotuloTexto);
            conteudo.AppendLine(mensagem.CorpoTexto);
            conteudo.AppendLine();
            conteudo.AppendLine(ConstantsEmail.RotuloHtml);
            conteudo.AppendLine(mensagem.CorpoHtml ?? string.Empty);

            return conteudo.ToString();
        }
    }
}
