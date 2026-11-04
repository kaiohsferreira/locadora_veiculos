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
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

WebApplication app = builder.Build();

// Primeiro do pipeline: qualquer excecao abaixo vira ProblemDetails.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "AutoVia API");
}

app.UseCors(ConstantsValues.PoliticaCors);

// Autenticacao (JWT, perfis cliente/funcionario) entra na tarefa P2-BE-01.
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposto para testes de integracao (WebApplicationFactory).</summary>
public partial class Program
{
}
