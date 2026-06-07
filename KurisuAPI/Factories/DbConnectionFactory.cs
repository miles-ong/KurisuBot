using Npgsql;

namespace KurisuAPI.Factories
{
    public sealed class DbConnectionFactory(string connectionString)
    {
        public async Task<NpgsqlConnection> CreateAsync(CancellationToken? token = null)
        {
            var connection = new NpgsqlConnection(connectionString);

            if (token is null) await connection.OpenAsync();
            else await connection.OpenAsync((CancellationToken)token);

            return connection;
        }
    }
}
