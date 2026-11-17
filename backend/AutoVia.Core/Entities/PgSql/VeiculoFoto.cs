using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela veiculo_foto: uma foto do veiculo. O arquivo fica no armazenamento externo; o banco guarda so a chave do objeto,
    /// a ordem (a menor ordem ativa e a capa), quem enviou e, depois da remocao, quem removeu. Desvio aprovado por Kaio em 16/11/2026
    /// (backend/docs/especificacao/desvios_aprovados.json, bloco veiculo_foto). Filtro "Empresa" pelo Veiculo (EscopoEmpresa.cs).
    /// </summary>
    public class VeiculoFoto : BaseEntities
    {
        [Column("id_veiculo_foto")]
        public int IdVeiculoFoto { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        /// <summary>CHECK ck_veiculo_foto_ordem: ordem maior ou igual a 1. Nao e renumerada quando uma foto sai.</summary>
        [Column("ordem")]
        public int Ordem { get; set; }

        /// <summary>Chave do objeto no armazenamento. Nunca sai em rota nem em log.</summary>
        [Column("chave_objeto")]
        public string ChaveObjeto { get; set; } = string.Empty;

        /// <summary>Reservada para a miniatura da fatia completa; nesta fatia fica sempre nula.</summary>
        [Column("chave_miniatura")]
        public string? ChaveMiniatura { get; set; }

        /// <summary>Funcionario que enviou a foto (obrigatorio). Vem do contexto, nunca da requisicao.</summary>
        [Column("id_funcionario_envio")]
        public int IdFuncionarioEnvio { get; set; }

        /// <summary>Funcionario que removeu a foto; nulo enquanto a foto esta ativa.</summary>
        [Column("id_funcionario_remocao")]
        public int? IdFuncionarioRemocao { get; set; }

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual Funcionario FuncionarioEnvio { get; set; } = null!;
        public virtual Funcionario? FuncionarioRemocao { get; set; }
    }
}
