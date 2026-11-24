using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de locacao em memoria para testar servico e controller sem PostgreSQL. A abertura reconfere a reserva e a
    /// locacao existente "sob trava" (como o repositorio real) e monta a locacao pela mesma regra pura (RegrasLocacao), com a
    /// linha CAUCAO na navegacao quando vier caucao. Os testes podem programar falhas da abertura e da releitura da caucao e
    /// congelar a leitura sem trava (corrida de duas aberturas).
    /// </summary>
    public class FakeLocacaoRepository : ILocacaoRepository
    {
        private readonly List<Reserva> _reservas = new List<Reserva>();
        private readonly List<Reserva> _reservasComoSemeadas = new List<Reserva>();
        private readonly List<Funcionario> _funcionarios = new List<Funcionario>();
        private readonly List<Locacao> _locacoes = new List<Locacao>();
        private int _proximoIdLocacao = 1;
        private int _proximoIdPagamento = 1;

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
            _reservasComoSemeadas.Add(Copiar(copia));
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

        /// <summary>Copias das locacoes gravadas (com os pagamentos gravados junto), na ordem da gravacao.</summary>
        public List<Locacao> LocacoesGravadas()
        {
            List<Locacao> copias = _locacoes.Select(Copiar).ToList();

            return copias;
        }

        /// <summary>
        /// Corrida de duas aberturas: verdadeiro faz a leitura sem trava (FindReservaByIdAsync e ExistsActiveByReservaAsync) ver a
        /// reserva como foi semeada e nenhuma locacao, como se a outra abertura ainda nao tivesse gravado. A abertura (sob trava)
        /// continua vendo o estado real.
        /// </summary>
        public bool LeituraSemTravaCongelada { get; set; }

        /// <summary>Falhas da abertura, uma por chamada, lancadas antes de gravar (falha do banco ou reconferencia que recusa).</summary>
        public Queue<Exception> FalhasNaAbertura { get; } = new Queue<Exception>();

        /// <summary>Gancho chamado no inicio da abertura (antes das falhas e da gravacao), com o id da reserva.</summary>
        public Action<int>? AoAbrir { get; set; }

        /// <summary>
        /// Roteiro da releitura da caucao: recebe o numero da chamada (1, 2...) e devolve a resposta ou lanca. Sem roteiro, a
        /// releitura procura nas locacoes gravadas.
        /// </summary>
        public Func<int, bool>? RoteiroConferenciaDaCaucao { get; set; }

        /// <summary>Quantas vezes a releitura da caucao foi feita.</summary>
        public int ChamadasExisteCaucaoGravada { get; private set; }

        /// <summary>Caucao recebida pela ultima abertura (nula = abertura sem caucao).</summary>
        public CaucaoAberturaRegistro? UltimaCaucao { get; private set; }

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
            List<Reserva> origem = LeituraSemTravaCongelada ? _reservasComoSemeadas : _reservas;
            Reserva? reserva = origem.FirstOrDefault(x => x.IdReserva == idReserva && x.DisabledAt == null);
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
            bool existe = !LeituraSemTravaCongelada && _locacoes.Any(x => x.IdReserva == idReserva && x.DisabledAt == null);
            return Task.FromResult(existe);
        }

        /// <summary>Quantas vezes a abertura chegou ao repositorio (teste: 404 do servico nao chama a abertura).</summary>
        public int ChamadasStartFromReserva { get; private set; }

        /// <summary>Data de retirada recebida pelo repositorio na ultima abertura (vem do servico, pelo IRelogio).</summary>
        public DateTime? UltimaDataRetirada { get; private set; }

        public Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada, CaucaoAberturaRegistro? caucao)
        {
            ChamadasStartFromReserva++;
            UltimaDataRetirada = dataRetirada;
            UltimaCaucao = caucao;
            AoAbrir?.Invoke(idReserva);

            Reserva? reserva = _reservas.FirstOrDefault(x => x.IdReserva == idReserva && x.DisabledAt == null);
            if (reserva == null)
            {
                throw new InvalidOperationException();
            }

            if (FalhasNaAbertura.Count > 0)
            {
                throw FalhasNaAbertura.Dequeue();
            }

            // Reconferencia "sob trava", como o repositorio real: a reserva ja aberta por outra abertura recusa aqui.
            if (!RegrasLocacao.ReservaPermiteAbrirLocacao(reserva.Status))
            {
                throw new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido);
            }

            if (_locacoes.Any(x => x.IdReserva == idReserva && x.DisabledAt == null))
            {
                throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
            }

            DateTime agora = DateTime.Now;
            Locacao locacao = RegrasLocacao.CriarLocacaoDeAbertura(reserva, reserva.Veiculo, idFuncionario, dataRetirada, agora, caucao);
            locacao.IdLocacao = _proximoIdLocacao;
            _proximoIdLocacao++;

            foreach (Pagamento pagamento in locacao.Pagamentos)
            {
                pagamento.IdPagamento = _proximoIdPagamento;
                pagamento.IdLocacao = locacao.IdLocacao;
                _proximoIdPagamento++;
            }

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

        public Task<bool> ExisteCaucaoGravadaAsync(int idReserva, string idTransacao)
        {
            ChamadasExisteCaucaoGravada++;

            if (RoteiroConferenciaDaCaucao != null)
            {
                bool roteirizada = RoteiroConferenciaDaCaucao(ChamadasExisteCaucaoGravada);
                return Task.FromResult(roteirizada);
            }

            bool existe = _locacoes.Any(locacao => locacao.IdReserva == idReserva
                && locacao.DisabledAt == null
                && locacao.Pagamentos.Any(pagamento => pagamento.Tipo == TipoPagamento.CAUCAO
                    && pagamento.IdTransacao == idTransacao
                    && pagamento.DisabledAt == null));

            return Task.FromResult(existe);
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
                DisabledAt = origem.DisabledAt,
                Pagamentos = origem.Pagamentos.Select(Copiar).ToList()
            };

            return copia;
        }

        private static Pagamento Copiar(Pagamento origem)
        {
            Pagamento copia = new Pagamento
            {
                IdPagamento = origem.IdPagamento,
                IdLocacao = origem.IdLocacao,
                IdVenda = origem.IdVenda,
                Tipo = origem.Tipo,
                Valor = origem.Valor,
                DataVencimento = origem.DataVencimento,
                DataPagamento = origem.DataPagamento,
                Forma = origem.Forma,
                Status = origem.Status,
                IdTransacao = origem.IdTransacao,
                QrCodeBase64 = origem.QrCodeBase64,
                PixCopiaECola = origem.PixCopiaECola,
                PixExpiraEm = origem.PixExpiraEm,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }
    }
}
