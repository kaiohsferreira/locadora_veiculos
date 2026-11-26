using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Encerramento da locacao (P5-BE-04, RF-25; devolucao-encerramento v4, passo 10 e leituras da devolucao). Parte do
    /// LocacaoRepository. A trava usa LockActiveRowAsync, na ordem global: veiculo, reserva, locacao, pagamentos (O-4).
    /// </summary>
    public partial class LocacaoRepository
    {
        /// <summary>Trava de linha da locacao (so @id; o filtro de empresa vem da releitura EF sob a trava).</summary>
        private const string SqlBloquearLocacaoEncerramento = "SELECT id_locacao FROM locacao WHERE id_locacao = @id AND disabled_at IS NULL FOR UPDATE";

        /// <summary>Trava de TODAS as linhas de pagamento da locacao, numa constante so (so @id).</summary>
        private const string SqlBloquearPagamentosEncerramento = "WITH travados AS (SELECT id_pagamento FROM pagamento WHERE id_locacao = @id AND disabled_at IS NULL ORDER BY id_pagamento FOR UPDATE) SELECT COUNT(*) FROM travados";

        /// <summary>Locacao ativa da locadora do contexto, com o veiculo ativo. Sem trava e sem rastreamento.</summary>
        public async Task<Locacao?> FindParaEncerramentoAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await ConsultaLocacaoAtiva(idLocacao)
                    .Where(x => x.Veiculo.DisabledAt == null)
                    .AsNoTracking()
                    .Include(x => x.Veiculo)
                    .FirstOrDefaultAsync();

                return locacao;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagamentos ativos da locacao ativa. Sem trava e sem rastreamento.</summary>
        public async Task<List<Pagamento>> ListarPagamentosAtivosDaLocacaoAsync(int idLocacao)
        {
            try
            {
                List<Pagamento> pagamentos = await _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null && x.Locacao != null && x.Locacao.DisabledAt == null)
                    .OrderBy(x => x.IdPagamento)
                    .ToListAsync();

                return pagamentos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cobrancas extras ativas da locacao, de qualquer apuracao. Sem trava e sem rastreamento.</summary>
        public async Task<List<CobrancaExtra>> ListarCobrancasExtrasAtivasDaLocacaoAsync(int idLocacao)
        {
            try
            {
                List<CobrancaExtra> cobrancas = await _db.CobrancasExtras
                    .AsNoTracking()
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null && x.Locacao.DisabledAt == null)
                    .OrderBy(x => x.IdCobranca)
                    .ToListAsync();

                return cobrancas;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Transacao final do encerramento: trava veiculo, reserva, locacao e pagamentos (ordem global), reconfere sob trava e
        /// grava locacao, veiculo, caucao, alvo da retencao, cobranca que faltava e diaria num SaveChanges. Locacao ja FINALIZADA:
        /// nada gravado e Criada = falso. Veiculo fora de LOCADO ou locacao cancelada: 409, nada gravado.
        /// </summary>
        public async Task<ResultadoEncerramento> EncerrarAtomicamenteAsync(EncerramentoRegistro registro)
        {
            try
            {
                await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                bool veiculoBloqueado = await LockActiveRowAsync(SqlBloquearVeiculoAtivo, registro.IdVeiculo);

                if (!veiculoBloqueado)
                {
                    throw new BusinessException(ConstantsMessage.ErroDevolucaoVeiculoEstadoInvalido);
                }

                if (registro.IdReserva.HasValue)
                {
                    await LockActiveRowAsync(SqlBloquearReservaAtiva, registro.IdReserva.Value);
                }

                bool locacaoBloqueada = await LockActiveRowAsync(SqlBloquearLocacaoEncerramento, registro.IdLocacao);

                if (!locacaoBloqueada)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                await LockActiveRowAsync(SqlBloquearPagamentosEncerramento, registro.IdLocacao);

                Locacao? locacao = await ConsultaLocacaoAtiva(registro.IdLocacao)
                    .Include(x => x.Veiculo)
                    .Include(x => x.Pagamentos.Where(pagamento => pagamento.DisabledAt == null))
                    .FirstOrDefaultAsync();

                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                if (locacao.Status == StatusLocacao.FINALIZADA)
                {
                    ResultadoEncerramento repeticao = new ResultadoEncerramento
                    {
                        Locacao = locacao,
                        Criada = false,
                        StatusVeiculo = locacao.Veiculo.Status,
                        KmAtualVeiculo = locacao.Veiculo.KmAtual
                    };

                    return repeticao;
                }

                if (locacao.Status == StatusLocacao.CANCELADA || !RegrasDevolucao.LocacaoPodeEncerrar(locacao.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroDevolucaoLocacaoCancelada);
                }

                Veiculo veiculo = locacao.Veiculo;

                if (veiculo.Status != StatusVeiculo.LOCADO)
                {
                    throw new BusinessException(ConstantsMessage.ErroDevolucaoVeiculoEstadoInvalido);
                }

                int idReservaDaLocacao = registro.IdReserva ?? 0;
                bool haOutraReserva = await _db.Reservas
                    .AsNoTracking()
                    .AnyAsync(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null && x.IdReserva != idReservaDaLocacao
                        && (x.Status == StatusReserva.ATIVA || x.Status == StatusReserva.CONFIRMADA));

                StatusVeiculo? statusFinal = RegrasDevolucao.StatusVeiculoAposDevolucao(veiculo.Status, haOutraReserva);

                if (statusFinal == null)
                {
                    throw new BusinessException(ConstantsMessage.ErroDevolucaoVeiculoEstadoInvalido);
                }

                locacao.Status = StatusLocacao.FINALIZADA;
                locacao.KmChegada = registro.KmChegada;
                locacao.DataDevolucaoEfetiva = registro.DataDevolucaoEfetiva;
                locacao.ValorTotal = registro.ValorTotal;
                locacao.UpdatedAt = registro.Agora;

                veiculo.Status = statusFinal.Value;
                veiculo.KmAtual = registro.KmChegada;
                veiculo.UpdatedAt = registro.Agora;

                RegravarCaucaoSeNecessario(locacao, registro);
                QuitarAlvoSeNecessario(locacao, registro);

                Pagamento? faltante = null;

                if (registro.LancarCobrancaFaltante && registro.ValorCobrancaFaltante > 0m)
                {
                    StatusPagamento statusFaltante = registro.CobrancaFaltanteRetida ? StatusPagamento.PAGO : StatusPagamento.PENDENTE;
                    faltante = CriarPagamentoDaLocacao(locacao.IdLocacao, TipoPagamento.COBRANCA_EXTRA, registro.ValorCobrancaFaltante, statusFaltante, registro);
                    _db.Pagamentos.Add(faltante);
                }

                Pagamento? diaria = null;
                bool diariaAtiva = locacao.Pagamentos.Any(x => x.Tipo == TipoPagamento.DIARIA && x.DisabledAt == null);

                if (registro.LancarDiaria && !diariaAtiva && registro.ValorDiaria > 0m)
                {
                    diaria = CriarPagamentoDaLocacao(locacao.IdLocacao, TipoPagamento.DIARIA, registro.ValorDiaria, StatusPagamento.PENDENTE, registro);
                    _db.Pagamentos.Add(diaria);
                }

                await _db.SaveChangesAsync();
                await transacao.CommitAsync();

                ResultadoEncerramento resultado = new ResultadoEncerramento
                {
                    Locacao = locacao,
                    Criada = true,
                    StatusVeiculo = statusFinal.Value,
                    KmAtualVeiculo = veiculo.KmAtual,
                    IdPagamentoDiaria = diaria?.IdPagamento,
                    IdPagamentoCobrancaFaltante = faltante?.IdPagamento
                };

                return resultado;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Regrava a caucao quando a leitura sob trava ainda a mostra PENDENTE; se ja foi resolvida, so registra o log.</summary>
        private void RegravarCaucaoSeNecessario(Locacao locacao, EncerramentoRegistro registro)
        {
            try
            {
                if (!registro.IdPagamentoCaucao.HasValue || !registro.NovoStatusCaucao.HasValue)
                {
                    return;
                }

                Pagamento? caucao = locacao.Pagamentos.FirstOrDefault(x => x.IdPagamento == registro.IdPagamentoCaucao.Value && x.Tipo == TipoPagamento.CAUCAO);

                if (caucao == null)
                {
                    return;
                }

                if (caucao.Status != StatusPagamento.PENDENTE)
                {
                    string registroLog = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoRetentativa, caucao.IdPagamento, caucao.Status.ToString(), locacao.IdLocacao);
                    _logService.WriteInfo(registroLog, GetType().ToString());

                    return;
                }

                caucao.Status = registro.NovoStatusCaucao.Value;
                caucao.Valor = registro.ValorCaucaoGravado ?? caucao.Valor;
                caucao.UpdatedAt = registro.Agora;

                if (registro.DataHojeNaCaucao)
                {
                    caucao.DataPagamento = registro.Hoje;
                }

                if (caucao.Status == StatusPagamento.PAGO)
                {
                    caucao.Forma = ConstantsValues.FormaCartao;
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quita a COBRANCA_EXTRA alvo da retencao: PAGO, forma CARTAO, data de hoje e id_transacao nulo (P-3).</summary>
        private void QuitarAlvoSeNecessario(Locacao locacao, EncerramentoRegistro registro)
        {
            try
            {
                if (!registro.QuitarAlvo || !registro.IdPagamentoAlvo.HasValue)
                {
                    return;
                }

                Pagamento? alvo = locacao.Pagamentos.FirstOrDefault(x => x.IdPagamento == registro.IdPagamentoAlvo.Value && x.Tipo == TipoPagamento.COBRANCA_EXTRA);

                if (alvo == null || !RegrasDevolucao.AlvoRetencaoEmAberto(alvo.Status))
                {
                    string registroLog = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoAlvoRetencaoInconsistente, locacao.IdLocacao, registro.IdPagamentoAlvo.Value);
                    _logService.WriteInfo(registroLog, GetType().ToString());

                    throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
                }

                alvo.Status = StatusPagamento.PAGO;
                alvo.Forma = ConstantsValues.FormaCartao;
                alvo.DataPagamento = registro.Hoje;
                alvo.IdTransacao = null;
                alvo.UpdatedAt = registro.Agora;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Monta um pagamento da locacao (origem locacao, sem venda), com as datas do registro.</summary>
        private Pagamento CriarPagamentoDaLocacao(int idLocacao, TipoPagamento tipo, decimal valor, StatusPagamento status, EncerramentoRegistro registro)
        {
            try
            {
                Pagamento pagamento = new Pagamento
                {
                    IdLocacao = idLocacao,
                    IdVenda = null,
                    Tipo = tipo,
                    Valor = valor,
                    DataVencimento = registro.Hoje,
                    Status = status,
                    IdTransacao = null,
                    CreatedAt = registro.Agora,
                    UpdatedAt = registro.Agora,
                    DisabledAt = null
                };

                if (status == StatusPagamento.PAGO)
                {
                    pagamento.DataPagamento = registro.Hoje;
                    pagamento.Forma = ConstantsValues.FormaCartao;
                }

                return pagamento;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
