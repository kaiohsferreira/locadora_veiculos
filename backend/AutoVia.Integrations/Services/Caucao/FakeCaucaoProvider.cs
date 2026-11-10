using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;

namespace AutoVia.Integrations.Services.Caucao
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede. Identificador deterministico: FAKE-CAU- +
    /// 12 primeiros hex do SHA-256 da chave de idempotencia, entao a mesma tentativa devolve a mesma caucao.
    /// Regras pelo token: FAKE-RECUSADO recusa (BusinessException), FAKE-ANALISE fica EM_ANALISE, qualquer
    /// outro reserva. O gateway so o libera em SANDBOX.
    /// </summary>
    public class FakeCaucaoProvider : ICaucaoProvider
    {
        private readonly CaucaoFakeStore _store;
        private readonly ILogService _logService;

        public FakeCaucaoProvider(CaucaoFakeStore store, ILogService logService)
        {
            _store = store;
            _logService = logService;
        }

        public string Nome => ConstantsCaucao.ProvedorFake;

        public bool IdentificadorValido(string identificadorProvedor)
        {
            return Regex.IsMatch(identificadorProvedor, ConstantsCaucao.PadraoIdentificadorFake);
        }

        public Task<CaucaoResultado> PreAutorizarAsync(ConfiguracaoIntegracao configuracao, CaucaoSolicitacao solicitacao, string chaveIdempotencia, int validadeReservaDias, CancellationToken cancellationToken = default)
        {
            try
            {
                string identificador = GerarIdentificador(chaveIdempotencia);
                CaucaoResultado caucao = _store.ObterOuCriar(identificador, (DateTime agora) => CriarCaucao(identificador, solicitacao, validadeReservaDias, agora));

                if (caucao.Status == CaucaoStatus.RECUSADA)
                {
                    throw new BusinessException(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro));
                }

                return Task.FromResult(caucao);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoPreAutorizar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoPreAutorizar, ex);
            }
        }

        public Task<CaucaoResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                CaucaoResultado? caucao = _store.Obter(identificadorProvedor);

                if (caucao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada);
                }

                return Task.FromResult(caucao);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoConsultar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoConsultar, ex);
            }
        }

        public Task<CaucaoResultado> CapturarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, decimal? valor, CancellationToken cancellationToken = default)
        {
            try
            {
                CaucaoResultado capturada = _store.Capturar(identificadorProvedor, valor);

                return Task.FromResult(capturada);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCapturar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoCapturar, ex);
            }
        }

        public Task<CaucaoResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                CaucaoResultado cancelada = _store.Cancelar(identificadorProvedor);

                return Task.FromResult(cancelada);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCancelar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoCancelar, ex);
            }
        }

        /// <summary>Identificador FAKE-CAU- + 12 primeiros hex do SHA-256 da chave de idempotencia (minusculo).</summary>
        private static string GerarIdentificador(string chaveIdempotencia)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(chaveIdempotencia));
            string hex = Convert.ToHexString(hash).ToLowerInvariant();

            return ConstantsCaucao.PrefixoIdentificadorFake + hex.Substring(0, ConstantsCaucao.TamanhoHashIdentificadorFake);
        }

        private static CaucaoResultado CriarCaucao(string identificador, CaucaoSolicitacao solicitacao, int validadeReservaDias, DateTime agora)
        {
            CaucaoStatus status = ObterStatusPorToken(solicitacao.TokenCartao);
            bool reservada = status == CaucaoStatus.RESERVADA;

            CaucaoResultado caucao = new CaucaoResultado
            {
                IdentificadorProvedor = identificador,
                ReferenciaExterna = solicitacao.ReferenciaExterna,
                Status = status,
                Valor = solicitacao.Valor,
                CartaoFinal = ConstantsCaucao.CartaoFinalFake,
                CriadoEm = agora,
                ReservaValidaAte = reservada ? (DateTime?)agora.AddDays(validadeReservaDias) : null,
                Provedor = ConstantsCaucao.ProvedorFake
            };

            return caucao;
        }

        private static CaucaoStatus ObterStatusPorToken(string token)
        {
            if (string.Equals(token, ConstantsCaucao.TokenFakeRecusado, StringComparison.Ordinal))
            {
                return CaucaoStatus.RECUSADA;
            }

            if (string.Equals(token, ConstantsCaucao.TokenFakeAnalise, StringComparison.Ordinal))
            {
                return CaucaoStatus.EM_ANALISE;
            }

            return CaucaoStatus.RESERVADA;
        }
    }
}
