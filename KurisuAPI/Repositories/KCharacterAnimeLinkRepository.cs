using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterAnimeLinkRepository(DbConnectionFactory factory) : KLinkRepository(factory)
    {
        protected override string TableName => "character_anime";
        protected override string FirstColumn => "character_id";
        protected override string SecondColumn => "anime_id";

        public Task<List<int>> GetByAnimeIdAsync(int id) => GetByFirstColumnAsync(id);
        public Task<List<int>> GetByCharacterIdAsync(int id) => GetBySecondColumnAsync(id);
    }
}
