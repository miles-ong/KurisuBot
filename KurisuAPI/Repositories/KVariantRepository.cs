using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Data;

namespace KurisuAPI.Repositories
{
    public sealed class KVariantRepository(DbConnectionFactory factory) : KDataRepository<KVariantData>(factory)
    {
        protected override string TableName => "variantinfo";
        protected override string SelectColumns => "id, character_id AS CharacterId, anime_id AS AnimeId, rarity, title";
        protected override string InsertColumns => "character_id, anime_id, rarity, title, artist";
        protected override string InsertValues => "@CharacterId, @AnimeId, @Rarity::cardrarity, @Title, @Artist";
        protected override string UpdateQuery => """
                                                 UPDATE variantinfo
                                                 SET anime_id    = @AnimeId,
                                                     rarity      = @Rarity::cardrarity,
                                                     title       = @Title,
                                                     artist      = @Artist
                                                 WHERE id = @Id
                                                 """;

        public Task<List<KVariantData>> GetByCharacterIdAsync(int cId)
        {
            var sql = $"""
                       SELECT {SelectColumns} FROM variantinfo
                       WHERE character_id = @cId
                       """;

            return QueryAsync(sql, new { cId });
        }
    }
}
