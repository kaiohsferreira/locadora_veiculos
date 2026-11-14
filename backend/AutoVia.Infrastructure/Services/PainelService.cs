using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Painel;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras da visao geral da locadora (P11-BE-01 e P11-BE-02). A locadora vem sempre do contexto (o filtro do banco).
    /// Periodo: intervalo fechado no inicio e aberto no fim, pela data de hoje do servidor. Valores em reais (receita,
    /// a receber, em atraso e variacao) so quando IContextoEmpresa.PodeVerValores e verdadeiro; sem permissao as somas nem
    /// sao consultadas. Quantidades e frota saem para todos os cargos. Os *Str sao formatados aqui com StaticMethods, porque
    /// o VO de retorno nao nasce de uma entidade (nao ha Profile para ele).
    /// </summary>
    public class PainelService : IPainelService
    {
        private readonly IPainelRepository _painelRepository;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public PainelService(IPainelRepository painelRepository, IContextoEmpresa contextoEmpresa, IRelogio relogio, ILogService logService)
        {
            _painelRepository = painelRepository;
            _contextoEmpresa = contextoEmpresa;
            _relogio = relogio;
            _logService = logService;
        }

        /// <summary>
        /// Monta a visao geral: periodo e datas, situacao da frota e ocupacao, locacoes, quantidades de cobrancas e, quando o
        /// cargo pode ver valores, receita, variacao, a receber e em atraso. Falha de consulta vira 500 (detalhe so no log).
        /// </summary>
        public async Task<ReturnVisaoGeralVO> ObterVisaoGeralAsync(PeriodoVisaoGeral periodo)
        {
            try
            {
                bool valoresVisiveis = _contextoEmpresa.PodeVerValores;
                DateTime agora = _relogio.Agora;
                DateOnly hoje = DateOnly.FromDateTime(agora);
                JanelaPeriodo janela = CalcularJanela(periodo, hoje);

                List<FrotaSituacaoContagem> contagens = await _painelRepository.ContarVeiculosPorSituacaoAsync();
                List<FrotaSituacaoVO> frota = MontarFrota(contagens);

                int totalCadastrados = contagens.Sum(x => x.Quantidade);
                bool primeiroAcesso = totalCadastrados == 0;

                int totalVeiculos = frota.Where(x => EmOperacao(x.Situacao)).Sum(x => x.Quantidade);
                int veiculosLocados = frota.Where(x => x.Situacao == StatusVeiculo.LOCADO).Sum(x => x.Quantidade);
                decimal taxaOcupacao = CalcularTaxaOcupacao(veiculosLocados, totalVeiculos);

                int quantidadeAReceber = await _painelRepository.ContarAReceberAsync(hoje);
                int quantidadeEmAtraso = await _painelRepository.ContarEmAtrasoAsync(hoje);

                decimal? receitaPeriodo = null;
                decimal? variacaoReceita = null;
                decimal? aReceber = null;
                decimal? emAtraso = null;

                if (valoresVisiveis)
                {
                    decimal receitaAtual = await _painelRepository.SomarReceitaPeriodoAsync(janela.Inicio, janela.FimExclusivo);
                    decimal receitaAnterior = await _painelRepository.SomarReceitaPeriodoAsync(janela.InicioAnterior, janela.Inicio);

                    receitaPeriodo = receitaAtual;
                    variacaoReceita = CalcularVariacao(receitaAtual, receitaAnterior);
                    aReceber = await _painelRepository.SomarAReceberAsync(hoje);
                    emAtraso = await _painelRepository.SomarEmAtrasoAsync(hoje);
                }

                int locacoesAtivas = await _painelRepository.ContarLocacoesEmAndamentoAsync();
                DateTime inicioDia = agora.Date;
                int devolucoesHoje = await _painelRepository.ContarDevolucoesPrevistasAsync(inicioDia, inicioDia.AddDays(1));

                ReturnVisaoGeralVO retorno = new ReturnVisaoGeralVO
                {
                    Periodo = periodo,
                    PeriodoInicioStr = StaticMethods.FormateDateToPTBR(janela.Inicio),
                    PeriodoFimStr = StaticMethods.FormateDateToPTBR(hoje),
                    PrimeiroAcesso = primeiroAcesso,
                    ValoresVisiveis = valoresVisiveis,
                    ReceitaPeriodo = receitaPeriodo,
                    ReceitaPeriodoStr = StaticMethods.FormateToMonetary(receitaPeriodo),
                    VariacaoReceitaPercentual = variacaoReceita,
                    AReceber = aReceber,
                    AReceberStr = StaticMethods.FormateToMonetary(aReceber),
                    QuantidadeCobrancasEmAberto = quantidadeAReceber,
                    EmAtraso = emAtraso,
                    EmAtrasoStr = StaticMethods.FormateToMonetary(emAtraso),
                    QuantidadeCobrancasVencidas = quantidadeEmAtraso,
                    TaxaOcupacaoPercentual = taxaOcupacao,
                    VeiculosLocados = veiculosLocados,
                    TotalVeiculos = totalVeiculos,
                    LocacoesAtivas = locacoesAtivas,
                    DevolucoesPrevistasHoje = devolucoesHoje,
                    FrotaPorSituacao = frota
                };

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarVisaoGeral, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Janela do periodo: inicio incluido e fim (hoje + 1) excluido. HOJE = hoje; SETE_DIAS e TRINTA_DIAS = os ultimos N
        /// dias incluindo hoje; MES_ATUAL = do dia 1 ate hoje. O periodo anterior tem a mesma duracao, logo antes do inicio.
        /// </summary>
        private static JanelaPeriodo CalcularJanela(PeriodoVisaoGeral periodo, DateOnly hoje)
        {
            DateOnly fimExclusivo = hoje.AddDays(1);
            DateOnly inicio;

            switch (periodo)
            {
                case PeriodoVisaoGeral.HOJE:
                    inicio = hoje;
                    break;
                case PeriodoVisaoGeral.SETE_DIAS:
                    inicio = hoje.AddDays(1 - ConstantsValues.DiasPeriodoCurto);
                    break;
                case PeriodoVisaoGeral.TRINTA_DIAS:
                    inicio = hoje.AddDays(1 - ConstantsValues.DiasPeriodoMedio);
                    break;
                case PeriodoVisaoGeral.MES_ATUAL:
                    inicio = new DateOnly(hoje.Year, hoje.Month, 1);
                    break;
                default:
                    throw new InvalidOperationException(ConstantsMessage.ErroConsultarVisaoGeral);
            }

            int duracao = fimExclusivo.DayNumber - inicio.DayNumber;
            DateOnly inicioAnterior = inicio.AddDays(-duracao);

            return new JanelaPeriodo(inicio, fimExclusivo, inicioAnterior);
        }

        /// <summary>Lista com TODAS as situacoes de StatusVeiculo, na ordem do enum; situacao sem veiculo sai com zero.</summary>
        private static List<FrotaSituacaoVO> MontarFrota(List<FrotaSituacaoContagem> contagens)
        {
            List<FrotaSituacaoVO> frota = new List<FrotaSituacaoVO>();
            StatusVeiculo[] situacoes = Enum.GetValues<StatusVeiculo>();

            foreach (StatusVeiculo situacao in situacoes)
            {
                int quantidade = contagens.Where(x => x.Situacao == situacao).Sum(x => x.Quantidade);

                frota.Add(new FrotaSituacaoVO
                {
                    Situacao = situacao,
                    Quantidade = quantidade
                });
            }

            return frota;
        }

        /// <summary>Veiculos em operacao: todos os status, menos VENDIDO e BAIXADO.</summary>
        private static bool EmOperacao(StatusVeiculo situacao)
        {
            return situacao != StatusVeiculo.VENDIDO && situacao != StatusVeiculo.BAIXADO;
        }

        /// <summary>Locados sobre os em operacao, em percentual com uma casa decimal. Zero quando o denominador e zero.</summary>
        private static decimal CalcularTaxaOcupacao(int veiculosLocados, int totalVeiculos)
        {
            if (totalVeiculos <= 0)
            {
                return 0m;
            }

            decimal taxa = (decimal)veiculosLocados * 100m / totalVeiculos;

            return Math.Round(taxa, ConstantsValues.CasasDecimaisPercentualVisaoGeral, MidpointRounding.AwayFromZero);
        }

        /// <summary>Variacao da receita contra o periodo anterior, em percentual com uma casa. Nulo quando a base e zero.</summary>
        private static decimal? CalcularVariacao(decimal receitaAtual, decimal receitaAnterior)
        {
            if (receitaAnterior == 0m)
            {
                return null;
            }

            decimal variacao = (receitaAtual - receitaAnterior) * 100m / receitaAnterior;

            return Math.Round(variacao, ConstantsValues.CasasDecimaisPercentualVisaoGeral, MidpointRounding.AwayFromZero);
        }

        /// <summary>Janela de um periodo: inicio incluido, fim excluido, e o inicio do periodo anterior de mesma duracao.</summary>
        private sealed record JanelaPeriodo(DateOnly Inicio, DateOnly FimExclusivo, DateOnly InicioAnterior);
    }
}
