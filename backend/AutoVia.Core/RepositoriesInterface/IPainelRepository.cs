using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Agregacoes da visao geral da locadora (P11-BE-01 e P11-BE-02). Todas as somas e contagens rodam no banco, com o filtro
    /// global "Empresa" do contexto (a locadora do funcionario autenticado) e DisabledAt nulo. Nenhum metodo traz entidades
    /// para somar em memoria. Sem bypass de isolamento: sem contexto de empresa, tudo volta zerado.
    /// </summary>
    public interface IPainelRepository
    {
        /// <summary>Soma da receita do periodo: pagamentos PAGO, sem caucao, com data de pagamento em [inicio, fimExclusivo).</summary>
        Task<decimal> SomarReceitaPeriodoAsync(DateOnly inicio, DateOnly fimExclusivo);

        /// <summary>Soma dos pagamentos PENDENTE com vencimento em hoje ou depois (ainda nao vencidos).</summary>
        Task<decimal> SomarAReceberAsync(DateOnly hoje);

        /// <summary>Quantidade dos pagamentos PENDENTE com vencimento em hoje ou depois.</summary>
        Task<int> ContarAReceberAsync(DateOnly hoje);

        /// <summary>Soma dos pagamentos em atraso: status ATRASADO, ou PENDENTE com vencimento anterior a hoje.</summary>
        Task<decimal> SomarEmAtrasoAsync(DateOnly hoje);

        /// <summary>Quantidade dos pagamentos em atraso (mesma regra da soma).</summary>
        Task<int> ContarEmAtrasoAsync(DateOnly hoje);

        /// <summary>Veiculos com DisabledAt nulo agrupados por situacao (so as situacoes que existem; o servico completa os zeros).</summary>
        Task<List<FrotaSituacaoContagem>> ContarVeiculosPorSituacaoAsync();

        /// <summary>Quantidade de locacoes em andamento (status EM_ANDAMENTO) com DisabledAt nulo.</summary>
        Task<int> ContarLocacoesEmAndamentoAsync();

        /// <summary>Quantidade de locacoes em andamento com devolucao prevista em [inicioDia, fimDia).</summary>
        Task<int> ContarDevolucoesPrevistasAsync(DateTime inicioDia, DateTime fimDia);
    }
}
