using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Fila em memoria dos pedidos de recuperacao (RecuperacaoSenhaFila): capacidade fixa, Enfileirar que nunca lanca,
    /// pedido nulo registrado sem dado pessoal, ordem de chegada e o objeto que nao expoe o CPF em texto.
    /// </summary>
    public class RecuperacaoSenhaFilaTests
    {
        private const string CpfDeTeste = "52998224725";
        private const string CpfPrimeiro = "11144477735";
        private const string CpfSegundo = "39053344705";

        private static PedidoRecuperacaoSenha Pedido(string cpf)
        {
            return new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, cpf);
        }

        [Fact]
        public void Enfileirar_AceitaAteACapacidadeEDevolveFalseNaSeguinte()
        {
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            for (int indice = 0; indice < ConstantsValues.CapacidadeFilaPedidosRecuperacao; indice++)
            {
                Assert.True(fila.Enfileirar(Pedido(CpfDeTeste)));
            }

            Assert.False(fila.Enfileirar(Pedido(CpfDeTeste)));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(CpfDeTeste, StringComparison.Ordinal));
        }

        [Fact]
        public void Enfileirar_PedidoNulo_DevolveFalseERegistraSemDadoPessoal()
        {
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            bool enfileirado = fila.Enfileirar(null!);

            Assert.False(enfileirado);
            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroPedidoRecuperacaoInvalido));
        }

        [Fact]
        public void Leitor_EntregaOsPedidosNaOrdemDeChegada()
        {
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(new FakeLogService());
            fila.Enfileirar(Pedido(CpfPrimeiro));
            fila.Enfileirar(Pedido(CpfSegundo));

            Assert.True(fila.Leitor.TryRead(out PedidoRecuperacaoSenha? primeiro));
            Assert.True(fila.Leitor.TryRead(out PedidoRecuperacaoSenha? segundo));

            Assert.Equal(CpfPrimeiro, primeiro?.Cpf);
            Assert.Equal(CpfSegundo, segundo?.Cpf);
        }

        [Fact]
        public void PedidoRecuperacaoSenha_ToStringNaoExpoeOCpf()
        {
            PedidoRecuperacaoSenha pedido = Pedido(CpfDeTeste);

            Assert.DoesNotContain(CpfDeTeste, pedido.ToString());
            Assert.Equal(ConstantsValues.TextoPedidoRecuperacaoOculto, pedido.ToString());
        }
    }
}
