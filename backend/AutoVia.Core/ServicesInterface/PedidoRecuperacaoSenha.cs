using AutoVia.Communication.Enums;
using AutoVia.Helpers;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Pedido de recuperacao de senha que segue para a fila e e processado em segundo plano (N1, RF-49).
    /// Carrega perfil e CPF ja normalizados (so digitos). O CPF nunca vai para log: ToString nao o expoe.
    /// </summary>
    public sealed class PedidoRecuperacaoSenha
    {
        /// <summary>Monta o pedido com o perfil informado e o CPF ja normalizado.</summary>
        public PedidoRecuperacaoSenha(PerfilUsuario? perfil, string cpf)
        {
            Perfil = perfil;
            Cpf = cpf;
        }

        /// <summary>Perfil da conta: CLIENTE ou FUNCIONARIO. Nulo so se o VO chegar sem ele (o processador nao encontra conta).</summary>
        public PerfilUsuario? Perfil { get; }

        /// <summary>CPF somente com digitos (11 caracteres quando vem do VO validado).</summary>
        public string Cpf { get; }

        /// <summary>Texto neutro: o CPF nunca aparece em log ou mensagem que use o objeto.</summary>
        public override string ToString()
        {
            return ConstantsValues.TextoPedidoRecuperacaoOculto;
        }
    }
}
