using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace AutoVia.Infrastructure.Persistence
{
    /// <summary>
    /// Isolamento por locadora (multi-locadora, T-BE-03b): o filtro global nomeado "Empresa" e
    /// registrado UMA vez, pelo AutoViaDbContext.OnModelCreating (metodo abaixo). Caminhos: Veiculo e
    /// Funcionario pela propria coluna id_empresa; Reserva, Locacao, Venda, Manutencao, Seguro e
    /// Sinistro pelo Veiculo; SinistroHistorico pelo Sinistro; VeiculoFoto pelo Veiculo; Vistoria e CobrancaExtra pela Locacao; Avaria pela Vistoria; Pagamento
    /// pela Locacao ou pela Venda; ClienteLocadora pelo proprio id_empresa OU pelo id_cliente do contexto.
    /// Fail-closed: sem contexto de empresa (id 0) nada volta.
    /// Globais sem filtro: Cliente, Categoria, RedefinicaoSenha, Integration*, Empresa.
    /// Lista fechada de quem pode desligar o filtro "Empresa" (bypass nomeado, por metodo, conferida pela guarda de testes),
    /// e o motivo de cada um: autenticacao (login, sessao e administrador inicial, antes de haver contexto); redefinicao de
    /// senha (titular do link, sem contexto); unicidade do veiculo (placa, chassi e renavam na plataforma inteira); CPF na
    /// plataforma (cadastro publico de locadora e inclusao de funcionario); reservas do proprio cliente (posse pelo cliente do
    /// contexto); conflito de periodo (reservas do veiculo, para a checagem de sobreposicao); veiculo de locadora ativa para
    /// reserva; trava do veiculo e da locacao (bloqueio de linha FOR UPDATE, sem leitura de dados fora do filtro); busca publica
    /// de disponiveis e catalogo publico de seminovos (so locadoras ativas e nao desativadas). Qualquer outro metodo que
    /// desligue o filtro reprova a guarda. Os metodos e a quantidade de cada simbolo estao em GuardaPorMetodoRepositoriosTests
    /// e em IsolamentoEmpresaGuardasTests.
    /// </summary>
    public partial class AutoViaDbContext
    {
        /// <summary>
        /// Registra o filtro "Empresa" nas 15 entidades que pertencem a uma locadora (ClienteLocadora tem a condicao dupla: locadora ou cliente). As expressoes leem
        /// IdEmpresaContexto de ESTA instancia (this): o EF troca o this pelo contexto que executa a consulta,
        /// por isso o filtro nao pode capturar outra instancia nem uma variavel local.
        /// </summary>
        private void AplicarEscopoEmpresa(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Veiculo>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Funcionario>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Reserva>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Locacao>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Venda>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Manutencao>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Seguro>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Sinistro>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<SinistroHistorico>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Sinistro.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Vistoria>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Locacao.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<CobrancaExtra>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Locacao.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Avaria>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Vistoria.Locacao.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<VeiculoFoto>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0 && x.Veiculo.IdEmpresa == IdEmpresaContexto);

            modelBuilder.Entity<Pagamento>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => IdEmpresaContexto > 0
                    && ((x.Locacao != null && x.Locacao.Veiculo.IdEmpresa == IdEmpresaContexto)
                        || (x.Venda != null && x.Venda.Veiculo.IdEmpresa == IdEmpresaContexto)));

            // Vinculo cliente x locadora: a locadora ve o seu par (id_empresa) e o cliente ve o seu (id_cliente).
            // Cada ramo so vale com o id do seu proprio contexto; sem nenhum dos dois, nada volta (fail-closed).
            modelBuilder.Entity<ClienteLocadora>().HasQueryFilter(
                ConstantsValues.NomeFiltroEmpresa,
                x => (IdEmpresaContexto > 0 && x.IdEmpresa == IdEmpresaContexto)
                    || (IdClienteContexto > 0 && x.IdCliente == IdClienteContexto));
        }
    }

    /// <summary>Extensoes do isolamento por locadora: bypass nomeado e restricao de catalogo.</summary>
    public static class EscopoEmpresa
    {
        /// <summary>
        /// Bypass nomeado: desliga SO o filtro "Empresa" desta consulta. Uso restrito aos metodos da lista fechada
        /// descrita na classe; qualquer outro uso reprova a guarda de testes.
        /// </summary>
        public static IQueryable<TEntidade> SemEscopoEmpresa<TEntidade>(this IQueryable<TEntidade> consulta)
            where TEntidade : class
        {
            List<string> filtrosIgnorados = new List<string> { ConstantsValues.NomeFiltroEmpresa };

            return consulta.IgnoreQueryFilters(filtrosIgnorados);
        }

        /// <summary>Restringe os veiculos as locadoras ativas (empresa ativa e nao desativada). Usado pelo catalogo publico.</summary>
        public static IQueryable<Veiculo> ApenasEmpresasAtivas(this IQueryable<Veiculo> consulta)
        {
            return consulta.Where(x => x.Empresa.Ativo && x.Empresa.DisabledAt == null);
        }
    }
}
