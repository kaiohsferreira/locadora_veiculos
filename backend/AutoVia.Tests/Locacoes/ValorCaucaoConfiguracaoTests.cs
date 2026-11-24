using AutoVia.Api.Extensions;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Tests.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Valor da caucao na configuracao do servidor (Pagamentos:ValorCaucao; contrato caucao-retirada v1.1, decisao 2 e CS-3):
    /// a validacao da subida, a fabrica do singleton CaucaoRetiradaOptions (valor invalido lanca, nunca vira 0) e a API de
    /// verdade, que nao sobe com valor invalido (molde Rede:CabecalhoIpCliente). A mensagem nunca repete o valor informado.
    /// </summary>
    public class ValorCaucaoConfiguracaoTests
    {
        /// <summary>Os invalidos do contrato: virgula, separador de milhar, sinal, mais de 2 casas, acima do maximo, expoente, espaco no meio e ponto sem casas.</summary>
        public static TheoryData<string> ValoresInvalidos => new TheoryData<string>
        {
            "1200,00",
            "1.200,00",
            "-1",
            "1200.005",
            "100000000.00",
            "1e3",
            "+1200",
            "1 200.00",
            "1200.",
            ".50",
            "abc"
        };

        /// <summary>Configuracao em memoria com Pagamentos:ValorCaucao no valor dado; null = chave ausente.</summary>
        private static IConfiguration CriarConfiguracao(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valor != null)
            {
                valores.Add(ConstantsValues.ChaveValorCaucao, valor);
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        /// <summary>Container so com a configuracao e o registro da caucao (AddCaucaoRetirada), para resolver o singleton.</summary>
        private static ServiceProvider CriarProvedor(string? valor)
        {
            ServiceCollection servicos = new ServiceCollection();
            servicos.AddSingleton<IConfiguration>(CriarConfiguracao(valor));
            servicos.AddCaucaoRetirada();

            return servicos.BuildServiceProvider();
        }

        // --- Validacao da subida (ValidarValorCaucao) ---

        [Theory]
        [MemberData(nameof(ValoresInvalidos))]
        public void ValidarValorCaucao_ValorInvalido_LancaComAMensagemFixaDeConfiguracao(string valor)
        {
            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => AddCaucaoRetiradaStartup.ValidarValorCaucao(CriarConfiguracao(valor)));

            Assert.Equal(ConstantsMessage.ErroConfiguracaoValorCaucao, excecao.Message);
        }

        /// <summary>A mensagem e fixa: o valor informado nao volta nela (nem inteiro, nem em parte).</summary>
        [Fact]
        public void ValidarValorCaucao_ValorInvalido_NaoRepeteOValorNaMensagem()
        {
            const string valorComMarca = "1.200,00-SEGREDO-7391";

            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => AddCaucaoRetiradaStartup.ValidarValorCaucao(CriarConfiguracao(valorComMarca)));

            Assert.DoesNotContain(valorComMarca, excecao.Message);
            Assert.DoesNotContain("SEGREDO-7391", excecao.Message);
            Assert.DoesNotContain("1.200,00", excecao.Message);
        }

        [Fact]
        public void ValidarValorCaucao_ComPonto_DevolveOValor()
        {
            decimal valor = AddCaucaoRetiradaStartup.ValidarValorCaucao(CriarConfiguracao("1200.00"));

            Assert.Equal(1200.00m, valor);
        }

        /// <summary>Ausente, vazio e 0: valor 0 (sem caucao), sem erro.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0")]
        public void ValidarValorCaucao_AusenteVazioOuZero_DevolveZero(string? valor)
        {
            decimal lido = AddCaucaoRetiradaStartup.ValidarValorCaucao(CriarConfiguracao(valor));

            Assert.Equal(0m, lido);
        }

        // --- Fabrica do singleton (CS-3: valor invalido lanca, nunca cai em 0) ---

        [Theory]
        [MemberData(nameof(ValoresInvalidos))]
        public void FabricaDoSingleton_ValorInvalido_LancaENuncaDevolveZero(string valor)
        {
            using ServiceProvider provedor = CriarProvedor(valor);

            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => provedor.GetRequiredService<CaucaoRetiradaOptions>());

            Assert.Equal(ConstantsMessage.ErroConfiguracaoValorCaucao, excecao.Message);
        }

        [Fact]
        public void FabricaDoSingleton_ComPonto_CaucaoExigidaComOValor()
        {
            using ServiceProvider provedor = CriarProvedor("1200.00");

            CaucaoRetiradaOptions opcoes = provedor.GetRequiredService<CaucaoRetiradaOptions>();

            Assert.Equal(1200.00m, opcoes.ValorCaucao);
            Assert.True(opcoes.CaucaoExigida);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("0.00")]
        public void FabricaDoSingleton_AusenteOuZero_SemCaucao(string? valor)
        {
            using ServiceProvider provedor = CriarProvedor(valor);

            CaucaoRetiradaOptions opcoes = provedor.GetRequiredService<CaucaoRetiradaOptions>();

            Assert.Equal(0m, opcoes.ValorCaucao);
            Assert.False(opcoes.CaucaoExigida);
        }

        [Fact]
        public void FabricaDoSingleton_DevolveSempreAMesmaInstancia()
        {
            using ServiceProvider provedor = CriarProvedor("1200.00");

            CaucaoRetiradaOptions primeira = provedor.GetRequiredService<CaucaoRetiradaOptions>();
            CaucaoRetiradaOptions segunda = provedor.GetRequiredService<CaucaoRetiradaOptions>();

            Assert.Same(primeira, segunda);
        }

        // --- API de verdade (TestServer): subida ---

        /// <summary>Com virgula a API nao sobe: a subida falha com a mensagem de configuracao, sem o valor informado.</summary>
        [Fact]
        public void Subida_ValorComVirgula_ApiNaoSobe()
        {
            using ValorCaucaoWebFactory fabrica = new ValorCaucaoWebFactory("1200,00");

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());

            Assert.Contains(ConstantsMessage.ErroConfiguracaoValorCaucao, excecao.ToString());
            Assert.DoesNotContain("1200,00", excecao.ToString());
        }

        /// <summary>Com ponto a API sobe, e o valor em uso (singleton criado na subida) e o configurado.</summary>
        [Fact]
        public void Subida_ValorComPonto_ApiSobeComOValorEmUso()
        {
            using ValorCaucaoWebFactory fabrica = new ValorCaucaoWebFactory("1200.00");

            fabrica.CreateClient();
            CaucaoRetiradaOptions opcoes = fabrica.Services.GetRequiredService<CaucaoRetiradaOptions>();

            Assert.Equal(1200.00m, opcoes.ValorCaucao);
            Assert.True(opcoes.CaucaoExigida);
        }

        /// <summary>Com 0 a API sobe sem caucao (a retirada fica como no RF-17).</summary>
        [Fact]
        public void Subida_ValorZero_ApiSobeSemCaucao()
        {
            using ValorCaucaoWebFactory fabrica = new ValorCaucaoWebFactory("0");

            fabrica.CreateClient();
            CaucaoRetiradaOptions opcoes = fabrica.Services.GetRequiredService<CaucaoRetiradaOptions>();

            Assert.Equal(0m, opcoes.ValorCaucao);
            Assert.False(opcoes.CaucaoExigida);
        }
    }

    /// <summary>Servidor de teste com Pagamentos:ValorCaucao no valor informado (configuracao em memoria, que vence as outras fontes).</summary>
    public sealed class ValorCaucaoWebFactory : AutenticacaoWebFactory
    {
        private readonly string _valorCaucao;

        public ValorCaucaoWebFactory(string valorCaucao)
        {
            _valorCaucao = valorCaucao;
        }

        protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
            valores[ConstantsValues.ChaveValorCaucao] = _valorCaucao;
        }
    }
}
