namespace AutoVia.Communication.ViewObjects.Utils
{
    /// <summary>
    /// Retorno do GET /api/health. Mede apenas se o processo esta de pe: nao
    /// consulta o banco, para que uma oscilacao do banco nao derrube o healthcheck.
    /// </summary>
    public class HealthReturnVO
    {
        public string Status { get; set; } = string.Empty;

        public string Mensagem { get; set; } = string.Empty;

        public string DataHoraStr { get; set; } = string.Empty;
    }
}
