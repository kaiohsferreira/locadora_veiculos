namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do contrato digital da locacao (contrato-locacao v5). Os textos de usuario foram aprovados pelo Kaio em 26/11/2026;
    /// os de log nao passam por aprovacao um a um (precedente de 17/11/2026). Constantes planas, no padrao do ConstantsMessage.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Regras de negocio (aprovadas) ---
        public const string ErroContratoSemVistoriaEntrada = "A locação não tem vistoria de entrada registrada. Registre a vistoria antes de emitir o contrato (RP-06).";
        public const string ErroContratoLocacaoNaoAberta = "O contrato só pode ser emitido com a locação em andamento ou atrasada.";
        public const string ErroContratoJaAssinado = "Este contrato já foi assinado.";
        public const string ErroContratoAceiteObrigatorio = "Marque os aceites obrigatórios para assinar o contrato.";

        // --- Formato da assinatura e do nome (aprovadas) ---
        public const string ErroContratoNomeAssinante = "Informe o nome de quem assina o contrato.";
        public const string ErroContratoNomeAssinanteTamanho = "O nome do assinante deve ter no máximo 100 caracteres.";
        public const string ErroContratoNomeAssinanteInvalido = "O nome do assinante contém caracteres inválidos.";
        public const string ErroContratoAssinaturaObrigatoria = "A assinatura é obrigatória.";
        public const string ErroContratoAssinaturaInvalida = "A assinatura deve ser uma imagem PNG válida.";
        public const string ErroContratoAssinaturaTamanho = "A assinatura excede o tamanho máximo permitido.";

        // --- Textos de log (sem aprovacao um a um; nunca logam nome, CPF, imagem nem documento) ---
        public const string ErroRepositorioContratoLocacao = "Falha no repositório do contrato da locação.";
        public const string ErroBuscarContratoLocacao = "Não foi possível consultar o contrato da locação.";
        public const string ErroAssinarContratoLocacao = "Não foi possível assinar o contrato da locação.";
        public const string ErroSalvarContratoLocacao = "Não foi possível gravar o contrato da locação.";

        // --- Clausulas do documento (textos aprovados em 26/11/2026, sem alteracao) ---
        public const string ContratoTituloClausulaObjeto = "Objeto da locação";
        public const string ContratoTextoClausulaObjeto = "O locador entrega ao locatário o veículo descrito neste contrato, para uso durante o período da locação, nas condições aqui previstas.";
        public const string ContratoTituloClausulaPrazos = "Prazos";
        public const string ContratoTextoClausulaPrazos = "A locação começa na data de retirada e tem como data prevista de devolução a indicada neste contrato. A devolução depois do prazo fica sujeita à cobrança de atraso prevista na tabela de cobranças.";
        public const string ContratoTituloClausulaValores = "Valores";
        public const string ContratoTextoClausulaValores = "A diária é a indicada neste contrato, cobrada por dia de locação. Outras cobranças, quando houver, são apuradas na devolução e informadas ao locatário.";
        public const string ContratoTituloClausulaCaucao = "Caução";
        public const string ContratoTextoClausulaCaucao = "Quando há caução, o valor indicado neste contrato é pré-autorizado no cartão do locatário na retirada. Na devolução, a caução é liberada ou retida apenas para cobrir cobranças extras apuradas.";
        public const string ContratoTituloClausulaCombustivel = "Política de combustível";
        public const string ContratoTextoClausulaCombustivel = "O veículo é retirado com o nível de combustível registrado na vistoria de entrada e deve ser devolvido com o mesmo nível. A falta de combustível na devolução é cobrada pelo valor do tanque completo indicado na tabela de cobranças, na proporção da diferença.";
        public const string ContratoTituloClausulaQuilometragem = "Quilometragem";
        public const string ContratoTextoClausulaQuilometragem = "A franquia de quilometragem é a indicada neste contrato. Cada quilômetro rodado além da franquia é cobrado pelo valor de km excedente da tabela de cobranças.";
        public const string ContratoTituloClausulaAvarias = "Avarias";
        public const string ContratoTextoClausulaAvarias = "Avarias registradas na vistoria de devolução que não constam na vistoria de entrada são cobradas conforme os valores apurados pela locadora.";
        public const string ContratoTituloClausulaNatureza = "Natureza do registro";
        public const string ContratoTextoClausulaNatureza = "Este documento é um registro gerado pelo sistema AutoVia. O texto do documento fica guardado com um código de conferência (SHA-256), que permite verificar se ele foi alterado depois da assinatura. Não tem certificação ICP-Brasil.";
    }
}
