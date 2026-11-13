using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ISessaoService de teste, sem banco: por padrao toda sessao e valida, para os testes HTTP
    /// que nao tratam revogacao. Invalidar so conta as chamadas.
    /// </summary>
    public class FakeSessaoService : ISessaoService
    {
        /// <summary>Resultado devolvido por SessaoValidaAsync (padrao: sessao valida).</summary>
        public bool Valida { get; set; } = true;

        /// <summary>Quantas vezes Invalidar foi chamado.</summary>
        public int Invalidacoes { get; private set; }

        public Task<bool> SessaoValidaAsync(PerfilUsuario perfil, int idUsuario, string? versaoSessao)
        {
            return Task.FromResult(Valida);
        }

        public void Invalidar(PerfilUsuario perfil, int idUsuario)
        {
            Invalidacoes++;
        }
    }
}
