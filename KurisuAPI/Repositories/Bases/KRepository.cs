using Dapper;
using KurisuAPI.Factories;
using Npgsql;

namespace KurisuAPI.Repositories.Bases
{
    public abstract class KRepository(DbConnectionFactory factory)
    {
        protected abstract string TableName { get; }

        protected async Task<NpgsqlConnection> OpenAsync(CancellationToken token = default) => 
            await factory.CreateAsync(token);

        protected async Task<int> ExecuteOrThrowAsync(string sql, object parameters, string notFoundMessage)
        {
            await using var conn = await OpenAsync();
            var rows = await conn.ExecuteAsync(sql, parameters);

            return rows <= 0 ? throw new KeyNotFoundException(notFoundMessage) : rows;
        }

        protected async Task ExecuteAsync(string sql, object parameters, CancellationToken token = default)
        {
            await using var conn = await OpenAsync(token);
            await conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: token));
        }

        protected async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            await using var conn = await OpenAsync();

            return (await conn.QueryAsync<T>(sql, parameters)).AsList();
        }
    }
}
