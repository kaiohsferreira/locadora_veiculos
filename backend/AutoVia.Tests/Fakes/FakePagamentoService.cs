using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;

namespace AutoVia.Tests.Fakes;

/// <summary>
/// IPagamentoService de mentira para os testes da apuracao: guarda cada chamada de LancarAsync e os lancamentos aceitos,
/// e pode falhar como um erro de banco. Os demais metodos nao sao usados pela apuracao.
/// </summary>
public sealed class FakePagamentoService : IPagamentoService
{
    private int _proximoId = 1;

    /// <summary>Quantas vezes LancarAsync foi chamado (inclusive as que falharam).</summary>
    public int Chamadas { get; private set; }

    /// <summary>Lancamentos aceitos, na ordem em que entraram.</summary>
    public List<LancarPagamentoVO> Lancamentos { get; } = new List<LancarPagamentoVO>();

    /// <summary>Quando verdadeiro, LancarAsync falha antes de gravar, como um erro de banco.</summary>
    public bool FalharLancamento { get; set; }

    /// <summary>Lanca a cobranca de mentira: devolve um id novo e guarda o pedido.</summary>
    public Task<ReturnPagamentoVO> LancarAsync(LancarPagamentoVO entrada)
    {
        Chamadas++;
        if (FalharLancamento)
        {
            throw new InvalidOperationException(ConstantsMessage.ErroApuracaoCobrancasExtras);
        }

        Lancamentos.Add(entrada);
        ReturnPagamentoVO resultado = new ReturnPagamentoVO
        {
            IdPagamento = _proximoId++,
            IdLocacao = entrada.IdLocacao,
            Tipo = entrada.Tipo ?? string.Empty,
            Valor = entrada.Valor,
            DataVencimento = entrada.DataVencimento ?? DateOnly.MinValue,
            Status = StatusPagamento.PENDENTE.ToString()
        };
        return Task.FromResult(resultado);
    }

    /// <summary>Nao usado pela apuracao.</summary>
    public Task<ReturnPagamentoVO> BaixarAsync(int idPagamento, BaixarPagamentoVO entrada)
    {
        throw new NotImplementedException();
    }

    /// <summary>Nao usado pela apuracao.</summary>
    public Task<PaginaVO<ReturnPagamentoVO>> ListarAsync(FiltroPagamentoVO filtro)
    {
        throw new NotImplementedException();
    }

    /// <summary>Nao usado pela apuracao.</summary>
    public Task<PaginaVO<ReturnMeuPagamentoVO>> ListarMeusAsync(int idCliente, FiltroMeusPagamentosVO filtro)
    {
        throw new NotImplementedException();
    }

    /// <summary>Nao usado pela apuracao.</summary>
    public Task<ReturnResumoMeusPagamentosVO> ResumirMeusAsync(int idCliente, FiltroResumoMeusPagamentosVO filtro)
    {
        throw new NotImplementedException();
    }
}
