using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public partial class FakeVeiculoRepository
    {
        public Task<VeiculoLocacaoConsulta?> FindParaLocacaoAsync(int idVeiculo, DateOnly? retirada, DateOnly? devolucao, CancellationToken cancellationToken = default)
        {
            Veiculo? veiculo = _veiculos.Find(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null
                && (x.Status == StatusVeiculo.DISPONIVEL || x.Status == StatusVeiculo.RESERVADO)
                && x.Empresa != null && x.Empresa.Ativo && x.Empresa.DisabledAt == null);
            if (veiculo == null)
            {
                return Task.FromResult<VeiculoLocacaoConsulta?>(null);
            }

            bool? disponivel = null;
            if (retirada.HasValue && devolucao.HasValue)
            {
                DateTime inicio = retirada.Value.ToDateTime(TimeOnly.MinValue);
                DateTime fim = devolucao.Value.ToDateTime(TimeOnly.MinValue);
                disponivel = !veiculo.Reservas.Any(r => r.DisabledAt == null
                    && (r.Status == StatusReserva.ATIVA || r.Status == StatusReserva.CONFIRMADA)
                    && r.DataRetiradaPrevista < fim && r.DataDevolucaoPrevista > inicio);
            }

            VeiculoLocacaoConsulta consulta = new VeiculoLocacaoConsulta
            {
                Veiculo = Copiar(veiculo),
                DisponivelNoPeriodo = disponivel
            };
            return Task.FromResult<VeiculoLocacaoConsulta?>(consulta);
        }
    }
}
