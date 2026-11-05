using AutoVia.Api.Extensions;
using AutoVia.Api.Middleware;
using AutoVia.Helpers;
using AutoVia.Integrations.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json (fora do git) sobrescreve o appsettings.json versionado
// com a connection string de cada maquina. Variavel de ambiente continua vencendo.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Cada grupo de registro mora no seu arquivo Extensions/Add<Assunto>Startup.cs.
builder.Services.AddCustomResponse();
builder.Services.AddValidation();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddMapping();
builder.Services.AddIntegrations();
builder.Services.AddAuthenticationJwt(builder.Configuration);
builder.Services.AddRateLimitingPublico(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

WebApplication app = builder.Build();

// Primeiro do pipeline: le o IP real do cliente (X-Forwarded-For do proxy) para o limite de requisicoes.
app.UseForwardedHeaders();

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
