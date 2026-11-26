using AutoVia.Communication.ViewObjects.Devolucao;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Encerramento da locacao na devolucao (P5-BE-04, RF-25 e RP-07/RP-12) e lista das cobrancas extras da locacao.</summary>
    public interface IDevolucaoService
    {
        /// <summary>Encerra a locacao: confere, decide a caucao, apura, grava na transacao final e devolve a resposta.</summary>
        Task<ReturnEncerramentoLocacaoVO> EncerrarAsync(int idLocacao, EncerrarLocacaoVO model);

        /// <summary>Lista as cobrancas extras ativas da locacao, sem recalcular e sem provedor.</summary>
        Task<ReturnCobrancasLocacaoVO> ListarCobrancasAsync(int idLocacao);
    }
}
