using KurisuAPI.Factories;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterAnimeLinkRepository(DbConnectionFactory factory) : KRepositoryBase(factory)
    {
        public async Task<List<int>> GetByAnimeIdAsync(int id)
        {
            const string sql = """
                SELECT character_id
                FROM character_anime
                WHERE anime_id = @id
            """;

            return await QueryAsync<int>(sql, new { id });
        }

        public async Task<List<int>> GetByCharacterIdAsync(int id)
        {
            const string sql = """
                SELECT anime_id
                FROM character_anime
                WHERE character_id = @id
            """;

            return await QueryAsync<int>(sql, new { id });
        }

        public async Task InsertAsync(int cid, int aid)
        {
            const string sql = """
                INSERT INTO character_anime (character_id, anime_id)
                VALUES (@cid, @aid)
                ON CONFLICT DO NOTHING
            """;

            await ExecuteAsync(sql, new { cid, aid });
        }

        public async Task DeleteAsync(int cid, int aid)
        {
            const string sql = """

            """;

            string message = $"Failed to find link between character (ID: {cid}) and anime (ID: {aid}).";

            await ExecuteOrThrowAsync(sql, new { cid, aid }, message);
        }
    }
}
