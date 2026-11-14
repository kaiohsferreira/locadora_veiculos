using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de entrada da verificacao do cadastro do cliente pela locadora (PATCH /api/clientes/{id}/verificacao).
    /// Estrutura plana, sem id de locadora: a locadora vem sempre do contexto do funcionario. A validacao e feita pelo
    /// VerificacaoCadastroVOValidator (decisao obrigatoria, justificativa obrigatoria na recusa e teto de tamanho).
    /// </summary>
    public class VerificacaoCadastroVO
    {
        /// <summary>Decisao da locadora: APROVADO ou RECUSADO (obrigatoria; PENDENTE nao e decisao).</summary>
        public StatusCadastroCliente? Decisao { get; set; }

        /// <summary>Motivo interno da recusa (ate 500 caracteres; obrigatorio quando RECUSADO). Ignorado na aprovacao e nunca sai para o cliente.</summary>
        public string? Justificativa { get; set; }
    }
}
