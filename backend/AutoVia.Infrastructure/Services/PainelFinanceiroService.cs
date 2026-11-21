using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras do painel financeiro (P7-BE-05, RF-35). Hoje sempre vem de IRelogio (nunca DateTime.Now); o periodo padrao e o mes
    /// corrente; recebido pela data do pagamento e os demais pelo vencimento; o ticket e calculado aqui (duas casas, AwayFromZero).
    /// Os *Str sao formatados aqui com StaticMethods, porque os VOs de retorno nao nascem de entidade. Sem regra de negocio no
    /// controller. O CPF sai mascarado; a situacao mostra PENDENTE vencido como ATRASADO.
    /// </summary>
    public class PainelFinanceiroService : IPainelFinanceiroService
    {
        private readonly IPainelFinanceiroRepository _painelFinanceiroRepository;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public PainelFinanceiroService(IPainelFinanceiroRepository painelFinanceiroRepository, IRelogio relogio, ILogService logService)
        {
            _painelFinanceiroRepository = painelFinanceiroRepository;
            _relogio = relogio;
            _logService = logService;
        }

        /// <summary>
        /// Monta os quatro indicadores do periodo: recebido (data do pagamento), a receber, em atraso (vencimento) e o ticket
        /// medio (vencimento, dividido pela quantidade de pagos). Falha de consulta vira 500 (detalhe so no log).
        /// </summary>
        public async Task<ReturnPainelFinanceiroVO> ObterIndicadoresAsync(FiltroPainelFinanceiroVO filtro)
        {
            try
            {
                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                (DateOnly De, DateOnly Ate) periodo = RegrasPainelFinanceiro.ResolverPeriodo(filtro.De, filtro.Ate, hoje);

                IndicadorFinanceiroRegistro recebido = await _painelFinanceiroRepository.ObterIndicadorRecebidoAsync(periodo.De, periodo.Ate);
                IndicadorFinanceiroRegistro aReceber = await _painelFinanceiroRepository.ObterIndicadorAReceberAsync(periodo.De, periodo.Ate, hoje);
                IndicadorFinanceiroRegistro emAtraso = await _painelFinanceiroRepository.ObterIndicadorEmAtrasoAsync(periodo.De, periodo.Ate, hoje);
                IndicadorFinanceiroRegistro pagos = await _painelFinanceiroRepository.ObterIndicadorTicketMedioAsync(periodo.De, periodo.Ate);

                decimal ticketMedio = RegrasPainelFinanceiro.CalcularTicketMedio(pagos.Valor, pagos.Quantidade);

                ReturnPainelFinanceiroVO retorno = new ReturnPainelFinanceiroVO
                {
                    De = periodo.De,
                    Ate = periodo.Ate,
                    DeStr = StaticMethods.FormateDateToPTBR(periodo.De),
                    AteStr = StaticMethods.FormateDateToPTBR(periodo.Ate),
                    RecebidoValor = recebido.Valor,
                    RecebidoStr = StaticMethods.FormateToMonetary(recebido.Valor),
                    QuantidadeRecebido = recebido.Quantidade,
                    AReceberValor = aReceber.Valor,
                    AReceberStr = StaticMethods.FormateToMonetary(aReceber.Valor),
                    QuantidadeAReceber = aReceber.Quantidade,
                    EmAtrasoValor = emAtraso.Valor,
                    EmAtrasoStr = StaticMethods.FormateToMonetary(emAtraso.Valor),
                    QuantidadeEmAtraso = emAtraso.Quantidade,
                    TicketMedioValor = ticketMedio,
                    TicketMedioStr = StaticMethods.FormateToMonetary(ticketMedio),
                    QuantidadeTicket = pagos.Quantidade
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
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a pagina da aba pedida (TODOS por padrao) e os quatro contadores. PAGOS_RECENTES ignora o periodo e o CPF sai
        /// mascarado. Falha de consulta vira 500 (detalhe so no log).
        /// </summary>
        public async Task<ReturnTitulosFinanceirosVO> ObterTitulosAsync(FiltroTitulosFinanceirosVO filtro)
        {
            try
            {
                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                (DateOnly De, DateOnly Ate) periodo = RegrasPainelFinanceiro.ResolverPeriodo(filtro.De, filtro.Ate, hoje);

                (List<TituloFinanceiroRegistro> Itens, int Total) lista = await _painelFinanceiroRepository.ListarTitulosAsync(
                    filtro.Aba,
                    periodo.De,
                    periodo.Ate,
                    hoje,
                    filtro.Deslocamento,
                    filtro.TamanhoPagina);

                ContadoresTitulosFinanceirosRegistro contadores = await _painelFinanceiroRepository.ContarTitulosAsync(periodo.De, periodo.Ate, hoje);

                List<ReturnTituloFinanceiroVO> itens = new List<ReturnTituloFinanceiroVO>();

                foreach (TituloFinanceiroRegistro titulo in lista.Itens)
                {
                    itens.Add(MontarTitulo(titulo, hoje));
                }

                ReturnTitulosFinanceirosVO retorno = new ReturnTitulosFinanceirosVO
                {
                    QuantidadeTodos = contadores.QuantidadeTodos,
                    QuantidadeEmAtraso = contadores.QuantidadeEmAtraso,
                    QuantidadeVenceHoje = contadores.QuantidadeVenceHoje,
                    QuantidadePagosRecentes = contadores.QuantidadePagosRecentes,
                    Pagina = new PaginaVO<ReturnTituloFinanceiroVO>(itens, lista.Total, filtro.Pagina, filtro.TamanhoPagina)
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
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Uma linha da lista: origem, rotulos, valor e datas em texto, situacao com PENDENTE vencido como ATRASADO e CPF mascarado.</summary>
        private static ReturnTituloFinanceiroVO MontarTitulo(TituloFinanceiroRegistro titulo, DateOnly hoje)
        {
            StatusPagamento situacao = RegrasPagamento.Situacao(titulo.Tipo, titulo.Status, titulo.DataVencimento, hoje);
            bool origemLocacao = titulo.IdLocacao.HasValue;

            return new ReturnTituloFinanceiroVO
            {
                IdPagamento = titulo.IdPagamento,
                Origem = origemLocacao ? ConstantsValues.OrigemTituloLocacao : ConstantsValues.OrigemTituloVenda,
                OrigemStr = origemLocacao
                    ? string.Format(ConstantsMessage.FormatoOrigemLocacaoTitulo, titulo.IdLocacao)
                    : string.Format(ConstantsMessage.FormatoOrigemVendaTitulo, titulo.IdVenda),
                Tipo = titulo.Tipo.ToString(),
                TipoStr = RotuloDoTipo(titulo.Tipo),
                ClienteNome = titulo.ClienteNome ?? string.Empty,
                ClienteCpfMascarado = RegrasPainelFinanceiro.MascararCpf(titulo.ClienteCpf),
                Valor = titulo.Valor,
                ValorStr = StaticMethods.FormateToMonetary(titulo.Valor),
                DataVencimentoStr = StaticMethods.FormateDateToPTBR(titulo.DataVencimento),
                DiasParaVencer = RegrasPagamento.DiasParaVencer(titulo.DataVencimento, hoje),
                Situacao = situacao.ToString(),
                DataPagamentoStr = titulo.DataPagamento.HasValue ? StaticMethods.FormateDateToPTBR(titulo.DataPagamento.Value) : null,
                Forma = titulo.Forma,
                PodeDarBaixa = situacao == StatusPagamento.PENDENTE || situacao == StatusPagamento.ATRASADO
            };
        }

        /// <summary>Rotulo curto do tipo do lancamento (textos aprovados pelo Kaio em 20/11/2026).</summary>
        private static string RotuloDoTipo(TipoPagamento tipo)
        {
            if (tipo == TipoPagamento.DIARIA)
            {
                return ConstantsMessage.RotuloTipoDiaria;
            }

            if (tipo == TipoPagamento.COBRANCA_EXTRA)
            {
                return ConstantsMessage.RotuloTipoCobrancaExtra;
            }

            if (tipo == TipoPagamento.VENDA)
            {
                return ConstantsMessage.RotuloTipoVenda;
            }

            return tipo.ToString();
        }
    }
}
