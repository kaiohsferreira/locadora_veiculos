using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso ao contrato da locacao (contrato_locacao). Todas as consultas passam pelo filtro de empresa (EscopoEmpresa, pela
    /// Locacao) e filtram DisabledAt nulo. A trava da locacao fica com IVistoriaRepository.ExecutarComLocacaoBloqueadaAsync.
    /// </summary>
    public interface IContratoLocacaoRepository
    {
        /// <summary>Locacao ativa da locadora do contexto, com cliente, veiculo e empresa do veiculo; sem rastreamento. Nulo quando nao ha.</summary>
        Task<Locacao?> FindLocacaoParaContratoAsync(int idLocacao);

        /// <summary>Contrato ativo (DisabledAt nulo) da locacao, com o funcionario que coletou; sem rastreamento. Nulo quando nao ha.</summary>
        Task<ContratoLocacao?> FindAtivoByLocacaoAsync(int idLocacao);

        /// <summary>Grava o contrato assinado (uma linha nova, na transacao da trava) e devolve a entidade gravada.</summary>
        Task<ContratoLocacao> InsertAsync(ContratoLocacao contrato);
    }
}
