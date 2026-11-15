using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Contrato de persistencia do vinculo cliente x locadora (T-BE-06). Todas as consultas passam pelo DbSet filtrado
    /// pelo filtro "Empresa" (locadora do contexto OU cliente do contexto), sem bypass. Leituras ignoram os vinculos
    /// desativados (DisabledAt nulo). A regra do vinculo ativo de menor id fica no servico; a unicidade nao tem indice.
    /// </summary>
    public interface IClienteLocadoraRepository
    {
        /// <summary>Busca a locadora ativa (DisabledAt nulo e Ativo verdadeiro) pelo id; nulo se nao existir, estiver inativa ou desativada.</summary>
        Task<Empresa?> FindEmpresaAtivaAsync(int idEmpresa);

        /// <summary>Verdadeiro quando o cliente existe e esta ativo (DisabledAt nulo). Cliente desativado nao e decidido nem aparece.</summary>
        Task<bool> ClienteAtivoAsync(int idCliente);

        /// <summary>Vinculo ativo de menor id do par cliente x locadora (a linha que vale); nulo se nao houver.</summary>
        Task<ClienteLocadora?> FindAtivoMaisAntigoAsync(int idCliente, int idEmpresa);

        /// <summary>Grava um vinculo PENDENTE novo (id gerado, datas com DateTime.Now, DisabledAt nulo) e confirma no banco.</summary>
        Task<ClienteLocadora> InsertAsync(ClienteLocadora vinculo);

        /// <summary>Exclusao logica de um vinculo (DisabledAt). Falso se nao achou o vinculo ativo visivel no contexto.</summary>
        Task<bool> DesativarAsync(int idClienteLocadora);

        /// <summary>Grava a decisao em TODAS as linhas ativas do par. Falso se o par nao tem linha ativa visivel no contexto.</summary>
        Task<bool> DecidirParAsync(int idCliente, int idEmpresa, StatusCadastroCliente status, string? justificativa, int idFuncionarioVerificador, DateTime dataVerificacao);

        /// <summary>Conta os vinculos ativos do cliente, um por locadora (a linha de menor id de cada par).</summary>
        Task<int> CountMinhasAsync(int idCliente);

        /// <summary>Lista os vinculos ativos do cliente, um por locadora, ordenados por data de solicitacao, paginados. Traz a locadora.</summary>
        Task<List<ClienteLocadora>> FindMinhasAsync(int idCliente, int deslocamento, int quantidade);

        /// <summary>
        /// Conta as locadoras distintas em que o cliente pediu aprovacao desde a data informada (linhas ativas ou
        /// desativadas, contando id_empresa distinto). Base do teto de pedidos de aprovacao por cliente.
        /// </summary>
        Task<int> CountEmpresasSolicitadasDesdeAsync(int idCliente, DateTime desde);
    }
}
