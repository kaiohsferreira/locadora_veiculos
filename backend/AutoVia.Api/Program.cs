using AutoVia.Api.Extensions;
using AutoVia.Api.Middleware;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Extensions;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json (fora do git) sobrescreve o appsettings.json versionado
// com a connection string de cada maquina. Variavel de ambiente continua vencendo.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Cada grupo de registro mora no seu arquivo Extensions/Add<Assunto>Startup.cs.
builder.Services.AddCustomResponse();
builder.Services.AddValidation();
builder.Services.AddDatabase();
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddMapping();
builder.Services.AddIntegrations();
builder.Services.AddAuthenticationJwt(builder.Configuration);
builder.Services.AddRecuperacaoSenha(builder.Configuration, builder.Environment);
builder.Services.AddLocadoras(builder.Configuration);
builder.Services.AddClientes(builder.Configuration);
builder.Services.AddLoginGoogle();
builder.Services.AddRateLimitingPublico(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

WebApplication app = builder.Build();

// Guarda do host: FORWARDEDHEADERS_ENABLED verdadeira faria o host anular o controle do IP do limite; a API nao sobe.
AddRateLimitingStartup.ValidarHostSemCabecalhosAutomaticos(app.Configuration);

// Sem banco configurado a subida falha aqui, com a mensagem do resolvedor: a conexao so e lida ao criar o contexto.
app.Configuration.ResolveDatabaseConnection();

// Aviso na subida quando a leitura do IP do cliente esta desligada (Rede:CabecalhoIpCliente vazia).
AddRateLimitingStartup.RegistrarAvisoCabecalhoDesligado(app.Configuration, app.Services.GetRequiredService<ILogService>());

// Primeiro do pipeline: le o IP real do cliente (cabecalho Rede:CabecalhoIpCliente, padrao X-Real-IP) para o limite
// de requisicoes. As opcoes saem da configuracao final: valor invalido impede a subida da API aqui mesmo.
app.UseForwardedHeaders(AddRateLimitingStartup.CriarCabecalhosEncaminhados(app.Configuration));

// Qualquer excecao abaixo vira ProblemDetails.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "AutoVia API");
}

app.UseCors(ConstantsValues.PoliticaCors);

// Limite das rotas publicas (login e cadastro de cliente), antes da autenticacao.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposto para testes de integracao (WebApplicationFactory).</summary>
public partial class Program
{
}
