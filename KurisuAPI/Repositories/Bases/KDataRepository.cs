using Dapper;
using KurisuAPI.Factories;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Repositories.Bases;

public abstract class KDataRepository<TId, TData>(DbConnectionFactory factory) : KRepository(factory)
    where TId : IEquatable<TId>
    where TData : IKData<TId>
{
    protected virtual string IdColumn => "id";
    protected virtual string SelectColumns => "*";
    protected abstract string InsertColumns { get; }
    protected abstract string InsertValues { get; }
    protected abstract string UpdateQuery { get; }
    
    public async Task<TData?> GetByIdAsync(TId id)
    {
        var sql = $"""
                   SELECT {SelectColumns}
                   FROM {TableName}
                   WHERE {IdColumn} = @id
                   """;
        
        return (await QueryAsync<TData>(sql, new { id })).SingleOrDefault();
    }

    public Task<TId> InsertAsync(TData data)
    {
        var sql = $"""
                   INSERT INTO {TableName} ({InsertColumns})
                   VALUES ({InsertValues})
                   RETURNING id
                   """;

        return InsertReturningAsync<TId>(sql, data);
    }

    public Task UpdateAsync(TId id, TData data) => ExecuteOrThrowAsync(UpdateQuery, data,
        $"Failed to find {typeof(TData).Name} (ID: {id}) in {TableName}.");

    public async Task DeleteAsync(TId id)
    {
        var sql = $"""
                   DELETE FROM {TableName}
                   WHERE {IdColumn} = @id
                   """;

        await ExecuteOrThrowAsync(sql, new { id }, 
            $"Failed to find {typeof(TData).Name} (ID: {id}) in {TableName}.");
    }
    
    protected Task<List<TData>> QueryAsync(string sql, object? parameters = null) => QueryAsync<TData>(sql, parameters);
    
    private async Task<TResult> InsertReturningAsync<TResult>(string sql, object parameters)
    {
        await using var conn = await OpenAsync();

        return (await conn.ExecuteScalarAsync<TResult>(sql, parameters))!;
    }
}

public abstract class KDataRepository<TData>(DbConnectionFactory factory) : KDataRepository<int, TData>(factory) where TData : IKData;