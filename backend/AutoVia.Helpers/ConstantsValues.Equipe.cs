namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da equipe da locadora (multi-locadora, P10-BE-03): tamanhos dos campos do funcionario, limite da busca,
    /// mascara do CPF exibido e rotulos usados nas mensagens de tamanho. Os limites de senha vem do cadastro do cliente.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Tamanho maximo do nome do funcionario (coluna nome, 120).</summary>
        public const int TamanhoMaximoNomeFuncionario = 120;

        /// <summary>Tamanho maximo da matricula (coluna matricula, 20).</summary>
        public const int TamanhoMaximoMatriculaFuncionario = 20;

        /// <summary>Tamanho maximo do e-mail do funcionario (120).</summary>
        public const int TamanhoMaximoEmailFuncionario = 120;

        /// <summary>Quantidade de digitos de um CPF (sem mascara).</summary>
        public const int TamanhoCpfFuncionario = 11;

        /// <summary>Tamanho maximo do texto da busca da equipe, medido depois do Trim (60).</summary>
        public const int TamanhoMaximoBuscaEquipe = 60;

        /// <summary>Caracteres da mascara do CPF que a busca da equipe ignora (pontos e traco); espacos sao ignorados a parte.</summary>
        public const string CaracteresMascaraCpf = ".-";

        /// <summary>Trecho ocultado do CPF exibido (341.***.***-12).</summary>
        public const string MascaraCpfOculta = "***";

        /// <summary>Rotulo do nome nas mensagens de tamanho.</summary>
        public const string RotuloNomeFuncionario = "nome";

        /// <summary>Rotulo da matricula nas mensagens de tamanho.</summary>
        public const string RotuloMatriculaFuncionario = "matrícula";

        /// <summary>Rotulo do e-mail nas mensagens de tamanho.</summary>
        public const string RotuloEmailFuncionario = "e-mail";
    }
}
