using Dapper;
using KurisuAPI.Factories;
using Npgsql;

namespace KurisuAPI.Repositories
{
    public abstract class KRepositoryBase(DbConnectionFactory factory)
    {
        protected async Task<NpgsqlConnection> OpenAsync()
            => await factory.CreateAsync();

        protected async Task<int> ExecuteOrThrowAsync(string sql, object parameters, string notFoundMessage)
        {
            await using NpgsqlConnection conn = await OpenAsync();
            int rows = await conn.ExecuteAsync(sql, parameters);

            if (rows <= 0) throw new KeyNotFoundException(notFoundMessage);

            return rows;
        }

        protected async Task ExecuteAsync(string sql, object parameters)
        {
            await using NpgsqlConnection conn = await OpenAsync();
            await conn.ExecuteAsync(sql, parameters);
        }

        protected async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            await using NpgsqlConnection conn = await OpenAsync();

            return (await conn.QueryAsync<T>(sql, parameters)).AsList();
        }
    }

    public abstract class KRepositoryBase<TEntity>(DbConnectionFactory factory) : KRepositoryBase(factory)
    {
        protected abstract string TableName { get; }
        protected virtual string IdColumn => "id";
        protected virtual string SelectColumns => "*";

        public virtual async Task<TEntity?> GetByIdAsync(int id)
        {
            string sql = $"""
                SELECT {SelectColumns}
                FROM {TableName}
                WHERE {IdColumn} = @id
            """;

            return (await QueryAsync<TEntity>(sql, new { id })).SingleOrDefault();
        }

        public virtual async Task DeleteAsync(int id)
        {
            string sql = $"""
                DELETE FROM {TableName}
                WHERE {IdColumn} = @id
            """;

            string message = $"Failed to find {typeof(TEntity).Name} (ID: {id}) in {TableName}.";

            await ExecuteOrThrowAsync(sql, new { id }, message);
        }

        protected async Task<List<TEntity>> QueryAsync(string sql, object? parameters = null)
            => await QueryAsync<TEntity>(sql, parameters);

        protected async Task<List<TEntity>> SearchByColumnAsync(string column, string query, int limit)
        {
            string sql = $"""
                SELECT {SelectColumns} FROM {TableName}
                WHERE {column} ILIKE @query
                LIMIT @limit
            """;

            return await QueryAsync(sql, new { query = $"%{query}%", limit });
        }

        protected async Task<TResult> InsertReturningAsync<TResult>(string sql, object parameters)
        {
            await using NpgsqlConnection conn = await OpenAsync();

            return (await conn.ExecuteScalarAsync<TResult>(sql, parameters))!;
        }
    }
}
