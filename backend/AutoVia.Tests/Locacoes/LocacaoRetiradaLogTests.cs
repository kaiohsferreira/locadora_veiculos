using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Log do 404 da retirada: servico e controller deixam registro, como nos outros metodos de locacao.</summary>
    public class LocacaoRetiradaLogTests
    {
        private static readonly DateTime AgoraDoRelogio = new DateTime(2026, 11, 19, 9, 30, 0);

        [Fact]
        public async Task RetiradaDeReservaInexistente_ServicoRegistraAoMenosUmLogDeErro()
        {
            FakeLogService log = new FakeLogService();
            LocacaoService servico = CriarServico(new FakeLocacaoRepository(), log);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetRetiradaAsync(99));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.NotEmpty(log.Entries);
        }

        [Fact]
        public async Task RetiradaDeReservaInexistente_ControllerRegistraAoMenosUmLogDeErro()
        {
            FakeLogService log = new FakeLogService();
            LocacaoController controller = new LocacaoController(new ServicoDeRetiradaNaoEncontrada(), log);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetRetirada(99));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.NotEmpty(log.Entries);
        }

        private static LocacaoService CriarServico(FakeLocacaoRepository repositorio, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeRelogio relogio = new FakeRelogio(AgoraDoRelogio);

            return new LocacaoService(repositorio, relogio, mapper, log, ContextoEmpresaDeTeste.Criar());
        }

        /// <summary>Servico que sempre responde 404 na retirada; os outros metodos nao sao usados neste teste.</summary>
        private sealed class ServicoDeRetiradaNaoEncontrada : ILocacaoService
        {
            public Task<ReturnLocacaoVO> StartFromReservaAsync(LocacaoVO locacaoVO, int idFuncionario)
            {
                throw new NotSupportedException();
            }

            public Task<ReturnLocacaoVO> GetByIdAsync(int idLocacao)
            {
                throw new NotSupportedException();
            }

            public Task<ReturnRetiradaVO> GetRetiradaAsync(int idReserva)
            {
                throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
            }
        }
    }
}
