using KurisuAPI.Factories;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Repositories.Bases;
    
public abstract class KDataSearchRepository<TId, TData>(DbConnectionFactory factory) : KDataRepository<TId, TData>(factory)
    where TId : IEquatable<TId>
    where TData : IKData<TId>
{
    protected abstract string SearchColumn { get; }

    public Task<List<TData>> SearchAsync(string query, int limit)
    {
        var sql = $"""
                   SELECT {SelectColumns} FROM {TableName}
                   WHERE {SearchColumn} ILIKE @query
                   LIMIT @limit
                   """;

        return QueryAsync(sql, new { query = $"%{query}%", limit });
    }
}

public abstract class KDataSearchRepository<TData>(DbConnectionFactory factory)
    : KDataSearchRepository<int, TData>(factory) where TData : IKData;


