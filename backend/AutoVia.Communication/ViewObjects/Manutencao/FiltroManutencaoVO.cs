using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// Filtros da lista de ordens de manutencao (GET /api/manutencoes): paginacao (herdada) e, opcionais, a busca, o tipo, a
    /// situacao e o veiculo. Estrutura plana. A validacao e feita pelo FiltroManutencaoVOValidator.
    /// </summary>
    public class FiltroManutencaoVO : FiltroPaginacaoVO
    {
        /// <summary>Texto da busca: placa, marca, modelo ou codigo da ordem ("OS #id"). Opcional, ate 60 caracteres depois do Trim.</summary>
        public string? Busca { get; set; }

        /// <summary>Tipo da ordem: PREVENTIVA ou CORRETIVA, pelo nome exato depois do Trim. Opcional.</summary>
        public string? Tipo { get; set; }

        /// <summary>Situacao da ordem: ABERTA (sem data de saida) ou ENCERRADA (com data de saida). Opcional.</summary>
        public string? Situacao { get; set; }

        /// <summary>Id do veiculo, para o historico de um veiculo. Opcional; maior que zero quando informado.</summary>
        public int? IdVeiculo { get; set; }
    }
}
