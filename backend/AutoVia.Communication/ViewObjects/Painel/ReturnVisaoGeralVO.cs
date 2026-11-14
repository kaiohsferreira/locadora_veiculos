using System.Collections.Generic;
using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Painel
{
    /// <summary>
    /// Resposta da visao geral da locadora (GET /api/painel/visao-geral, painel.contract.json). Os campos em reais (receita,
    /// a receber, em atraso, variacao e os *Str) saem nulos ou vazios para cargos sem permissao de valores
    /// (ADMINISTRADOR e FINANCEIRO podem ver); as quantidades saem sempre. Os *Str ja vem formatados em reais ou em dd/MM/yyyy.
    /// </summary>
    public class ReturnVisaoGeralVO
    {
        /// <summary>Periodo pedido (HOJE, SETE_DIAS, TRINTA_DIAS ou MES_ATUAL).</summary>
        public PeriodoVisaoGeral Periodo { get; set; }

        /// <summary>Primeiro dia do periodo, em dd/MM/yyyy.</summary>
        public string PeriodoInicioStr { get; set; } = string.Empty;

        /// <summary>Ultimo dia incluido no periodo (hoje), em dd/MM/yyyy.</summary>
        public string PeriodoFimStr { get; set; } = string.Empty;

        /// <summary>Verdadeiro quando a locadora nao tem nenhum veiculo ativo (qualquer status): a tela mostra o primeiro acesso.</summary>
        public bool PrimeiroAcesso { get; set; }

        /// <summary>Verdadeiro quando o cargo do token pode ver valores em reais (ADMINISTRADOR ou FINANCEIRO).</summary>
        public bool ValoresVisiveis { get; set; }

        /// <summary>Receita do periodo (pagamentos PAGO com data de pagamento no periodo, sem caucao). Nulo sem permissao de valores.</summary>
        public decimal? ReceitaPeriodo { get; set; }

        /// <summary>Receita do periodo formatada em reais. Vazio sem permissao de valores.</summary>
        public string ReceitaPeriodoStr { get; set; } = string.Empty;

        /// <summary>Variacao percentual da receita contra o periodo anterior de mesma duracao, com uma casa decimal. Nulo quando a receita do periodo anterior e zero ou sem permissao de valores.</summary>
        public decimal? VariacaoReceitaPercentual { get; set; }

        /// <summary>Total a receber (pagamentos PENDENTE ainda nao vencidos). Nulo sem permissao de valores.</summary>
        public decimal? AReceber { get; set; }

        /// <summary>Total a receber formatado em reais. Vazio sem permissao de valores.</summary>
        public string AReceberStr { get; set; } = string.Empty;

        /// <summary>Quantidade de cobrancas a receber (pagamentos PENDENTE nao vencidos). Sempre preenchida.</summary>
        public int QuantidadeCobrancasEmAberto { get; set; }

        /// <summary>Total em atraso (pagamentos ATRASADO ou PENDENTE vencido). Nulo sem permissao de valores.</summary>
        public decimal? EmAtraso { get; set; }

        /// <summary>Total em atraso formatado em reais. Vazio sem permissao de valores.</summary>
        public string EmAtrasoStr { get; set; } = string.Empty;

        /// <summary>Quantidade de cobrancas em atraso. Sempre preenchida.</summary>
        public int QuantidadeCobrancasVencidas { get; set; }

        /// <summary>Taxa de ocupacao: veiculos LOCADO / veiculos em operacao (sem VENDIDO e BAIXADO), com uma casa decimal. Zero quando nao ha veiculo em operacao.</summary>
        public decimal TaxaOcupacaoPercentual { get; set; }

        /// <summary>Veiculos na situacao LOCADO.</summary>
        public int VeiculosLocados { get; set; }

        /// <summary>Veiculos em operacao: todos os status menos VENDIDO e BAIXADO.</summary>
        public int TotalVeiculos { get; set; }

        /// <summary>Locacoes em andamento (status EM_ANDAMENTO).</summary>
        public int LocacoesAtivas { get; set; }

        /// <summary>Locacoes em andamento com devolucao prevista para hoje.</summary>
        public int DevolucoesPrevistasHoje { get; set; }

        /// <summary>Veiculos por situacao, com todos os status de StatusVeiculo na ordem do enum (zero quando nao ha nenhum).</summary>
        public List<FrotaSituacaoVO> FrotaPorSituacao { get; set; } = new List<FrotaSituacaoVO>();
    }
}
