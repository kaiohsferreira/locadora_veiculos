using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;

namespace AutoVia.Integrations.Services.Caucao
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede. Identificador deterministico: FAKE-CAU- +
    /// 12 primeiros hex do SHA-256 da chave de idempotencia, entao a mesma tentativa devolve a mesma caucao.
    /// Regras pelo token: FAKE-RECUSADO recusa (BusinessException), FAKE-ANALISE fica EM_ANALISE, qualquer
    /// outro reserva.
    /// TRAVA UNICA (decisao 5 de 24/11/2026, a mesma do PIX): so responde com a chave Pagamentos:PermitirProvedorFake verdadeira,
    /// IHostEnvironment Development e a integracao em SANDBOX. A regra esta em RegrasProvedorFakePix.PodeResponder e e conferida
    /// aqui, no inicio das quatro operacoes (ConferirTravaDupla), antes de tocar no CaucaoFakeStore. SimulacaoLiberada usa o
    /// mesmo predicado, sem lancar.
    /// </summary>
    public class FakeCaucaoProvider : ICaucaoProvider
    {
        private readonly CaucaoFakeStore _store;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;

        public FakeCaucaoProvider(CaucaoFakeStore store, IHostEnvironment hostEnvironment, IConfiguration configuration, ILogService logService)
        {
            _store = store;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _logService = logService;
        }

        public string Nome => ConstantsCaucao.ProvedorFake;

        public bool IdentificadorValido(string identificadorProvedor)
        {
            return Regex.IsMatch(identificadorProvedor, ConstantsCaucao.PadraoIdentificadorFake);
        }

        /// <summary>
        /// Mesmo predicado da trava (ConferirTravaDupla), sem lancar: verdadeiro so com a chave Pagamentos:PermitirProvedorFake
        /// ligada, a API em Development e a integracao em SANDBOX. A chave e relida a cada chamada.
        /// </summary>
        public bool SimulacaoLiberada(ConfiguracaoIntegracao configuracao)
        {
            bool chavePermitida = RegrasProvedorFakePix.LerChave(_configuration);
            bool liberada = RegrasProvedorFakePix.PodeResponder(_hostEnvironment.IsDevelopment(), configuracao.Ambiente, chavePermitida);

            return liberada;
        }

        public Task<CaucaoResultado> PreAutorizarAsync(ConfiguracaoIntegracao configuracao, CaucaoSolicitacao solicitacao, string chaveIdempotencia, int validadeReservaDias, CancellationToken cancellationToken = default)
        {
            try
            {
                ConferirTravaDupla(configuracao);
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
                ConferirTravaDupla(configuracao);
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
                ConferirTravaDupla(configuracao);
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
                ConferirTravaDupla(configuracao);
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

        /// <summary>
        /// Trava unica do FAKE da caucao: sem a chave, fora de Development ou fora do SANDBOX nao responde (502, com a mensagem do
        /// FAKE fora do sandbox). Recusa antes de tocar no CaucaoFakeStore: nada e criado, consultado, capturado nem cancelado.
        /// </summary>
        private void ConferirTravaDupla(ConfiguracaoIntegracao configuracao)
        {
            if (!SimulacaoLiberada(configuracao))
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoFakeForaDoSandbox);
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
