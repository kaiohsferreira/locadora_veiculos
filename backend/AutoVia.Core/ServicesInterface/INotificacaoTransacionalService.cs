using System;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Notificações internas sobre operações de negócio já persistidas.</summary>
    public interface INotificacaoTransacionalService
    {
        Task ConfirmarReservaAsync(ReservaCriadaEmail evento);

        Task EmitirReciboDevolucaoAsync(LocacaoFinalizadaEmail evento);

        Task AvisarCobrancaAsync(CobrancaLancadaEmail evento);
    }

    public sealed record ReservaCriadaEmail(
        int IdReserva,
        int IdCliente,
        int IdEmpresa,
        DateOnly RetiradaPrevista,
        DateOnly DevolucaoPrevista,
        decimal? ValorEstimado);

    /// <summary>O caso de uso obtém o e-mail pela FK do titular da locação persistida.</summary>
    public sealed record LocacaoFinalizadaEmail(
        int IdLocacao,
        string DestinatarioEmail,
        DateTime DevolucaoEfetiva,
        decimal ValorFinal);

    /// <summary>O caso de uso obtém o e-mail pela FK do titular da origem persistida.</summary>
    public sealed record CobrancaLancadaEmail(
        int IdPagamento,
        int? IdLocacao,
        int? IdVenda,
        string DestinatarioEmail,
        decimal Valor,
        DateOnly DataVencimento);
}
