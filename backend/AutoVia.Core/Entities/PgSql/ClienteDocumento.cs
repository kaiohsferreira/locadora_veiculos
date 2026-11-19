using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela cliente_documento: CNH ou comprovante de residencia do cliente (RF-08). O arquivo fica no armazenamento externo;
    /// o banco guarda so a chave do objeto, o nome limpo para a tela, o tipo do conteudo e o tamanho. Um documento ativo por
    /// tipo (regra do servico, sem indice unico). Desvio aprovado por Kaio em 19/11/2026 (backend/docs/especificacao/desvios_aprovados.json,
    /// bloco cliente_documento). Sem id da locadora: o cliente e da plataforma.
    /// </summary>
    public class ClienteDocumento : BaseEntities
    {
        [Column("id_cliente_documento")]
        public int IdClienteDocumento { get; set; }

        [Column("id_cliente")]
        public int IdCliente { get; set; }

        /// <summary>Tipo do documento. Enum gravado como texto (CHECK ck_cliente_documento_tipo).</summary>
        [Column("tipo")]
        public TipoDocumentoCliente Tipo { get; set; }

        /// <summary>Chave do objeto no armazenamento. Nunca sai em resposta de log nem de erro.</summary>
        [Column("chave_objeto")]
        public string ChaveObjeto { get; set; } = string.Empty;

        /// <summary>Nome original ja limpo (sem pasta e sem controle, ate 120 caracteres). So para a tela mostrar.</summary>
        [Column("nome_arquivo")]
        public string NomeArquivo { get; set; } = string.Empty;

        /// <summary>Tipo do conteudo decidido pelos primeiros bytes: image/jpeg, image/png ou application/pdf.</summary>
        [Column("tipo_conteudo")]
        public string TipoConteudo { get; set; } = string.Empty;

        [Column("tamanho_bytes")]
        public int TamanhoBytes { get; set; }

        // --- Navegacao ---
        public virtual Cliente Cliente { get; set; } = null!;
    }
}
