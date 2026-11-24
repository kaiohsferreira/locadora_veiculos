using AutoVia.Api.Extensions;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Teto de aberturas com caucao por funcionario (decisao 5 de 24/11/2026; contrato caucao-retirada v1.1): 5 por minuto por
    /// identidade, outra identidade nao e afetada, e a leitura do numero nunca lanca (texto nao inteiro, zero, negativo, vazio ou
    /// ausente caem no padrao de 5), para o teto nao derrubar as rotas de locacao.
    /// </summary>
    public class TetoCaucaoPorFuncionarioTests
    {
        private const string Funcionario20 = "funcionario:20";
        private const string Funcionario21 = "funcionario:21";

        private static IConfiguration Configuracao(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valor != null)
            {
                valores.Add(ConstantsValues.ChaveLimiteCaucaoPorFuncionario, valor);
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        [Fact]
        public void CincoEntram_ASextaNoMesmoMinutoNao_EOutraIdentidadeNaoEAfetada()
        {
            using TetoCaucaoPorFuncionario teto = new TetoCaucaoPorFuncionario(Configuracao(null));

            for (int tentativa = 0; tentativa < ConstantsValues.LimiteCaucaoPorFuncionarioMinutoPadrao; tentativa++)
            {
                Assert.True(teto.TentarEntrar(Funcionario20));
            }

            Assert.False(teto.TentarEntrar(Funcionario20));
            Assert.True(teto.TentarEntrar(Funcionario21));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void LimiteZeroOuNegativoNoConstrutor_CaiNoPadrao(int limite)
        {
            using TetoCaucaoPorFuncionario teto = new TetoCaucaoPorFuncionario(limite, TimeSpan.FromMinutes(1));

            for (int tentativa = 0; tentativa < ConstantsValues.LimiteCaucaoPorFuncionarioMinutoPadrao; tentativa++)
            {
                Assert.True(teto.TentarEntrar(Funcionario20));
            }

            Assert.False(teto.TentarEntrar(Funcionario20));
        }

        [Theory]
        [InlineData("5.0")]
        [InlineData("cinco")]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData(null)]
        public void LerLimite_ValorQueNaoServe_DevolveOPadraoSemLancar(string? valor)
        {
            int limite = TetoCaucaoPorFuncionario.LerLimite(Configuracao(valor));

            Assert.Equal(ConstantsValues.LimiteCaucaoPorFuncionarioMinutoPadrao, limite);
        }

        [Fact]
        public void LerLimite_InteiroPositivo_Vale()
        {
            int limite = TetoCaucaoPorFuncionario.LerLimite(Configuracao("7"));

            Assert.Equal(7, limite);
        }

        [Fact]
        public void Configurado_ComSete_ASetimaEntraEAOitavaNao()
        {
            using TetoCaucaoPorFuncionario teto = new TetoCaucaoPorFuncionario(Configuracao("7"));

            for (int tentativa = 0; tentativa < 7; tentativa++)
            {
                Assert.True(teto.TentarEntrar(Funcionario20));
            }

            Assert.False(teto.TentarEntrar(Funcionario20));
        }
    }
}
