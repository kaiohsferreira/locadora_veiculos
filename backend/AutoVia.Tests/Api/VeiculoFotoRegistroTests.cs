using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Registro de injecao das fotos do veiculo: o processador de imagem e Singleton (o limite de processamentos e a fila de
    /// espera sao do processo inteiro); o servico e o repositorio sao Scoped (um por requisicao).
    /// </summary>
    public sealed class VeiculoFotoRegistroTests
    {
        [Fact]
        public void ProcessadorDeFoto_EhSingletonEntreEscopos()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();

            using IServiceScope primeiroEscopo = fabrica.Services.CreateScope();
            using IServiceScope segundoEscopo = fabrica.Services.CreateScope();
            IProcessadorFotoService primeiro = primeiroEscopo.ServiceProvider.GetRequiredService<IProcessadorFotoService>();
            IProcessadorFotoService segundo = segundoEscopo.ServiceProvider.GetRequiredService<IProcessadorFotoService>();

            Assert.Same(primeiro, segundo);
        }

        [Fact]
        public void ServicoEScopoDeFotos_SaoResolvidosPorEscopo()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();

            using IServiceScope primeiroEscopo = fabrica.Services.CreateScope();
            using IServiceScope segundoEscopo = fabrica.Services.CreateScope();
            IVeiculoFotoService servicoPrimeiro = primeiroEscopo.ServiceProvider.GetRequiredService<IVeiculoFotoService>();
            IVeiculoFotoService servicoSegundo = segundoEscopo.ServiceProvider.GetRequiredService<IVeiculoFotoService>();
            IVeiculoFotoRepository repositorio = primeiroEscopo.ServiceProvider.GetRequiredService<IVeiculoFotoRepository>();

            Assert.NotSame(servicoPrimeiro, servicoSegundo);
            Assert.NotNull(repositorio);
        }
    }
}
