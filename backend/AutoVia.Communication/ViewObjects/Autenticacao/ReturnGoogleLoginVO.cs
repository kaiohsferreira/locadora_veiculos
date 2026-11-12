using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de saida do login com Google (POST /api/auth/google/login). A situacao diz o que o front faz.
    /// Sessao so vem em AUTENTICADO. Nome e e-mail sao os da propria conta Google (CONFIRMACAO_NECESSARIA
    /// e CADASTRO_NECESSARIO); nenhum dado de cadastro da AutoVia sai nas situacoes sem sessao.
    /// </summary>
    public class ReturnGoogleLoginVO
    {
        /// <summary>Situacao do login: AUTENTICADO, CONFIRMACAO_NECESSARIA, CADASTRO_NECESSARIO ou CONTA_AMBIGUA.</summary>
        public SituacaoLoginGoogle Situacao { get; set; }

        /// <summary>Sessao aberta; preenchida somente quando a situacao e AUTENTICADO.</summary>
        public ReturnLoginVO? Sessao { get; set; }

        /// <summary>Nome exibido pelo Google; so em CONFIRMACAO_NECESSARIA e CADASTRO_NECESSARIO.</summary>
        public string? Nome { get; set; }

        /// <summary>E-mail verificado pelo Google; so em CONFIRMACAO_NECESSARIA e CADASTRO_NECESSARIO.</summary>
        public string? Email { get; set; }
    }
}
