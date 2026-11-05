using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// Filtros da listagem de veiculos (GET /api/veiculos). Herda pagina e tamanhoPagina de
    /// FiltroPaginacaoVO. Todos os filtros sao opcionais e combinam com AND.
    /// Validado por FiltroVeiculoVOValidator.
    /// </summary>
    public class FiltroVeiculoVO : FiltroPaginacaoVO
    {
        /// <summary>Id da categoria (maior que zero). Opcional.</summary>
        public int? IdCategoria { get; set; }

        /// <summary>Status do veiculo (DISPONIVEL, RESERVADO, LOCADO, MANUTENCAO, VENDIDO ou BAIXADO). Opcional.</summary>
        public StatusVeiculo? Status { get; set; }

        /// <summary>Busca parcial pela placa, sem diferenciar maiusculas, ignorando hifen e espacos. Opcional.</summary>
        public string? Placa { get; set; }
    }
}
