using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Troca manual de status do veiculo (P1-BE-04). Arquivo parcial do VeiculoRepository. Transacao manual: trava o veiculo
    /// da locadora do contexto, le se ha reserva e locacao em aberto, aplica a regra pura e grava. O filtro "Empresa" continua
    /// ligado nas leituras, como segunda barreira. NotFoundException e BusinessException sao lancadas dentro da transacao, e
    /// o catch deste metodo registra no log sempre e relanca.
    /// </summary>
    public partial class VeiculoRepository
    {
        private const string SqlTravaStatusVeiculo = "SELECT * FROM veiculo WHERE id_veiculo = {0} AND id_empresa = {1} AND disabled_at IS NULL FOR UPDATE";

        /// <summary>
        /// Troca o status do veiculo em transacao. Devolve se alterou e o status anterior. Sem alteracao (mesmo status manual)
        /// nao grava nem muda UpdatedAt. Lanca NotFoundException (veiculo nao achado na locadora) ou BusinessException (recusa).
        /// </summary>
        public async Task<(bool Alterou, StatusVeiculo StatusAnterior)> AlterarStatusAtomicamenteAsync(int idVeiculo, StatusVeiculo destino)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Veiculo? veiculo = await ConsultaTravaStatusVeiculo(idVeiculo).FirstOrDefaultAsync();

                    if (veiculo == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                    }

                    bool temReservaEmAberto = await ConsultaReservasEmAbertoDoVeiculo(idVeiculo).AnyAsync();
                    bool temLocacaoEmAberto = await ConsultaLocacoesEmAbertoDoVeiculo(idVeiculo).AnyAsync();
                    StatusVeiculo statusAnterior = veiculo.Status;

                    RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(statusAnterior, destino, temReservaEmAberto, temLocacaoEmAberto);

                    if (resultado.SemAlteracao)
                    {
                        await transacao.CommitAsync();

                        return (false, statusAnterior);
                    }

                    if (!resultado.Permitida)
                    {
                        throw new BusinessException(resultado.MensagemRecusa ?? ConstantsMessage.ErroAlterarStatusVeiculo);
                    }

                    veiculo.Status = destino;
                    veiculo.UpdatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();

                    return (true, statusAnterior);
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
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: trava (FOR UPDATE) do veiculo pelo id e pela locadora do contexto, com o filtro "Empresa" ligado.
        /// Os dois valores vao como parametros do SQL bruto; nao ha interpolacao.
        /// </summary>
        public IQueryable<Veiculo> ConsultaTravaStatusVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .FromSqlRaw(SqlTravaStatusVeiculo, idVeiculo, _db.IdEmpresaContexto);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: reservas nao desativadas ATIVA ou CONFIRMADA do veiculo, com o filtro "Empresa" ligado. Lida
        /// depois da trava, pela troca manual de status.
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
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: locacoes nao desativadas EM_ANDAMENTO ou ATRASADA do veiculo, com o filtro "Empresa" ligado. Lida
        /// depois da trava, pela troca manual de status.
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
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
