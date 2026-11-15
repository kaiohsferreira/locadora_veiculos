using AutoVia.Api.Extensions;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Ligacao da configuracao do teto (secao Clientes) com ClientesOptions pelo registro real (AddClientes): com a chave
    /// informada o limite e o valor configurado; sem a chave, o padrao de 5.
    /// </summary>
    public class ClientesOptionsConfigTests
    {
        private static ClientesOptions ResolverOpcoes(Dictionary<string, string?> valores)
        {
            IConfiguration configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();

            ServiceCollection services = new ServiceCollection();
            services.AddClientes(configuracao);

            using ServiceProvider provedor = services.BuildServiceProvider();

            return provedor.GetRequiredService<ClientesOptions>();
        }

        [Fact]
        public void ChaveConfigurada_DefineOLimiteDoTeto()
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>
            {
                { ConstantsValues.ChaveLimitePedidosAprovacaoPorDia, "3" }
            };

            ClientesOptions opcoes = ResolverOpcoes(valores);

            Assert.Equal(3, opcoes.LimitePedidosAprovacaoPorDia);
        }

        [Fact]
        public void SemAChave_ValeOPadraoDeCinco()
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            ClientesOptions opcoes = ResolverOpcoes(valores);

            Assert.Equal(5, opcoes.LimitePedidosAprovacaoPorDia);
            Assert.Equal(ConstantsValues.LimitePedidosAprovacaoPorDiaPadrao, opcoes.LimitePedidosAprovacaoPorDia);
        }
    }
}
