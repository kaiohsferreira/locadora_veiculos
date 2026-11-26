using AutoVia.Communication.ViewObjects.Contratos;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato digital da locacao (RF-21; RP-06): documento montado na hora enquanto nao assinado, assinatura com copia guardada
    /// no banco e leitura da copia depois. Nao trava locacao, retirada nem devolucao (D10).
    /// </summary>
    public interface IContratoLocacaoService
    {
        /// <summary>Contrato da locacao: a copia guardada quando assinado; o documento montado quando pendente.</summary>
        Task<ReturnContratoLocacaoVO> ObterAsync(int idLocacao);

        /// <summary>Coleta a assinatura: grava o documento, o hash SHA-256, a imagem e o funcionario, sob a trava da locacao.</summary>
        Task<ReturnContratoLocacaoVO> AssinarAsync(int idLocacao, int idFuncionario, AssinarContratoLocacaoVO model);
    }
}
