using KurisuAPI.Factories;
using KurisuCore.Models.Bases;
using Npgsql;

namespace KurisuAPI.Repositories.Bases;

public abstract class KDataBulkRepository<TId, TData>(DbConnectionFactory factory) : KDataRepository<TId, TData>(factory)
    where TId : IEquatable<TId>
    where TData : IKData<TId>
{
    protected abstract string TempTableColumns { get; }
    protected abstract Task WriteBulkRowAsync(NpgsqlBinaryImporter binaryImporter, TData data, CancellationToken token);
    
    public async Task BulkUpsertAsync(IEnumerable<TData> data, CancellationToken token = default)
    {
        var dirty = data as IList<TData> ?? data.ToList();

        if (dirty.Count == 0) return;

        await using var conn = await OpenAsync(token);
        await using var transaction = await conn.BeginTransactionAsync(token);

        var tempTable = $"temp_{TableName}";

        var createTempSql = $"""
                             CREATE TEMP TABLE {tempTable} (
                                {TempTableColumns}
                             ) ON COMMIT DROP
                             """;

        await using var createCommand = new NpgsqlCommand(createTempSql, conn, transaction);
        await createCommand.ExecuteNonQueryAsync(token);

        await using var writer = await conn.BeginBinaryImportAsync(
            $"COPY {tempTable} ({InsertColumns}) FROM STDIN (FORMAT BINARY)", token);

        foreach (var d in dirty)
        {
            await writer.StartRowAsync(token);
            await WriteBulkRowAsync(writer, d, token);
        }

        await writer.CompleteAsync(token);
        
        var upsertSql = $"""
                         INSERT INTO {TableName} ({InsertColumns})
                         SELECT {InsertColumns}
                         FROM {tempTable}
                         ON CONFLICT ({IdColumn}) DO UPDATE SET
                            {BuildBulkUpdateSetClause()}
                         """;

        await using var upsertCommand = new NpgsqlCommand(upsertSql, conn, transaction);
        await upsertCommand.ExecuteNonQueryAsync(token);

        await transaction.CommitAsync(token);
    }

    public async Task BulkDeleteAsync(IEnumerable<TId> ids, CancellationToken token = default)
    {
        var list = ids as IList<TId> ?? ids.ToList();

        if (list.Count == 0) return;

        var sql = $"""
                   DELETE FROM {TableName}
                   WHERE {IdColumn} IN @ids
                   """;

        await ExecuteAsync(sql, new { ids = list }, token);
    }
    
    private string BuildBulkUpdateSetClause() =>
        string.Join(",\n            ", InsertColumns
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(c => !c.Equals(IdColumn, StringComparison.OrdinalIgnoreCase))
            .Select(c => $"{c} = EXCLUDED.{c}"));
}

public abstract class KDataBulkRepository<TData>(DbConnectionFactory factory) : KDataBulkRepository<long, TData>(factory)
    where TData : IKData<long>;