using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Busca publica de veiculos disponiveis (P3-BE-01, RF-11 e RF-72). Arquivo parcial do VeiculoRepository.
    /// A busca e o detalhe leem veiculos de todas as locadoras ativas pelo mesmo recorte publico e compartilham a
    /// checagem de reservas sobrepostas.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>Conta os veiculos disponiveis no periodo, de todas as locadoras ativas, com os filtros da busca.</summary>
        public async Task<int> CountDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, string? cidade = null, string? uf = null)
        {
            try
            {
                int total = await ConsultaDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax, cidade, uf).CountAsync();
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista a pagina dos veiculos disponiveis no periodo, com a categoria e a locadora, ordenada por id.</summary>
        public async Task<List<Veiculo>> FindDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade, string? cidade = null, string? uf = null)
        {
            try
            {
                List<Veiculo> veiculos = await ConsultaDisponiveisPaginada(retirada, devolucao, idCategoria, valorMin, valorMax, deslocamento, quantidade, cidade, uf)
                    .ToListAsync();
                return veiculos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta da busca publica: veiculos de todas as locadoras ativas (empresa ativa e nao desativada),
        /// com status DISPONIVEL ou RESERVADO e sem reserva ativa ou confirmada que se sobreponha ao periodo. O bypass
        /// do filtro de locadora vale para a consulta inteira, inclusive a subconsulta de reservas. Sem rastreamento.
        /// </summary>
        public IQueryable<Veiculo> ConsultaDisponiveis(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, string? cidade = null, string? uf = null)
        {
            try
            {
                IQueryable<Reserva> reservas = ConsultaReservasSobrepostas(retirada, devolucao);
                IQueryable<Veiculo> consulta = ConsultaFrotaPublicaLocacao()
                    .Where(x => !reservas.Any(r => r.IdVeiculo == x.IdVeiculo));

                if (idCategoria.HasValue)
                {
                    int categoria = idCategoria.Value;
                    consulta = consulta.Where(x => x.IdCategoria == categoria);
                }

                if (valorMin.HasValue)
                {
                    decimal minimo = valorMin.Value;
                    consulta = consulta.Where(x => x.ValorDiaria >= minimo);
                }

                if (valorMax.HasValue)
                {
                    decimal maximo = valorMax.Value;
                    consulta = consulta.Where(x => x.ValorDiaria <= maximo);
                }

                if (cidade != null && uf != null)
                {
                    string cidadeNormalizada = cidade.Trim().ToLowerInvariant();
                    string ufNormalizada = uf.Trim().ToUpperInvariant();
                    consulta = consulta.Where(x => x.Empresa.Cidade.Trim().ToLower() == cidadeNormalizada
                        && x.Empresa.Uf.ToUpper() == ufNormalizada);
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        /// <summary>Recorte público compartilhado pela busca e pelo detalhe de locação.</summary>
        private IQueryable<Veiculo> ConsultaFrotaPublicaLocacao()
        {
            try
            {
                return _db.Veiculos.AsNoTracking().SemEscopoEmpresa().ApenasEmpresasAtivas()
                    .Where(x => x.DisabledAt == null && (x.Status == StatusVeiculo.DISPONIVEL || x.Status == StatusVeiculo.RESERVADO));
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        /// <summary>Reservas ativas ou confirmadas que interceptam o intervalo de retirada inclusiva e devolução exclusiva.</summary>
        private IQueryable<Reserva> ConsultaReservasSobrepostas(DateOnly retirada, DateOnly devolucao)
        {
            try
            {
                DateTime inicio = retirada.ToDateTime(TimeOnly.MinValue);
                DateTime fim = devolucao.ToDateTime(TimeOnly.MinValue);
                return _db.Reservas.Where(r => r.DisabledAt == null
                    && (r.Status == StatusReserva.ATIVA || r.Status == StatusReserva.CONFIRMADA))
                    .Where(RegrasReserva.SobrepoePeriodo(inicio, fim));
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Pagina da busca publica: a consulta de ConsultaDisponiveis com Categoria, Empresa e a capa (fotos ativas, primeira
        /// por ordem) carregadas, ordenada por id do veiculo, com deslocamento e quantidade. A capa vem no mesmo filtro publico
        /// da consulta, sem parametro de empresa.
        /// </summary>
        public IQueryable<Veiculo> ConsultaDisponiveisPaginada(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade, string? cidade = null, string? uf = null)
        {
            try
            {
                IQueryable<Veiculo> pagina = ConsultaDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax, cidade, uf)
                    .Include(x => x.Categoria)
                    .Include(x => x.Empresa)
                    .Include(x => x.Fotos.Where(foto => foto.DisabledAt == null).OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto).Take(ConstantsValues.QuantidadeFotoCapaPorVeiculo))
                    .OrderBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }
    }
}
