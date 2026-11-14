using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// Resposta da listagem da equipe (GET /api/equipe). Nao segue PaginaVO porque a equipe traz os totais da empresa
    /// inteira (excecao registrada em api-conventions v3). Total e pagina usam os filtros; os totais nao usam.
    /// </summary>
    public class ReturnEquipeVO
    {
        /// <summary>Funcionarios da pagina atual, ordenados por nome.</summary>
        public List<ReturnFuncionarioVO> Itens { get; set; } = new List<ReturnFuncionarioVO>();

        /// <summary>Quantidade que atende aos filtros (todas as paginas).</summary>
        public int Total { get; set; }

        /// <summary>Pagina atual, base 1.</summary>
        public int Pagina { get; set; }

        /// <summary>Itens por pagina.</summary>
        public int TamanhoPagina { get; set; }

        /// <summary>Funcionarios ativos da locadora inteira, sem os filtros.</summary>
        public int TotalAtivos { get; set; }

        /// <summary>Funcionarios com acesso desativado da locadora inteira, sem os filtros.</summary>
        public int TotalInativos { get; set; }

        /// <summary>Administradores ativos da locadora inteira, sem os filtros.</summary>
        public int TotalAdministradores { get; set; }

        /// <summary>Ativos e inativos juntos da locadora inteira, sem os filtros.</summary>
        public int TotalCadastrados { get; set; }
    }
}
