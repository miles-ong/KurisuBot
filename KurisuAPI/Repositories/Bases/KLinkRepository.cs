using KurisuAPI.Factories;

namespace KurisuAPI.Repositories.Bases
{
    public abstract class KLinkRepository<TFirstId, TSecondId>(DbConnectionFactory factory) : KRepository(factory)
        where TFirstId : IEquatable<TFirstId>
        where TSecondId : IEquatable<TSecondId>
    {
        protected abstract string FirstColumn { get; }
        protected abstract string SecondColumn { get; }
        
        public Task InsertAsync(TFirstId id1, TSecondId id2)
        {
            var sql = $"""
                       INSERT INTO {TableName} ({FirstColumn}, {SecondColumn})
                       VALUES (@id1, @id2)
                       ON CONFLICT DO NOTHING
                       """;

            return ExecuteAsync(sql, new { id1, id2 });
        }

        public Task DeleteAsync(TFirstId id1, TSecondId id2)
        {
            var sql = $"""
                       DELETE FROM {TableName}
                       WHERE {FirstColumn} = @id1 AND {SecondColumn} = @id2
                       """;

            return ExecuteOrThrowAsync(sql, new { id1, id2 },
                $"Failed to find link in {TableName} between {FirstColumn} (ID: {id1}) and {SecondColumn} (ID: {id2}).");
        }

        protected Task<List<TSecondId>> GetByFirstColumnAsync(TFirstId id)
        {
            var sql = $"""
                       SELECT {FirstColumn}
                       FROM {TableName}
                       WHERE {SecondColumn} = @id
                       """;

            return QueryAsync<TSecondId>(sql, new { id });
        }

        protected Task<List<TFirstId>> GetBySecondColumnAsync(TSecondId id)
        {
            var sql = $"""
                       SELECT {SecondColumn}
                       FROM {TableName}
                       WHERE {SecondColumn} = @id
                       """;

            return QueryAsync<TFirstId>(sql, new { id });
        }
    }

    public abstract class KLinkRepository(DbConnectionFactory factory) : KLinkRepository<int, int>(factory);
}
