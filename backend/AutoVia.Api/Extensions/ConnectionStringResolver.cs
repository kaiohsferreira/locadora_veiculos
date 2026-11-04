namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Descobre a string de conexao do PostgreSQL.
    ///
    /// Ordem de precedencia (a primeira que existir vence):
    /// 1. ConnectionStrings:AutoViaConnection (appsettings.json, sobrescrita pelo
    ///    appsettings.Local.json, que nao vai para o git, ou pela variavel de
    ///    ambiente ConnectionStrings__AutoViaConnection);
    /// 2. DATABASE_URL no formato postgresql://usuario:senha@host:porta/banco,
    ///    que plataformas como Railway e Render injetam;
    /// 3. falha com mensagem clara na subida, em vez de um erro pior na primeira requisicao.
    /// </summary>
    public static class ConnectionStringResolver
    {
        public const string NomeConnectionString = "AutoViaConnection";

        public static string ResolveDatabaseConnection(this IConfiguration configuration)
        {
            string? explicitConnection = configuration.GetConnectionString(NomeConnectionString);

            if (!string.IsNullOrWhiteSpace(explicitConnection))
                return explicitConnection;

            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrWhiteSpace(databaseUrl))
                return ConvertDatabaseUrl(databaseUrl);

            throw new InvalidOperationException(
                "Banco de dados nao configurado. Defina ConnectionStrings:AutoViaConnection " +
                "(appsettings.Local.json ou variavel ConnectionStrings__AutoViaConnection) ou DATABASE_URL.");
        }

        /// <summary>Converte postgresql://usuario:senha@host:porta/banco para o formato do Npgsql.</summary>
        private static string ConvertDatabaseUrl(string databaseUrl)
        {
            Uri? uri;

            if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException(
                    "DATABASE_URL nao e uma URI valida. Esperado: postgresql://usuario:senha@host:porta/banco");
            }

            string[] credentials = uri.UserInfo.Split(':', 2);

            // A senha vem percent-encoded na URI; sem decodificar, senha com '@'
            // ou '#' chega errada e a autenticacao falha sem explicar o motivo.
            string username = Uri.UnescapeDataString(credentials[0]);
            string password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
            string database = uri.AbsolutePath.TrimStart('/');
            int port = uri.Port > 0 ? uri.Port : 5432;

            return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};" +
                   "SSL Mode=Prefer;Trust Server Certificate=true";
        }
    }
}
