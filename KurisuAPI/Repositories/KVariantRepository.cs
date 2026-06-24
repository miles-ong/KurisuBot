using KurisuAPI.Factories;
using KurisuAPI.Records;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuAPI.Repositories
{
    public sealed class KVariantRepository(DbConnectionFactory factory) : KRepositoryBase<KVariantRow>(factory)
    {
        protected override string TableName => "variantinfo";
        protected override string SelectColumns => "id, character_id AS CharacterId, anime_id AS AnimeId, rarity, title";

        public async Task<List<KVariantRow>> GetByCharacterIdAsync(int cid)
            => await QueryAsync($"SELECT {SelectColumns} FROM variantinfo WHERE character_id = @cid", new { cid }); 

        public async Task<int> InsertAsync(int cid, int aid, KRarity rarity, string title)
        {
            const string sql = """
                INSERT INTO variantinfo (character_id, anime_id, rarity, title)
                VALUES (@cid, @aid, @rarity::cardrarity, @title)
                RETURNING id
            """;

            return await InsertReturningAsync<int>(sql, new { cid, aid, rarity = rarity.ToString(), title });
        }

        public async Task UpdateAsync(KVariant variant)
        {
            const string sql = """
                UPDATE variantinfo
                SET anime_id    = @AnimeId,
                    rarity      = @Rarity::cardrarity,
                    title       = @Title
                WHERE id = @Id
            """;

            object parameters = new
            {
                variant.Id,
                AnimeId = variant.Anime.Id,
                Rarity = variant.Rarity.ToString(),
                variant.Title
            };

            string message = $"Failed to find variant {variant} in database.";

            await ExecuteOrThrowAsync(sql, parameters, message);
        }
    }
}
