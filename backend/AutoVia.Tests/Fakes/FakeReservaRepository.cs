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
        private readonly Dictionary<int, string> _emailPorTitular = new Dictionary<int, string>();
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

        public void VincularEmailTitular(int idCliente, string email)
        {
            _emailPorTitular[idCliente] = email;
        }

        public Task<string?> FindEmailTitularAsync(int idReserva, int idCliente, int idEmpresa)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva
                && x.IdCliente == idCliente
                && x.DisabledAt == null);
            Veiculo? veiculo = reserva?.Veiculo ?? _veiculoVinculado;
            if (reserva == null || veiculo == null || veiculo.IdVeiculo != reserva.IdVeiculo
                || veiculo.IdEmpresa != idEmpresa || veiculo.DisabledAt != null
                || reserva.Cliente?.DisabledAt != null)
            {
                return Task.FromResult<string?>(null);
            }

            _emailPorTitular.TryGetValue(idCliente, out string? email);
            return Task.FromResult(email);
        }

        public Task<Reserva?> FindByIdAsync(int idReserva, int idCliente)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva
                && x.IdCliente == idCliente
                && x.DisabledAt == null);
            return Task.FromResult(reserva);
        }

        public Task<int> CountAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca)
        {
            int total = FiltrarDoCliente(idCliente, status, idReservaBusca, textoBusca).Count();
            return Task.FromResult(total);
        }

        public Task<List<Reserva>> FindAllAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca, bool ordemCrescente, int deslocamento, int quantidade)
        {
            IEnumerable<Reserva> filtradas = FiltrarDoCliente(idCliente, status, idReservaBusca, textoBusca);
            IOrderedEnumerable<Reserva> ordenadas = ordemCrescente
                ? filtradas.OrderBy(x => x.DataRetiradaPrevista).ThenBy(x => x.IdReserva)
                : filtradas.OrderByDescending(x => x.DataRetiradaPrevista).ThenByDescending(x => x.IdReserva);
            List<Reserva> reservas = ordenadas
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

        /// <summary>Reservas nao desativadas do cliente com o filtro de status (nulo = todos) e a busca (codigo OU modelo).</summary>
        private IEnumerable<Reserva> FiltrarDoCliente(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca)
        {
            return _reservas.Where(x => x.IdCliente == idCliente
                && x.DisabledAt == null
                && (status == null || status.Contains(x.Status))
                && CasaBusca(x, idReservaBusca, textoBusca));
        }

        private static bool CasaBusca(Reserva reserva, int? idReservaBusca, string? textoBusca)
        {
            if (!idReservaBusca.HasValue && textoBusca == null)
            {
                return true;
            }

            if (idReservaBusca.HasValue && reserva.IdReserva == idReservaBusca.Value)
            {
                return true;
            }

            return textoBusca != null && (reserva.Veiculo?.Modelo ?? string.Empty).ToLowerInvariant().Contains(textoBusca);
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
