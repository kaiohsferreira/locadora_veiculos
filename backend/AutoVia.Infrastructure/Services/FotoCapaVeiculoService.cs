using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Capa do veiculo nas listas. Excecao deliberada a "registrar e relancar" (decisao do Kaio, 16/11/2026): falha do
    /// armazenamento (IntegrationException) e chave ausente no lote viram erroFotoCapa no item e NAO sobem. Cancelamento
    /// sobe sem log; excecao nao prevista sobe com log. Um registro por chamada, sem URL, chave, mensagem do SDK nem bytes.
    /// </summary>
    public sealed class FotoCapaVeiculoService : IFotoCapaVeiculoService
    {
        private readonly IArmazenamentoGateway _armazenamentoGateway;
        private readonly ILogService _logService;

        public FotoCapaVeiculoService(IArmazenamentoGateway armazenamentoGateway, ILogService logService)
        {
            _armazenamentoGateway = armazenamentoGateway;
            _logService = logService;
        }

        /// <summary>
        /// Zera os dois campos, monta a capa de cada veiculo da pagina, assina em lote (uma chamada, so se houver capa) e
        /// aplica nos itens: sucesso, sem foto (nulo) ou falha (erroFotoCapa). Cancelamento e excecao nao prevista sobem.
        /// </summary>
        public async Task PreencherAsync<T>(IReadOnlyCollection<Veiculo?> veiculos, IReadOnlyCollection<T> itens, CancellationToken cancellationToken = default) where T : IComFotoCapa
        {
            try
            {
                foreach (T item in itens)
                {
                    item.FotoCapa = null;
                    item.ErroFotoCapa = null;
                }

                Dictionary<int, VeiculoFoto> capasPorVeiculo = MontarCapasPorVeiculo(veiculos);
                if (capasPorVeiculo.Count == 0)
                {
                    return;
                }

                Dictionary<int, FotoCapaVeiculoVO> fotosAssinadas = await AssinarCapasAsync(capasPorVeiculo, cancellationToken);

                foreach (T item in itens)
                {
                    if (!capasPorVeiculo.ContainsKey(item.IdVeiculo))
                    {
                        continue;
                    }

                    if (fotosAssinadas.TryGetValue(item.IdVeiculo, out FotoCapaVeiculoVO? foto))
                    {
                        item.FotoCapa = foto;
                    }
                    else
                    {
                        item.ErroFotoCapa = ConstantsMessage.ErroFotoCapaIndisponivel;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroMontarCapaVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Uma capa por veiculo (repetido conta uma vez). Veiculo nulo, desativado, de locadora desativada ou sem foto ativa fica fora.</summary>
        private static Dictionary<int, VeiculoFoto> MontarCapasPorVeiculo(IReadOnlyCollection<Veiculo?> veiculos)
        {
            Dictionary<int, VeiculoFoto> capasPorVeiculo = new Dictionary<int, VeiculoFoto>();

            foreach (Veiculo? veiculo in veiculos)
            {
                if (veiculo == null || capasPorVeiculo.ContainsKey(veiculo.IdVeiculo) || !VeiculoMostraCapa(veiculo))
                {
                    continue;
                }

                VeiculoFoto? capa = RegrasVeiculoFoto.EscolherCapa(veiculo.Fotos);
                if (capa != null)
                {
                    capasPorVeiculo[veiculo.IdVeiculo] = capa;
                }
            }

            return capasPorVeiculo;
        }

        /// <summary>Veiculo ativo; quando a locadora veio carregada, ela tem de estar ativa e nao desativada.</summary>
        private static bool VeiculoMostraCapa(Veiculo veiculo)
        {
            if (veiculo.DisabledAt != null)
            {
                return false;
            }

            if (veiculo.Empresa == null)
            {
                return true;
            }

            return veiculo.Empresa.Ativo && veiculo.Empresa.DisabledAt == null;
        }

        /// <summary>
        /// Uma assinatura em lote para as chaves das capas. Falha do armazenamento (IntegrationException) registra uma vez e
        /// devolve vazio: todos os itens com capa levam o erro. Chave ausente no lote registra uma vez e so aquele veiculo fica sem foto assinada.
        /// </summary>
        private async Task<Dictionary<int, FotoCapaVeiculoVO>> AssinarCapasAsync(Dictionary<int, VeiculoFoto> capasPorVeiculo, CancellationToken cancellationToken)
        {
            Dictionary<int, FotoCapaVeiculoVO> fotos = new Dictionary<int, FotoCapaVeiculoVO>();
            List<string> chaves = capasPorVeiculo.Values.Select(capa => capa.ChaveObjeto).Distinct().ToList();

            IReadOnlyDictionary<string, ArquivoArmazenado> assinadas;
            try
            {
                assinadas = await _armazenamentoGateway.AssinarUrlsAsync(chaves, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(new Exception(ex.GetType().Name), ConstantsMessage.ErroMontarCapaVeiculo, GetType().ToString());
                return fotos;
            }

            bool chaveAusente = false;
            foreach (KeyValuePair<int, VeiculoFoto> capa in capasPorVeiculo)
            {
                if (assinadas.TryGetValue(capa.Value.ChaveObjeto, out ArquivoArmazenado? arquivo) && arquivo != null)
                {
                    fotos[capa.Key] = MontarFotoCapa(arquivo);
                }
                else
                {
                    chaveAusente = true;
                }
            }

            if (chaveAusente)
            {
                _logService.Write(ConstantsMessage.ErroCapaVeiculoSemUrlAssinada, ConstantsMessage.ErroMontarCapaVeiculo, GetType().ToString());
            }

            return fotos;
        }

        private static FotoCapaVeiculoVO MontarFotoCapa(ArquivoArmazenado arquivo)
        {
            FotoCapaVeiculoVO foto = new FotoCapaVeiculoVO
            {
                Url = arquivo.Url,
                UrlMiniatura = null,
                ExpiraEm = RegrasVeiculoFoto.FormatarExpiraEm(arquivo.ExpiraEm)
            };

            return foto;
        }
    }
}
