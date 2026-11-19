using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Acesso a dados das ordens de manutencao (P8-BE-01, P8-BE-02 e P8-BE-03). Abertura e encerramento rodam em transacao
    /// manual, com a trava do veiculo (a mesma consulta da troca de status: VeiculoRepository.SqlTravaStatusVeiculo) e as
    /// mesmas leituras de reserva e de locacao em aberto. A lista de alertas e uma consulta so, com a base por subconsulta.
    /// O filtro "Empresa" fica ligado em todas as leituras. Todo catch registra no log e relanca; 404, 409 e 400 sao
    /// lancados dentro da transacao e registrados aqui.
    /// </summary>
    public class ManutencaoRepository : IManutencaoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ManutencaoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>
        /// Abre a ordem em transacao: trava o veiculo, aplica a regra de troca para MANUTENCAO (com reserva e locacao em
        /// aberto), recusa ordem aberta, grava a ordem com o km do veiculo neste momento e, se preciso, o status MANUTENCAO.
        /// Devolve a ordem gravada, com o veiculo. Lanca NotFoundException ou BusinessException, registradas no log.
        /// </summary>
        public async Task<Manutencao> AbrirAtomicamenteAsync(Manutencao manutencao)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Veiculo? veiculo = await ConsultaTravaVeiculo(manutencao.IdVeiculo).FirstOrDefaultAsync();

                    if (veiculo == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                    }

                    bool temReservaEmAberto = await ConsultaReservasEmAbertoDoVeiculo(manutencao.IdVeiculo).AnyAsync();
                    bool temLocacaoEmAberto = await ConsultaLocacoesEmAbertoDoVeiculo(manutencao.IdVeiculo).AnyAsync();

                    RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(veiculo.Status, StatusVeiculo.MANUTENCAO, temReservaEmAberto, temLocacaoEmAberto);

                    if (!resultado.Permitida)
                    {
                        throw new BusinessException(resultado.MensagemRecusa ?? ConstantsMessage.ErroAbrirManutencao);
                    }

                    bool temOrdemAberta = await ConsultaManutencoesAbertasDoVeiculo(manutencao.IdVeiculo).AnyAsync();

                    if (temOrdemAberta)
                    {
                        throw new BusinessException(ConstantsMessage.ErroManutencaoJaAberta);
                    }

                    DateTime agora = DateTime.Now;

                    manutencao.IdManutencao = 0;
                    manutencao.KmRegistrado = veiculo.KmAtual;
                    manutencao.DataSaida = null;
                    manutencao.Custo = null;
                    manutencao.CreatedAt = agora;
                    manutencao.UpdatedAt = agora;
                    manutencao.DisabledAt = null;

                    if (veiculo.Status != StatusVeiculo.MANUTENCAO)
                    {
                        veiculo.Status = StatusVeiculo.MANUTENCAO;
                        veiculo.UpdatedAt = agora;
                    }

                    _db.Manutencoes.Add(manutencao);
                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();

                    manutencao.Veiculo = veiculo;

                    return manutencao;
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAbrirManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Encerra a ordem em transacao: le a ordem ativa (sem rastreamento) para achar o veiculo, trava o veiculo, relê a
        /// ordem depois da trava, recusa ordem ja encerrada, confere data de saida e km (ValidationException, 400), grava a
        /// saida, o custo e o km do veiculo e devolve o veiculo a DISPONIVEL so se ele estiver em MANUTENCAO.
        /// </summary>
        public async Task<Manutencao> EncerrarAtomicamenteAsync(int idManutencao, DateOnly dataSaida, int kmAtual, decimal? custo)
        {
            try
            {
                Manutencao? ordemAtual = await ConsultaManutencaoAtiva(idManutencao).AsNoTracking().FirstOrDefaultAsync();

                if (ordemAtual == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroManutencaoNaoEncontrada);
                }

                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Veiculo? veiculo = await ConsultaTravaVeiculo(ordemAtual.IdVeiculo).FirstOrDefaultAsync();

                    if (veiculo == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                    }

                    // Relida depois da trava: outro encerramento pode ter gravado a saida enquanto o veiculo estava travado.
                    Manutencao? ordem = await ConsultaManutencaoAtiva(idManutencao).FirstOrDefaultAsync();

                    if (ordem == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroManutencaoNaoEncontrada);
                    }

                    if (ordem.DataSaida != null)
                    {
                        throw new BusinessException(ConstantsMessage.ErroManutencaoJaEncerrada);
                    }

                    if (dataSaida < ordem.DataEntrada)
                    {
                        throw CriarRecusaDeCampo(ConstantsValues.CampoDataSaidaManutencao, ConstantsMessage.ErroManutencaoDataSaidaAnterior);
                    }

                    if (kmAtual < veiculo.KmAtual)
                    {
                        throw CriarRecusaDeCampo(ConstantsValues.CampoKmAtualManutencao, string.Format(ConstantsMessage.ErroManutencaoKmMenor, veiculo.KmAtual));
                    }

                    DateTime agora = DateTime.Now;

                    ordem.DataSaida = dataSaida;
                    ordem.Custo = custo;
                    ordem.UpdatedAt = agora;
                    veiculo.KmAtual = kmAtual;
                    veiculo.UpdatedAt = agora;

                    if (veiculo.Status == StatusVeiculo.MANUTENCAO)
                    {
                        veiculo.Status = StatusVeiculo.DISPONIVEL;
                    }

                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();

                    ordem.Veiculo = veiculo;

                    return ordem;
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista os veiculos em alerta (VENCIDA ou PROXIMA), do mais atrasado para o menos atrasado, no teto da lista, e o
        /// total sem o teto. Uma consulta para cada, sem trazer a frota para a memoria.
        /// </summary>
        public async Task<(List<VeiculoRevisaoRegistro> Itens, int Total)> ListarAlertasRevisaoAsync()
        {
            try
            {
                int total = await ConsultaAlertasRevisao().CountAsync();

                List<VeiculoRevisaoRegistro> itens = await ConsultaAlertasRevisaoOrdenada().ToListAsync();

                return (itens, total);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: a lista de alertas ja ordenada (o mais atrasado primeiro, faltam km crescente, e id do veiculo para
        /// desempatar), no teto da lista e projetada em VeiculoRevisaoRegistro. O maximo de preventivas sem registro vira zero
        /// (COALESCE). Publica para o teste de SQL por ToQueryString; quem executa e ListarAlertasRevisaoAsync.
        /// </summary>
        public IQueryable<VeiculoRevisaoRegistro> ConsultaAlertasRevisaoOrdenada()
        {
            try
            {
                IQueryable<VeiculoRevisaoRegistro> consulta = ConsultaAlertasRevisao()
                    .OrderBy(x => (x.Manutencoes
                        .Where(m => m.Tipo == TipoManutencao.PREVENTIVA && m.DisabledAt == null)
                        .Select(m => (int?)m.KmRegistrado)
                        .Max() ?? 0) + ConstantsValues.IntervaloRevisaoKm - x.KmAtual)
                    .ThenBy(x => x.IdVeiculo)
                    .Take(ConstantsValues.TetoAlertasRevisao)
                    .Select(x => new VeiculoRevisaoRegistro(
                        x.IdVeiculo,
                        x.Placa,
                        x.Marca,
                        x.Modelo,
                        x.Categoria.Nome,
                        x.KmAtual,
                        x.Manutencoes
                            .Where(m => m.Tipo == TipoManutencao.PREVENTIVA && m.DisabledAt == null)
                            .Select(m => (int?)m.KmRegistrado)
                            .Max()));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: trava (FOR UPDATE) do veiculo pelo id e pela locadora do contexto, com o filtro "Empresa" ligado.
        /// Mesmo SQL da trava da troca de status. Os valores vao como parametros.
        /// </summary>
        public IQueryable<Veiculo> ConsultaTravaVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .FromSqlRaw(VeiculoRepository.SqlTravaStatusVeiculo, idVeiculo, _db.IdEmpresaContexto);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta nomeada: ordem ativa (nao desativada) pelo id, com o filtro "Empresa" ligado.</summary>
        public IQueryable<Manutencao> ConsultaManutencaoAtiva(int idManutencao)
        {
            try
            {
                IQueryable<Manutencao> consulta = _db.Manutencoes
                    .Where(x => x.IdManutencao == idManutencao && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: ordens abertas do veiculo (data de saida nula e nao desativadas), com o filtro "Empresa" ligado.
        /// Mesmo predicado da troca manual de status (VeiculoRepository.ConsultaManutencoesAbertasDoVeiculo).
        /// </summary>
        public IQueryable<Manutencao> ConsultaManutencoesAbertasDoVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<Manutencao> consulta = _db.Manutencoes
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null && x.DataSaida == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: reservas do veiculo em aberto (ATIVA ou CONFIRMADA), com o filtro "Empresa" ligado. Mesmo
        /// predicado de VeiculoRepository.ConsultaReservasEmAbertoDoVeiculo (provado por ToQueryString).
        /// </summary>
        public IQueryable<Reserva> ConsultaReservasEmAbertoDoVeiculo(int idVeiculo)
        {
            try
            {
                StatusReserva[] statusAbertos = RegrasStatusVeiculo.StatusReservasEmAberto();

                IQueryable<Reserva> consulta = _db.Reservas
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null && statusAbertos.Contains(x.Status));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: locacoes do veiculo em aberto (EM_ANDAMENTO ou ATRASADA), com o filtro "Empresa" ligado. Mesmo
        /// predicado de VeiculoRepository.ConsultaLocacoesEmAbertoDoVeiculo (provado por ToQueryString).
        /// </summary>
        public IQueryable<Locacao> ConsultaLocacoesEmAbertoDoVeiculo(int idVeiculo)
        {
            try
            {
                StatusLocacao[] statusAbertos = RegrasStatusVeiculo.StatusLocacoesEmAberto();

                IQueryable<Locacao> consulta = _db.Locacoes
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null && statusAbertos.Contains(x.Status));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: veiculos da locadora (filtro "Empresa" ligado) em alerta de revisao. Fora VENDIDO e BAIXADO, nao
        /// desativados, e com km atual maior ou igual a base mais o intervalo menos a margem. A base e o maior km das
        /// preventivas nao desativadas, por subconsulta; sem preventiva, zero. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Veiculo> ConsultaAlertasRevisao()
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Status != StatusVeiculo.VENDIDO
                        && x.Status != StatusVeiculo.BAIXADO
                        && x.KmAtual >= (x.Manutencoes
                            .Where(m => m.Tipo == TipoManutencao.PREVENTIVA && m.DisabledAt == null)
                            .Select(m => (int?)m.KmRegistrado)
                            .Max() ?? 0) + ConstantsValues.IntervaloRevisaoKm - ConstantsValues.MargemRevisaoProximaKm);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Monta a excecao de validacao de um campo (400), com a mensagem da regra.</summary>
        private static ValidationException CriarRecusaDeCampo(string campo, string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(campo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }
    }
}
