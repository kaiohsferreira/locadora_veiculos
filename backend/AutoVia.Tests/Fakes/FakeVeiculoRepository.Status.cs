using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Troca manual de status do FakeVeiculoRepository. Aplica a MESMA regra pura do VeiculoRepository real
    /// (RegrasStatusVeiculo.Avaliar) e a mesma leitura de reserva e de locacao em aberto. Veiculo desativado ou de outra
    /// locadora (IdEmpresaDoContexto) conta como inexistente, como o filtro do banco.
    /// </summary>
    public partial class FakeVeiculoRepository
    {
        /// <summary>Locadora do contexto da requisicao simulada. Zero (padrao) nao acha nenhum veiculo: o filtro fica fechado.</summary>
        public int IdEmpresaDoContexto { get; set; }

        /// <summary>Reservas do veiculo que a leitura de reservas em aberto enxerga (todas as locadoras, filtradas pelo veiculo).</summary>
        public List<Reserva> ReservasDoVeiculo { get; } = new List<Reserva>();

        /// <summary>Locacoes do veiculo que a leitura de locacoes em aberto enxerga.</summary>
        public List<Locacao> LocacoesDoVeiculo { get; } = new List<Locacao>();

        /// <summary>Quando nao for null, AlterarStatusAtomicamenteAsync lanca esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoAlterarStatus { get; set; }

        /// <summary>Troca o status em memoria, com a regra pura. Devolve se alterou e o status anterior; lanca 404 e 409 como o real.</summary>
        public Task<(bool Alterou, StatusVeiculo StatusAnterior)> AlterarStatusAtomicamenteAsync(int idVeiculo, StatusVeiculo destino)
        {
            if (ExcecaoAoAlterarStatus != null)
            {
                throw ExcecaoAoAlterarStatus;
            }

            Veiculo? veiculo = _veiculos.FirstOrDefault(x => x.IdVeiculo == idVeiculo
                && x.DisabledAt == null
                && IdEmpresaDoContexto > 0
                && x.IdEmpresa == IdEmpresaDoContexto);

            if (veiculo == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
            }

            StatusVeiculo statusAnterior = veiculo.Status;

            bool temReservaEmAberto = ReservasDoVeiculo.Any(x => x.IdVeiculo == idVeiculo
                && x.DisabledAt == null
                && RegrasStatusVeiculo.StatusReservasEmAberto().Contains(x.Status));

            bool temLocacaoEmAberto = LocacoesDoVeiculo.Any(x => x.IdVeiculo == idVeiculo
                && x.DisabledAt == null
                && RegrasStatusVeiculo.StatusLocacoesEmAberto().Contains(x.Status));

            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(statusAnterior, destino, temReservaEmAberto, temLocacaoEmAberto);

            if (resultado.SemAlteracao)
            {
                return Task.FromResult<(bool Alterou, StatusVeiculo StatusAnterior)>((false, statusAnterior));
            }

            if (!resultado.Permitida)
            {
                throw new BusinessException(resultado.MensagemRecusa ?? ConstantsMessage.ErroAlterarStatusVeiculo);
            }

            veiculo.Status = destino;
            veiculo.UpdatedAt = DateTime.Now;

            return Task.FromResult<(bool Alterou, StatusVeiculo StatusAnterior)>((true, statusAnterior));
        }
    }
}
