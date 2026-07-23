using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;

namespace KurisuAPI.Repositories
{
    public sealed class KVariantPackLinkRepository(DbConnectionFactory factory) : KLinkRepository(factory)
    {
        protected override string TableName => "variant_pack";
        protected override string FirstColumn => "variant_id";
        protected override string SecondColumn => "pack_id";

        public Task<List<int>> GetByVariantIdAsync(int id) => GetByFirstColumnAsync(id);
        public Task<List<int>> GetByPackIdAsync(int id) => GetBySecondColumnAsync(id);
    }
}
