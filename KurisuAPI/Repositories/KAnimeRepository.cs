using KurisuAPI.Factories;
using KurisuCore.Models;

namespace KurisuAPI.Repositories
{
    public sealed class KAnimeRepository(DbConnectionFactory factory) : KRepositoryBase<KAnime>(factory)
    {
        protected override string TableName => "animeinfo";

        public Task<List<KAnime>> SearchAsync(string query, int limit)
            => SearchByColumnAsync("title", query, limit);

        public async Task<KAnime> InsertAsync(string title, string? description)
        {
            const string sql = """
                INSERT INTO animeinfo (title, description)
                VALUES (@title, @description)
                RETURNING id
            """;

            int id = await InsertReturningAsync<int>(sql, new { title, description });

            return new KAnime(id, title, description ?? "No description.");
        }

        public async Task UpdateAsync(KAnime anime)
        {
            const string sql = """
                UPDATE animeinfo
                SET title = @Title,
                    description = @Description
                WHERE id = @Id
            """;

            string message = $"Failed to find anime {anime} in database.";

            await ExecuteOrThrowAsync(sql, anime, message);
        }
    }
}
