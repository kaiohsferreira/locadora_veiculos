using AutoVia.Helpers;
using AutoVia.Infrastructure.Repositories;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Guarda do INSERT do PENDENTE (T-BE-06, C1): com transacao aberta o SaveChanges nao confirma e o 409 desfaria o
    /// PENDENTE. Limite declarado: o AutoViaDbContext real nao abre transacao sem conexao com o banco, e aqui nao ha banco.
    /// Por isso a condicao e testada na menor costura possivel (ValidarSemTransacaoAberta), recebendo o mesmo bool que o
    /// InsertAsync le de Database.CurrentTransaction. O caminho completo com transacao real fica para teste de integracao.
    /// </summary>
    public class GuardasDoVinculoTests
    {
        [Fact]
        public void ValidarSemTransacaoAberta_ComTransacaoAberta_RecusaComMensagemDeConstante()
        {
            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => ClienteLocadoraRepository.ValidarSemTransacaoAberta(true));

            Assert.Equal(ConstantsMessage.ErroVinculoDentroDeTransacao, excecao.Message);
        }

        [Fact]
        public void ValidarSemTransacaoAberta_SemTransacao_NaoLanca()
        {
            ClienteLocadoraRepository.ValidarSemTransacaoAberta(false);
        }
    }
}
