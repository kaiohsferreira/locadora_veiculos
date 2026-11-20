using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso a dados das ordens de manutencao (P8-BE-01, P8-BE-02 e P8-BE-03). A abertura e o encerramento rodam em
    /// transacao com a trava do veiculo (mesma consulta da troca de status). Lanca NotFoundException e BusinessException
    /// dentro da transacao; o repositorio registra cada uma no log e relanca.
    /// </summary>
    public interface IManutencaoRepository
    {
        /// <summary>
        /// Abre a ordem em transacao: trava o veiculo, aplica a regra de troca para MANUTENCAO, recusa ordem aberta e grava a
        /// ordem com a quilometragem do veiculo no momento. Devolve a ordem gravada, com o veiculo. Lanca NotFoundException
        /// (veiculo) ou BusinessException (regra ou ordem aberta).
        /// </summary>
        Task<Manutencao> AbrirAtomicamenteAsync(Manutencao manutencao);

        /// <summary>
        /// Encerra a ordem em transacao: trava o veiculo, confere a data de saida e a quilometragem, grava a saida, o custo e
        /// o km do veiculo e, se ele estiver em MANUTENCAO, devolve-o a DISPONIVEL. Lanca NotFoundException (ordem ou
        /// veiculo), BusinessException (ordem ja encerrada) ou ValidationException (datas ou km).
        /// </summary>
        Task<Manutencao> EncerrarAtomicamenteAsync(int idManutencao, DateOnly dataSaida, int kmAtual, decimal? custo);

        /// <summary>
        /// Lista os veiculos da locadora em VENCIDA ou PROXIMA (teto da lista), do mais atrasado para o menos atrasado, e o
        /// total sem o teto. A base de cada veiculo sai por subconsulta no banco.
        /// </summary>
        Task<(List<VeiculoRevisaoRegistro> Itens, int Total)> ListarAlertasRevisaoAsync();

        /// <summary>
        /// Lista as ordens da locadora com os filtros (busca ja normalizada: texto em minusculas, placa sem hifen e codigo da
        /// ordem; tipo, situacao (aberta = sem data de saida) e veiculo) e devolve a pagina e o total sem a pagina. Ordena por
        /// data de entrada e id decrescentes. A capa vem por Include. Duas consultas nomeadas.
        /// </summary>
        Task<(List<Manutencao> Itens, int Total)> ListarManutencoesAsync(string? textoBusca, string? placaBusca, int? idOsBusca, TipoManutencao? tipo, bool? aberta, int? idVeiculo, int deslocamento, int quantidade);

        /// <summary>Conta as ordens abertas (sem data de saida, nao desativadas) da locadora, sem periodo.</summary>
        Task<int> ContarOrdensAbertasAsync();

        /// <summary>Soma o custo das ordens encerradas da locadora cuja data de saida cai no periodo (inclusive). Custo nulo nao soma; sem ordem, zero.</summary>
        Task<decimal> SomarCustoEncerradasAsync(DateOnly dataInicio, DateOnly dataFim);

        /// <summary>Veiculos da locadora, nao desativados, com os ids pedidos e a capa (fotos ativas), para montar a foto dos alertas.</summary>
        Task<List<Veiculo>> ListarVeiculosDosAlertasAsync(IReadOnlyCollection<int> idsVeiculos);
    }
}
