using KurisuAPI.Factories;
using KurisuCore.Models;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterRepository(DbConnectionFactory factory) : KRepositoryBase<KCharacter>(factory)
    {
        protected override string TableName => "characterinfo";

        public Task<List<KCharacter>> SearchAsync(string query, int limit)
            => SearchByColumnAsync("name", query, limit);

        public async Task<KCharacter> InsertAsync(string name)
        {
            const string sql = """
                INSERT INTO characterinfo (name)
                VALUES (@name)
                RETURNING id
            """;

            int id = await InsertReturningAsync<int>(sql, new { name });

            return new KCharacter(id, name);
        }

        public async Task UpdateAsync(KCharacter character)
        {
            const string sql = """
                UPDATE characterinfo
                SET name = @Name
                WHERE id = @Id
            """;

            string message = $"Failed to find character {character} in database.";

            await ExecuteOrThrowAsync(sql, character, message);
        }
    }
}
