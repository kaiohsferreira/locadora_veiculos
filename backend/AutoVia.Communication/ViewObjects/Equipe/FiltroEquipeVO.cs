using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// Filtro da listagem da equipe (GET /api/equipe): paginacao herdada mais busca, cargo e situacao. Todos os filtros
    /// sao opcionais; ausente significa sem filtro. Validado por FiltroEquipeVOValidator.
    /// </summary>
    public class FiltroEquipeVO : FiltroPaginacaoVO
    {
        /// <summary>Busca em nome, CPF (so digitos) ou matricula, ate 60 caracteres. Vazia ou so espacos = sem filtro.</summary>
        public string? Busca { get; set; }

        /// <summary>Cargo para filtrar (um dos seis valores). Ausente = todos os cargos.</summary>
        public CargoFuncionario? Cargo { get; set; }

        /// <summary>Situacao: true so ativos, false so inativos, ausente = todos.</summary>
        public bool? Ativo { get; set; }
    }
}
