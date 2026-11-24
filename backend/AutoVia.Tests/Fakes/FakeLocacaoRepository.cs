using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>Repositorio de locacao em memoria para testar servico e controller sem PostgreSQL.</summary>
    public class FakeLocacaoRepository : ILocacaoRepository
    {
        private readonly List<Reserva> _reservas = new List<Reserva>();
        private readonly List<Funcionario> _funcionarios = new List<Funcionario>();
        private readonly List<Locacao> _locacoes = new List<Locacao>();
        private int _proximoIdLocacao = 1;

        public void Adicionar(Reserva reserva)
        {
            Reserva copia = Copiar(reserva);
            if (copia.IdReserva == 0)
            {
                copia.IdReserva = _reservas.Count + 1;
            }

            if (copia.Veiculo.IdVeiculo == 0)
            {
                copia.Veiculo.IdVeiculo = copia.IdVeiculo;
            }

            copia.IdVeiculo = copia.Veiculo.IdVeiculo;
            _reservas.Add(copia);
        }

        public void Adicionar(Funcionario funcionario)
        {
            Funcionario copia = Copiar(funcionario);
            _funcionarios.Add(copia);
        }

        public void Adicionar(Locacao locacao)
        {
            Locacao copia = Copiar(locacao);
            if (copia.IdLocacao == 0)
            {
                copia.IdLocacao = _proximoIdLocacao;
            }

            if (copia.IdLocacao >= _proximoIdLocacao)
            {
                _proximoIdLocacao = copia.IdLocacao + 1;
            }

            _locacoes.Add(copia);
        }

        public Reserva? ObterReserva(int idReserva)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva);
            return reserva == null ? null : Copiar(reserva);
        }

        public Veiculo? ObterVeiculo(int idReserva)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva);
            return reserva == null ? null : Copiar(reserva.Veiculo);
        }

        public Task<Reserva?> FindReservaParaRetiradaAsync(int idReserva)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva && x.DisabledAt == null);
            if (reserva != null)
            {
                reserva.Locacoes = _locacoes.Where(x => x.IdReserva == idReserva && x.DisabledAt == null).Select(Copiar).ToList();
            }
            return Task.FromResult(reserva);
        }

        public Task<Reserva?> FindReservaByIdAsync(int idReserva)
        {
            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva && x.DisabledAt == null);
            return Task.FromResult(reserva == null ? null : Copiar(reserva));
        }

        public Task<Funcionario?> FindActiveFuncionarioByIdAsync(int idFuncionario)
        {
            Funcionario? funcionario = _funcionarios.FirstOrDefault(x =>
                x.IdFuncionario == idFuncionario && x.Ativo && x.DisabledAt == null);
            return Task.FromResult(funcionario == null ? null : Copiar(funcionario));
        }

        public Task<bool> ExistsActiveByReservaAsync(int idReserva)
        {
            bool existe = _locacoes.Any(x => x.IdReserva == idReserva && x.DisabledAt == null);
            return Task.FromResult(existe);
        }

        /// <summary>Quantas vezes a abertura chegou ao repositorio (teste: 404 do servico nao chama a abertura).</summary>
        public int ChamadasStartFromReserva { get; private set; }

        /// <summary>Data de retirada recebida pelo repositorio na ultima abertura (vem do servico, pelo IRelogio).</summary>
        public DateTime? UltimaDataRetirada { get; private set; }

        public Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada)
        {
            ChamadasStartFromReserva++;
            UltimaDataRetirada = dataRetirada;

            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva && x.DisabledAt == null);
            if (reserva == null)
            {
                throw new InvalidOperationException();
            }

            DateTime agora = DateTime.Now;
            Locacao locacao = new Locacao
            {
                IdLocacao = _proximoIdLocacao,
                IdReserva = reserva.IdReserva,
                IdCliente = reserva.IdCliente,
                IdVeiculo = reserva.Veiculo.IdVeiculo,
                IdFuncionario = idFuncionario,
                DataRetirada = dataRetirada,
                DataDevolucaoPrevista = reserva.DataDevolucaoPrevista,
                KmSaida = reserva.Veiculo.KmAtual,
                ValorDiaria = reserva.Veiculo.ValorDiaria,
                ValorCaucao = 0m,
                Status = StatusLocacao.EM_ANDAMENTO,
                CreatedAt = agora,
                UpdatedAt = agora
            };

            _proximoIdLocacao++;
            reserva.Status = StatusReserva.CONCLUIDA;
            reserva.Veiculo.Status = StatusVeiculo.LOCADO;
            _locacoes.Add(Copiar(locacao));

            return Task.FromResult(Copiar(locacao));
        }

        public Task<Locacao?> FindByIdAsync(int idLocacao)
        {
            Locacao? locacao = _locacoes.FirstOrDefault(x => x.IdLocacao == idLocacao && x.DisabledAt == null);
            return Task.FromResult(locacao == null ? null : Copiar(locacao));
        }

        private static Reserva Copiar(Reserva origem)
        {
            Reserva copia = new Reserva
            {
                IdReserva = origem.IdReserva,
                IdCliente = origem.IdCliente,
                Cliente = origem.Cliente,
                IdVeiculo = origem.IdVeiculo,
                DataReserva = origem.DataReserva,
                DataRetiradaPrevista = origem.DataRetiradaPrevista,
                DataDevolucaoPrevista = origem.DataDevolucaoPrevista,
                ValorEstimado = origem.ValorEstimado,
                Status = origem.Status,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt,
                Veiculo = Copiar(origem.Veiculo)
            };

            return copia;
        }

        private static Veiculo Copiar(Veiculo origem)
        {
            Veiculo copia = new Veiculo
            {
                IdVeiculo = origem.IdVeiculo,
                IdCategoria = origem.IdCategoria,
                IdEmpresa = origem.IdEmpresa,
                AnoModelo = origem.AnoModelo,
                Categoria = origem.Categoria,
                Empresa = origem.Empresa,
                Fotos = origem.Fotos,
                Placa = origem.Placa,
                Marca = origem.Marca,
                Modelo = origem.Modelo,
                KmAtual = origem.KmAtual,
                ValorDiaria = origem.ValorDiaria,
                Status = origem.Status,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }

        private static Funcionario Copiar(Funcionario origem)
        {
            Funcionario copia = new Funcionario
            {
                IdFuncionario = origem.IdFuncionario,
                Nome = origem.Nome,
                Cpf = origem.Cpf,
                Matricula = origem.Matricula,
                Cargo = origem.Cargo,
                Email = origem.Email,
                DataAdmissao = origem.DataAdmissao,
                Ativo = origem.Ativo,
                SenhaHash = origem.SenhaHash,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }

        private static Locacao Copiar(Locacao origem)
        {
            Locacao copia = new Locacao
            {
                IdLocacao = origem.IdLocacao,
                IdReserva = origem.IdReserva,
                IdCliente = origem.IdCliente,
                Cliente = origem.Cliente,
                IdVeiculo = origem.IdVeiculo,
                IdFuncionario = origem.IdFuncionario,
                DataRetirada = origem.DataRetirada,
                DataDevolucaoPrevista = origem.DataDevolucaoPrevista,
                DataDevolucaoEfetiva = origem.DataDevolucaoEfetiva,
                KmSaida = origem.KmSaida,
                KmChegada = origem.KmChegada,
                ValorDiaria = origem.ValorDiaria,
                ValorCaucao = origem.ValorCaucao,
                ValorTotal = origem.ValorTotal,
                Status = origem.Status,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }
    }
}
