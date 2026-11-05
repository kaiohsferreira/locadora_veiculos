namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Garante o administrador inicial configurado. Idempotente: se ja existe um
    /// funcionario ativo com o CPF configurado, nao cria outro. Nunca registra a senha.
    /// </summary>
    public interface IAdminInicialService
    {
        /// <summary>Cria o administrador inicial se ainda nao existir; devolve true somente quando criou.</summary>
        Task<bool> GarantirAdminInicialAsync();
    }
}
