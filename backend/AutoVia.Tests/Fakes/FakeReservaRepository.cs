using AutoVia.Communication.Enums;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public class FakeReservaRepository : IReservaRepository
    {
        private int _proximoId = 1;
        private readonly List<Reserva> _reservas = new List<Reserva>();
        private Veiculo? _veiculoVinculado;
        public Reserva? Armazenada { get; private set; }
        public Veiculo? VeiculoPersistido { get { return _veiculoVinculado; } }
        public int ChamadasDeInsercao { get; private set; }

        public Task<Veiculo?> FindVeiculoParaReservaAsync(int idVeiculo)
        {
            Veiculo? veiculo = _veiculoVinculado != null
                && _veiculoVinculado.IdVeiculo == idVeiculo
                && _veiculoVinculado.DisabledAt == null
                ? _veiculoVinculado
                : null;
            return Task.FromResult(veiculo);
        }

        public Task<Reserva> InsertAtomicallyAsync(Reserva reserva)
        {
            ChamadasDeInsercao++;
            reserva.IdReserva = _proximoId++;
            _reservas.Add(reserva);
            Armazenada = reserva;
            return Task.FromResult(reserva);
        }

        public void Adicionar(Reserva reserva)
        {
            if (reserva.IdReserva == 0)
            {
                reserva.IdReserva = _proximoId;
            }
            if (reserva.IdReserva >= _proximoId)
            {
                _proximoId = reserva.IdReserva + 1;
            }
            _reservas.Add(reserva);
            Armazenada = reserva;
        }

        public void VincularVeiculo(Veiculo veiculo)
        {
            _veiculoVinculado = veiculo;
        }

        public Task<Reserva?> FindByIdAsync(int idReserva, int idCliente)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva
                && x.IdCliente == idCliente
                && x.DisabledAt == null);
            return Task.FromResult(reserva);
        }

        public Task<int> CountAsync(int idCliente, StatusReserva? status)
        {
            int total = _reservas.Count(x => x.IdCliente == idCliente
                && x.DisabledAt == null
                && (!status.HasValue || x.Status == status.Value));
            return Task.FromResult(total);
        }

        public Task<List<Reserva>> FindAllAsync(int idCliente, StatusReserva? status, int deslocamento, int quantidade)
        {
            List<Reserva> reservas = _reservas
                .Where(x => x.IdCliente == idCliente
                    && x.DisabledAt == null
                    && (!status.HasValue || x.Status == status.Value))
                .OrderByDescending(x => x.DataRetiradaPrevista)
                .ThenByDescending(x => x.IdReserva)
                .Skip(deslocamento)
                .Take(quantidade)
                .ToList();
            return Task.FromResult(reservas);
        }

        public Task CancelAtomicallyAsync(int idReserva, int idCliente)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva
                && x.IdCliente == idCliente
                && x.DisabledAt == null);
            if (reserva == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
            }
            if (reserva.Status == StatusReserva.CANCELADA)
            {
                return Task.CompletedTask;
            }
            if (reserva.Status == StatusReserva.CONCLUIDA)
            {
                throw new BusinessException(ConstantsMessage.ErroReservaConcluidaNaoCancelavel);
            }
            reserva.Status = StatusReserva.CANCELADA;
            RecalcularStatusVeiculo(reserva.IdVeiculo);
            return Task.CompletedTask;
        }

        private void RecalcularStatusVeiculo(int idVeiculo)
        {
            if (_veiculoVinculado == null
                || _veiculoVinculado.IdVeiculo != idVeiculo
                || (_veiculoVinculado.Status != StatusVeiculo.DISPONIVEL && _veiculoVinculado.Status != StatusVeiculo.RESERVADO))
            {
                return;
            }

            bool existeReservaAtiva = _reservas.Any(x => x.IdVeiculo == idVeiculo
                && x.DisabledAt == null
                && (x.Status == StatusReserva.ATIVA || x.Status == StatusReserva.CONFIRMADA));
            _veiculoVinculado.Status = existeReservaAtiva ? StatusVeiculo.RESERVADO : StatusVeiculo.DISPONIVEL;
        }
    }
}
