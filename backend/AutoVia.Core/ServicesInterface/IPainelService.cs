using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Painel;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Visao geral da locadora (P11-BE-01 e P11-BE-02): indicadores, situacao da frota e primeiro acesso, da locadora do
    /// funcionario autenticado. O corte dos valores em reais e feito aqui, por IContextoEmpresa.PodeVerValores.
    /// </summary>
    public interface IPainelService
    {
        /// <summary>Monta a visao geral no periodo pedido, com a data de hoje do servidor.</summary>
        Task<ReturnVisaoGeralVO> ObterVisaoGeralAsync(PeriodoVisaoGeral periodo);
    }
}
