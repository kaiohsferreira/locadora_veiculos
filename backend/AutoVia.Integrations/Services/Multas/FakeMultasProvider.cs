using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Multas;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Multas;
using AutoVia.Integrations.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Services.Multas
{
    /// <summary>Provedor deterministico de teste, sem rede e permitido apenas em SANDBOX.</summary>
    public class FakeMultasProvider : IMultasProvider
    {
        private readonly ILogService _logService;

        public FakeMultasProvider(ILogService logService)
        {
            _logService = logService;
        }

        public string Nome => ConstantsMultas.ProvedorFake;

        public Task<MultasConsultaResultado> ConsultarInfracoesAsync(
            ConfiguracaoIntegracao configuracao,
            MultasConsultaSolicitacao solicitacao,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
                {
                    throw new IntegrationException(configuracao.Codigo, ConstantsMultas.ChaveProvedor, ConstantsMessage.ErroMultasFakeForaDoSandbox);
                }

                if (solicitacao == null
                    || !StaticMethods.IsPlacaValida(solicitacao.Placa)
                    || solicitacao.InicioLocal > solicitacao.FimLocal)
                {
                    throw new BusinessException(ConstantsMessage.ErroMultasSolicitacaoInvalida);
                }

                int quantidade = DeterminarQuantidade(solicitacao.Placa);
                List<MultaResultado> infracoes = CriarInfracoes(solicitacao, quantidade);
                MultasConsultaResultado resultado = new MultasConsultaResultado { Infracoes = infracoes };
                return Task.FromResult(resultado);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroMultasConsulta, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Multas, ConstantsMessage.ErroMultasConsulta, ex);
            }
        }

        private static int DeterminarQuantidade(string placa)
        {
            char ultimoCaractere = placa[placa.Length - 1];
            if (ultimoCaractere == ConstantsMultas.FinalPlacaSemResultados)
            {
                return 0;
            }

            if (ultimoCaractere == ConstantsMultas.FinalPlacaUmResultado)
            {
                return 1;
            }

            return 2;
        }

        private static List<MultaResultado> CriarInfracoes(MultasConsultaSolicitacao solicitacao, int quantidade)
        {
            List<MultaResultado> infracoes = new List<MultaResultado>();
            long intervaloTicks = solicitacao.FimLocal.Ticks - solicitacao.InicioLocal.Ticks;

            for (int indice = 0; indice < quantidade; indice++)
            {
                long divisor = quantidade + 1L;
                DateTime dataHora = solicitacao.InicioLocal.AddTicks(intervaloTicks / divisor * (indice + 1L));
                string codigo = indice == 0 ? ConstantsMultas.CodigoInfracaoFakeUm : ConstantsMultas.CodigoInfracaoFakeDois;
                string descricao = indice == 0 ? ConstantsMultas.DescricaoInfracaoFakeUm : ConstantsMultas.DescricaoInfracaoFakeDois;
                decimal valor = indice == 0 ? ConstantsMultas.ValorInfracaoFakeUm : ConstantsMultas.ValorInfracaoFakeDois;
                string identificador = GerarIdentificador(solicitacao.Placa, dataHora, indice);

                infracoes.Add(new MultaResultado
                {
                    IdentificadorProvedor = identificador,
                    DataHoraLocal = dataHora,
                    CodigoInfracao = codigo,
                    Descricao = descricao,
                    Valor = valor
                });
            }

            return infracoes;
        }

        private static string GerarIdentificador(string placa, DateTime dataHora, int indice)
        {
            string material = string.Concat(placa, dataHora.ToString("O", CultureInfo.InvariantCulture), indice.ToString(CultureInfo.InvariantCulture));
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
            string hashHexadecimal = Convert.ToHexString(hash).ToLowerInvariant();
            return ConstantsMultas.PrefixoIdentificadorFake + hashHexadecimal.Substring(0, ConstantsMultas.TamanhoHashIdentificadorFake);
        }
    }
}