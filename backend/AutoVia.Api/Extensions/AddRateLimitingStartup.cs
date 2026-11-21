using AutoVia.Api.ActionResults;
using AutoVia.Api.Middleware;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Limite de requisicoes das rotas publicas: janela fixa por IP. Politicas independentes, cada uma
    /// com o seu contador: AutoViaPublico (login, cadastro, recuperacao e Google; um minuto; numero em
    /// Autenticacao:LimitePublicoPorMinuto), AutoViaVerificacaoCadastro (verificacao de cadastro; um minuto;
    /// numero em Clientes:LimiteVerificacaoCadastroPorMinuto), AutoViaCadastroEmpresa (cadastro publico de
    /// locadora; uma hora; numero em Locadoras:LimiteCadastroPorHora, padrao 5) e AutoViaLeituraPublica (busca de
    /// disponiveis e catalogo de seminovos; um minuto; numero em Veiculos:LimiteLeituraPublicaPorMinuto, padrao 60) e
    /// AutoViaEnvioFotoVeiculo (envio de foto do veiculo, rota autenticada; um minuto; numero em Veiculos:LimiteEnvioFotoPorMinuto,
    /// padrao 60) e AutoViaLeituraArquivo (leitura publica de arquivo do armazenamento; um minuto; numero em
    /// Armazenamento:LimiteLeituraPorMinuto, padrao 600; o teto da rota fica em TetoLeituraArquivo, depois deste limite) e
    /// AutoViaGeracaoPix (geracao de PIX de cobranca, nas rotas da locadora e do cliente; um minuto; por IP; numero em
    /// Pagamentos:LimiteGeracaoPixPorMinuto, padrao 10; o teto por cliente, TetoGeracaoPixPorCliente, e chamado pela acao
    /// depois da autenticacao e vale junto com este). Um valor menor ou igual a
    /// zero cai no padrao. O IP vem do cabecalho configurado em Rede:CabecalhoIpCliente (padrao X-Real-IP,
    /// um salto), lido pelo UseForwardedHeaders montado no Program; vazio desliga a leitura e vale o IP do socket.
    /// Um valor que nao e nome de cabecalho HTTP valido impede a subida da API, e com FORWARDEDHEADERS_ENABLED
    /// verdadeira a API tambem nao sobe (o host anularia o controle).
    /// Risco residual: sem lista de proxies, quem alcanca a API sem passar pela borda escolhe o proprio
    /// cabecalho. Por isso existem os tetos sem IP (LimiteExcedidoException). O comportamento real da borda do
    /// Railway so e confirmado pelo teste no ambiente publicado.
    /// </summary>
    public static class AddRateLimitingStartup
    {
        /// <summary>Registra as politicas de limite das rotas publicas (429 em problem+json). Os cabecalhos encaminhados ficam em CriarCabecalhosEncaminhados, chamado pelo Program.</summary>
        public static IServiceCollection AddRateLimitingPublico(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                // Lidos quando o pipeline e montado (host ja com toda a configuracao aplicada).
                int limitePublico = LerLimite(configuration, ConstantsValues.ChaveLimitePublico, ConstantsValues.LimitePublicoPorMinutoPadrao);
                int limiteVerificacao = LerLimite(configuration, ConstantsValues.ChaveLimiteVerificacaoCadastro, ConstantsValues.LimiteVerificacaoCadastroPorMinutoPadrao);
                int limiteCadastroEmpresa = LerLimite(configuration, ConstantsValues.ChaveLimiteCadastroPorHora, ConstantsValues.LimiteCadastroEmpresaPorHoraPadrao);
                int limiteLeituraPublica = LerLimite(configuration, ConstantsValues.ChaveLimiteLeituraPublica, ConstantsValues.LimiteLeituraPublicaPorMinutoPadrao);
                int limiteEnvioFoto = LerLimite(configuration, ConstantsValues.ChaveLimiteEnvioFotoVeiculo, ConstantsValues.LimiteEnvioFotoVeiculoPorMinutoPadrao);
                int limiteLeituraArquivo = LerLimite(configuration, ConstantsValues.ChaveLimiteLeituraArquivo, ConstantsValues.LimiteLeituraArquivoPorMinutoPadrao);
                int limiteEnvioDocumento = LerLimite(configuration, ConstantsValues.ChaveLimiteEnvioDocumentoCliente, ConstantsValues.LimiteEnvioDocumentoClientePorMinutoPadrao);
                int limiteListaDocumento = LerLimite(configuration, ConstantsValues.ChaveLimiteListaDocumentoCliente, ConstantsValues.LimiteListaDocumentoClientePorMinutoPadrao);
                int limiteListaDocumentoEquipe = LerLimite(configuration, ConstantsValues.ChaveLimiteListaDocumentoEquipe, ConstantsValues.LimiteListaDocumentoEquipePorMinutoPadrao);
                int limiteGeracaoPix = LerLimite(configuration, ConstantsValues.ChaveLimiteGeracaoPix, ConstantsValues.LimiteGeracaoPixPorMinutoPadrao);

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimitePublico, limitePublico, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteVerificacaoCadastro, limiteVerificacao, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteCadastroEmpresa, limiteCadastroEmpresa, TimeSpan.FromHours(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteLeituraPublica, limiteLeituraPublica, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteEnvioFotoVeiculo, limiteEnvioFoto, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteLeituraArquivo, limiteLeituraArquivo, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteEnvioDocumentoCliente, limiteEnvioDocumento, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteListaDocumentoCliente, limiteListaDocumento, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteListaDocumentoEquipe, limiteListaDocumentoEquipe, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteGeracaoPix, limiteGeracaoPix, TimeSpan.FromMinutes(1));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (EhPoliticaDaLeituraDeArquivo(context.HttpContext))
                    {
                        // Registro agregado do 429 desta rota (no maximo um por minuto). As outras politicas continuam sem registro.
                        RegistroRecusasLimiteLeitura? registro = context.HttpContext.RequestServices.GetService<RegistroRecusasLimiteLeitura>();
                        registro?.RegistrarPorIp();
                        // Referrer-Policy tambem no 429 da rota de leitura (o 429 nao passa pela acao).
                        CabecalhoReferrerLeituraArquivo.Registrar(context.HttpContext.Response);
                    }

                    await ProblemDetailsWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status429TooManyRequests,
                        ConstantsMessage.TituloLimiteRequisicoes,
                        ConstantsMessage.ErroLimiteRequisicoes);
                };
            });

            // Teto da rota de leitura de arquivo (contador sem IP, consultado pela acao) e registro agregado do seu 429.
            services.AddSingleton<TetoLeituraArquivo>();
            services.AddSingleton<TetoGeracaoPixPorCliente>();
            services.AddSingleton<RegistroRecusasLimiteLeitura>();
            services.TryAddSingleton(TimeProvider.System);

            return services;
        }

        /// <summary>
        /// Guarda do host (logo apos o Build): com FORWARDEDHEADERS_ENABLED verdadeira (bool.TryParse, sem diferenciar
        /// maiusculas) a API nao sobe, porque o host inseriria um UseForwardedHeaders proprio, com opcoes padrao, antes do nosso.
        /// Ausente ou falsa: sobe.
        /// </summary>
        public static void ValidarHostSemCabecalhosAutomaticos(IConfiguration configuration)
        {
            string? valor = configuration.GetValue<string>(ConstantsValues.ChaveForwardedHeadersHabilitado);
            bool habilitado = bool.TryParse(valor?.Trim(), out bool resultado) && resultado;

            if (habilitado)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroHostComCabecalhosAutomaticos);
            }
        }

        /// <summary>
        /// Aviso na subida (log de informacao pelo ILogService) quando a leitura do cabecalho de IP esta desligada (chave
        /// vazia ou so com espacos). A mensagem nao leva o valor de configuracao.
        /// </summary>
        public static void RegistrarAvisoCabecalhoDesligado(IConfiguration configuration, ILogService logService)
        {
            string? valor = configuration.GetValue<string>(ConstantsValues.ChaveCabecalhoIpCliente, ConstantsValues.CabecalhoIpClientePadrao);

            if (string.IsNullOrWhiteSpace(valor))
            {
                logService.WriteInfo(ConstantsMessage.AvisoCabecalhoIpDesligado, nameof(AddRateLimitingStartup));
            }
        }

        /// <summary>
        /// Monta as opcoes do UseForwardedHeaders a partir da configuracao final. O Program chama no proprio fluxo de subida,
        /// logo apos o Build e antes de atender requisicoes: um valor invalido lanca InvalidOperationException e a API nao sobe.
        /// Nao e feito dentro de lambda de options (que rodaria so na primeira resolucao).
        /// </summary>
        public static ForwardedHeadersOptions CriarCabecalhosEncaminhados(IConfiguration configuration)
        {
            string? cabecalhoIpCliente = ValidarCabecalhoIpCliente(
                configuration.GetValue<string>(ConstantsValues.ChaveCabecalhoIpCliente, ConstantsValues.CabecalhoIpClientePadrao));

            ForwardedHeadersOptions opcoes = new ForwardedHeadersOptions();
            AplicarCabecalhosEncaminhados(opcoes, cabecalhoIpCliente);

            return opcoes;
        }

        /// <summary>
        /// Valida o valor de Rede:CabecalhoIpCliente. Vazio ou so com espacos devolve nulo (nenhum cabecalho de IP e lido).
        /// Valor fora do token do RFC 9110 (espaco, acento, caractere de controle...) lanca InvalidOperationException, com
        /// a mensagem de configuracao, sem repetir o valor informado.
        /// </summary>
        public static string? ValidarCabecalhoIpCliente(string? valor)
        {
            string aparado = valor?.Trim() ?? string.Empty;

            if (aparado.Length == 0)
            {
                return null;
            }

            if (!EhNomeDeCabecalhoHttp(aparado))
            {
                throw new InvalidOperationException(ConstantsMessage.ErroConfiguracaoCabecalhoIpCliente);
            }

            return aparado;
        }

        /// <summary>
        /// Aplica o cabecalho de IP as opcoes do UseForwardedHeaders: sem cabecalho, so X-Forwarded-Proto e lido; com
        /// cabecalho, X-Forwarded-For e lido no nome informado. Um salto (ForwardLimit = 1); listas de proxies vazias.
        /// </summary>
        public static void AplicarCabecalhosEncaminhados(ForwardedHeadersOptions options, string? cabecalhoIpCliente)
        {
            // O proxy do Railway nao tem IP fixo, por isso a lista de proxies confiaveis fica vazia.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = ConstantsValues.LimiteSaltosEncaminhados;

            if (cabecalhoIpCliente == null)
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                return;
            }

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardedForHeaderName = cabecalhoIpCliente;
        }

        /// <summary>Indica se o texto e um nome de cabecalho HTTP valido: letras, digitos e os caracteres do token do RFC 9110.</summary>
        private static bool EhNomeDeCabecalhoHttp(string nome)
        {
            foreach (char caractere in nome)
            {
                bool letraOuDigito = (caractere >= 'a' && caractere <= 'z')
                    || (caractere >= 'A' && caractere <= 'Z')
                    || (caractere >= '0' && caractere <= '9');

                if (!letraOuDigito && ConstantsValues.CaracteresTokenCabecalho.IndexOf(caractere) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Indica se o endpoint da requisicao tem a politica da leitura de arquivo (so ela registra o 429 agregado).</summary>
        private static bool EhPoliticaDaLeituraDeArquivo(HttpContext httpContext)
        {
            EnableRateLimitingAttribute? atributo = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

            return atributo?.PolicyName == ConstantsValues.PoliticaLimiteLeituraArquivo;
        }

        /// <summary>Le o limite da chave informada; zero ou negativo cai no padrao informado. Interno: o teto da leitura usa a mesma regra.</summary>
        internal static int LerLimite(IConfiguration configuration, string chave, int padrao)
        {
            int limite = configuration.GetValue<int>(chave, padrao);

            if (limite <= 0)
            {
                limite = padrao;
            }

            return limite;
        }

        /// <summary>Registra uma politica de janela fixa (a janela informada), com contador separado por IP (desconhecido quando nao ha IP).</summary>
        private static void RegistrarPoliticaPorIp(RateLimiterOptions options, string nomePolitica, int limite, TimeSpan janela)
        {
            options.AddPolicy<string>(nomePolitica, httpContext =>
            {
                // Unico ponto de particao: a chave passa sempre por ChaveIpLimite (IPv6 por /64, IPv4 mapeado vira IPv4).
                string chaveIp = ChaveIpLimite.Normalizar(httpContext.Connection.RemoteIpAddress);

                return RateLimitPartition.GetFixedWindowLimiter<string>(chaveIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = janela,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        }
    }
}
