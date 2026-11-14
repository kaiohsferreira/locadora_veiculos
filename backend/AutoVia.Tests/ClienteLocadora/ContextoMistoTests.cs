using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Contexto misto (T-BE-06, C2): o ContextoEmpresa nunca tem locadora e cliente ao mesmo tempo. Definir recusa quando
    /// ja ha cliente; DefinirCliente recusa quando ja ha locadora. Sem essa regra, o filtro "Empresa" (OR) daria visao
    /// cruzada entre locadoras. Os dois sentidos sao testados, e o contexto continua valido depois da recusa.
    /// </summary>
    public class ContextoMistoTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdClienteDoTeste = 5;
        private const int IdFuncionarioDoTeste = 9;

        private static ContextoEmpresa CriarContexto()
        {
            return new ContextoEmpresa(new FakeLogService());
        }

        [Fact]
        public void DefinirCliente_ComLocadoraJaDefinida_RecusaComMensagemDeConstante()
        {
            ContextoEmpresa contexto = CriarContexto();
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => contexto.DefinirCliente(IdClienteDoTeste));

            Assert.Equal(ConstantsMessage.ErroContextoMisto, excecao.Message);
            Assert.Equal(0, contexto.IdCliente);
            Assert.Equal(EmpresaDoTeste, contexto.IdEmpresa);
        }

        [Fact]
        public void Definir_ComClienteJaDefinido_RecusaComMensagemDeConstante()
        {
            ContextoEmpresa contexto = CriarContexto();
            contexto.DefinirCliente(IdClienteDoTeste);

            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false));

            Assert.Equal(ConstantsMessage.ErroContextoMisto, excecao.Message);
            Assert.Equal(0, contexto.IdEmpresa);
            Assert.Equal(IdClienteDoTeste, contexto.IdCliente);
        }

        [Fact]
        public void SoEmpresa_OuSoCliente_SaoAceitos()
        {
            ContextoEmpresa funcionario = CriarContexto();
            funcionario.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            ContextoEmpresa cliente = CriarContexto();
            cliente.DefinirCliente(IdClienteDoTeste);

            Assert.Equal(EmpresaDoTeste, funcionario.IdEmpresa);
            Assert.Equal(0, funcionario.IdCliente);
            Assert.Equal(IdClienteDoTeste, cliente.IdCliente);
            Assert.Equal(0, cliente.IdEmpresa);
        }
    }
}
