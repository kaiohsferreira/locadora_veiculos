using AutoVia.Api.Hosting;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Api.Extensions
{
    public static class AddServicesStartup
    {
        /// <summary>
        /// Registro dos servicos de aplicacao (Scoped, salvo indicacao).
        /// Cada modulo acrescenta aqui: services.AddScoped&lt;IVeiculoService, VeiculoService&gt;();
        /// </summary>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Singleton: sem estado, usado tambem pelo gateway HTTP (singleton).
            services.AddSingleton<ILogService, LogService>();

            // --- Modulos (Prompt 3) ---
            services.AddScoped<ICategoriaService, CategoriaService>();

            // --- Cliente ---
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IClienteLocadoraService, ClienteLocadoraService>();

            // --- Locadora (cadastro publico, P10-BE-01) ---
            services.AddScoped<IEmpresaService, EmpresaService>();

            // --- Equipe da locadora (P10-BE-03) ---
            services.AddScoped<IEquipeService, EquipeService>();

            // --- Veiculo (SEAM: base comum das tarefas P1-BE-02 e P1-BE-03) ---
            services.AddScoped<IVeiculoService, VeiculoService>();

            // --- Painel da locadora (visao geral, P11-BE-01 e P11-BE-02) ---
            services.AddScoped<IPainelService, PainelService>();
            // Singleton: relogio sem estado (DateTime.Now); os testes trocam por um relogio fixo.
            services.AddSingleton<IRelogio, RelogioSistema>();

            // --- Contexto de empresa (multi-locadora, T-BE-03b) ---
            // Scoped: uma empresa e um cargo por requisicao, preenchidos na validacao do token com dados do banco.
            services.AddScoped<IContextoEmpresa, ContextoEmpresa>();

            // --- Fotos do veiculo ---
            // Scoped: o servico usa o contexto da sessao. O processador e Singleton: o limite de processamentos simultaneos
            // e a fila de espera valem para o processo inteiro.
            services.AddScoped<IVeiculoFotoService, VeiculoFotoService>();
            services.AddSingleton<IProcessadorFotoService, ProcessadorFotoService>();
            // Scoped: a capa das listas (frota e reservas) usa o gateway de armazenamento da requisicao.
            services.AddScoped<IFotoCapaVeiculoService, FotoCapaVeiculoService>();

            // --- Reserva ---
            services.AddScoped<IReservaService, ReservaService>();

            // --- Proposta de compra (P6-BE-02) ---
            services.AddScoped<IPropostaCompraService, PropostaCompraService>();

            // --- Painel operacional ---
            services.AddScoped<IPainelOperacionalService, PainelOperacionalService>();

            // --- Locacao ---
            services.AddScoped<ILocacaoService, LocacaoService>();
            services.AddScoped<IVistoriaService, VistoriaService>();
            services.AddScoped<IAvariaService, AvariaService>();

            // --- Autenticacao ---
            // Singleton: sem estado (hash, emissao de token); as opcoes sao registradas em AddAuthenticationJwt.
            services.AddSingleton<ISenhaHasher, SenhaHasher>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddScoped<IAutenticacaoService, AutenticacaoService>();
            // Scoped: usa o repositorio de login; o cache do resultado e o IMemoryCache (AddIntegrations).
            services.AddScoped<ISessaoService, SessaoService>();
            services.AddScoped<IAdminInicialService, AdminInicialService>();

            // Garante o administrador inicial na subida (idempotente; falha so vai para o log).
            services.AddHostedService<AdminInicialHostedService>();

            // --- Seguros (P9-BE-01) ---
            services.AddScoped<ISeguroService, SeguroService>();

            services.AddScoped<IVistoriaSaidaService, VistoriaSaidaService>();
            services.AddScoped<ICobrancasExtrasService, CobrancasExtrasService>();

            services.AddScoped<IConfiguracoesCobrancasExtrasService, ConfiguracoesCobrancasExtrasService>();
            services.AddScoped<IApuracaoCobrancasExtrasService, ApuracaoCobrancasExtrasService>();
            return services;
        }
    }
}
