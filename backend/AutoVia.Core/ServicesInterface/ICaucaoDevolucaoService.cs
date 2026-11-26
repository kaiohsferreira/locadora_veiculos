using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Decisao da caucao no encerramento (caucao-devolucao v4). Consulta e age no provedor FORA de transacao e de trava; devolve
    /// o que a transacao final deve gravar. Nao grava nada.
    /// </summary>
    public interface ICaucaoDevolucaoService
    {
        /// <summary>Decide a caucao da locacao: consulta o provedor, age quando a tabela manda e devolve a decisao com a gravacao.</summary>
        Task<DecisaoCaucaoEncerramento> DecidirAsync(Locacao locacao, Pagamento? linhaCaucao, decimal totalExtras);

        /// <summary>
        /// Acao e valores da caucao para a resposta quando a locacao ja esta encerrada (leitura do banco, sem provedor). Nulo quando
        /// nao ha caucao.
        /// </summary>
        AcaoCaucaoDevolucao AcaoDoBanco(Pagamento? linhaCaucao, decimal valorReservado);
    }

    /// <summary>Resultado da decisao da caucao, pronto para o repositorio e para a resposta.</summary>
    public sealed class DecisaoCaucaoEncerramento
    {
        /// <summary>Acao da tabela C1 a C14 (caucao-devolucao v4).</summary>
        public AcaoCaucaoDevolucao Acao { get; set; }

        /// <summary>Novo status da caucao; nulo quando a linha fica como esta (PENDENTE, tratar a mao).</summary>
        public StatusPagamento? NovoStatusCaucao { get; set; }

        /// <summary>Valor gravado na caucao quando ha regravacao.</summary>
        public decimal? ValorCaucaoGravado { get; set; }

        /// <summary>Grava a data de hoje na caucao (captura ou retencao).</summary>
        public bool DataHojeNaCaucao { get; set; }

        /// <summary>Verdadeiro quando a retencao quita a COBRANCA_EXTRA alvo.</summary>
        public bool QuitarAlvo { get; set; }

        /// <summary>Verdadeiro quando a caucao nao foi confirmada no provedor (tratar a mao).</summary>
        public bool TratarAMao { get; set; }

        /// <summary>Valor retido da caucao (zero quando baixada ou nao confirmada).</summary>
        public decimal ValorRetido { get; set; }

        /// <summary>Valor liberado da caucao (zero quando nao confirmada).</summary>
        public decimal ValorLiberado { get; set; }

        /// <summary>Texto de aviso de ConstantsMessage, quando a tabela manda um; nulo caso contrario.</summary>
        public string? MensagemAviso { get; set; }
    }
}
