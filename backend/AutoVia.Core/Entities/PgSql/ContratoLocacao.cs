using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela contrato_locacao (RF-21; desvio D10 do contrato contrato-locacao v5): copia assinada do contrato da locacao, com o
    /// documento em JSON (snapshot), o SHA-256 do documento e a imagem da assinatura. Um contrato ativo por locacao, checado no
    /// servico sob a trava da locacao; sem indice unico no banco (decisao de 04/11/2026). Exclusao so logica (DisabledAt).
    /// </summary>
    public class ContratoLocacao : BaseEntities
    {
        [Column("id_contrato_locacao")]
        public int IdContratoLocacao { get; set; }

        [Column("id_locacao")]
        public int IdLocacao { get; set; }

        /// <summary>Funcionario que coletou a assinatura (o da claim da requisicao, RN-9).</summary>
        [Column("id_funcionario")]
        public int IdFuncionario { get; set; }

        /// <summary>Snapshot do documento (JSON UTF-8). O hash cobre exatamente este texto.</summary>
        [Column("documento_json")]
        public string DocumentoJson { get; set; } = string.Empty;

        /// <summary>SHA-256 de documento_json, em hexadecimal minusculo (64 caracteres).</summary>
        [Column("hash_sha256")]
        public string HashSha256 { get; set; } = string.Empty;

        [Column("nome_assinante")]
        public string NomeAssinante { get; set; } = string.Empty;

        /// <summary>Sempre verdadeiro quando gravado (RN-5).</summary>
        [Column("aceite_condicoes_gerais")]
        public bool AceiteCondicoesGerais { get; set; }

        /// <summary>Verdadeiro so quando a clausula de politica de combustivel consta do documento e o aceite foi dado.</summary>
        [Column("aceite_politica_combustivel")]
        public bool AceitePoliticaCombustivel { get; set; }

        /// <summary>PNG em base64 (teto de 87.384 caracteres). Fica no banco, nao no armazenamento (decisao de arquitetura).</summary>
        [Column("assinatura_png_base64")]
        public string AssinaturaPngBase64 { get; set; } = string.Empty;

        /// <summary>Data e hora da assinatura pelo IRelogio (timestamp sem fuso).</summary>
        [Column("assinado_em")]
        public DateTime AssinadoEm { get; set; }

        // --- Navegacao ---
        public virtual Locacao Locacao { get; set; } = null!;
        public virtual Funcionario Funcionario { get; set; } = null!;
    }
}
