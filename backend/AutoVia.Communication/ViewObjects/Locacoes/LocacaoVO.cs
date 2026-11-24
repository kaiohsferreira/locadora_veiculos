namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>
    /// Dados para iniciar uma locacao a partir de uma reserva. Flat: a reserva e, so quando a caucao e exigida
    /// (Pagamentos:ValorCaucao maior que 0), os dados que o formulario do cartao gera. Nunca leva valor, e-mail, CPF, descricao,
    /// referencia nem locadora: o valor vem da configuracao do servidor e o pagador vem do banco (contrato caucao-retirada v1.1).
    /// </summary>
    public class LocacaoVO
    {
        /// <summary>Reserva ativa ou confirmada da locadora do contexto.</summary>
        public int IdReserva { get; set; }

        /// <summary>Token do cartao gerado no front (8 a 128 caracteres: letras, numeros, '-' e '_'). Nunca vai para o log.</summary>
        public string? CaucaoTokenCartao { get; set; }

        /// <summary>Bandeira devolvida pelo formulario do cartao (2 a 30 caracteres: minusculas, numeros e '_'). Vem junto com o token.</summary>
        public string? CaucaoMetodoPagamentoId { get; set; }

        /// <summary>Emissor (issuer_id) devolvido pelo formulario do cartao; opcional, de 1 a 2147483647.</summary>
        public long? CaucaoEmissorId { get; set; }

        /// <summary>Device id do formulario do cartao para antifraude; opcional (em branco = ausente). Nunca vai para o log.</summary>
        public string? CaucaoIdentificadorDispositivo { get; set; }
    }
}
