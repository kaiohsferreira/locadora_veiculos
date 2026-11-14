using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ISessaoService de teste, sem banco: por padrao toda sessao e valida e o funcionario pertence a
    /// empresa 1 com cargo ATENDENTE (empresa ativa, nao operadora), para os testes HTTP que nao tratam
    /// revogacao. Invalidar so conta as chamadas.
    /// </summary>
    public class FakeSessaoService : ISessaoService
    {
        /// <summary>Resultado devolvido por SessaoValidaAsync (padrao: sessao valida).</summary>
        public bool Valida { get; set; } = true;

        /// <summary>Situacao devolvida por ObterSituacaoAsync (padrao: funcionario ativo na empresa 1, cargo ATENDENTE).</summary>
        public SessaoSituacaoVO Situacao { get; set; } = new SessaoSituacaoVO
        {
            Ativo = true,
            IdEmpresa = 1,
            Cargo = CargoFuncionario.ATENDENTE,
            EmpresaAtiva = true,
            EmpresaOperadora = false
        };

        /// <summary>Quantas vezes Invalidar foi chamado.</summary>
        public int Invalidacoes { get; private set; }

        /// <summary>Ids dos funcionarios em cada chamada de Invalidar, na ordem (para o teste conferir o alvo).</summary>
        public List<int> IdsInvalidados { get; } = new List<int>();

        public Task<bool> SessaoValidaAsync(PerfilUsuario perfil, int idUsuario, string? versaoSessao)
        {
            return Task.FromResult(Valida);
        }

        public Task<SessaoSituacaoVO> ObterSituacaoAsync(PerfilUsuario perfil, int idUsuario)
        {
            return Task.FromResult(Situacao);
        }

        public void Invalidar(PerfilUsuario perfil, int idUsuario)
        {
            Invalidacoes++;
            IdsInvalidados.Add(idUsuario);
        }
    }
}
