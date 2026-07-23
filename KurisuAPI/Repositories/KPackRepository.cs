using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Data;

namespace KurisuAPI.Repositories
{
    public sealed class KPackRepository(DbConnectionFactory factory) : KDataSearchRepository<KPackData>(factory)
    {
        protected override string TableName => "packinfo";
        protected override string SelectColumns =>
            "id, enabled, pack_type AS PackType, price, discount, fixed_card_count AS FixedCardCount, " +
            "sr_weight AS SrWeight, ssr_weight AS SsrWeight, ur_weight AS UrWeight";
        protected override string SearchColumn => "name";
        protected override string InsertColumns =>
            "enabled, name, pack_type, price, discount, fixed_card_count, sr_weight, ssr_weight, ur_weight";
        protected override string InsertValues =>
            "@Enabled, @Name, @PackType, @Discount, @FixedCardCount, @SrWeight, @SsrWeight, @UrWeight";
        protected override string UpdateQuery => """
                                                 UPDATE packinfo
                                                 SET enabled             = @Enabled,
                                                     name                = @Name,
                                                     pack_type           = @PackType,
                                                     price               = @Price,
                                                     discount            = @Discount,
                                                     fixed_card_count    = @FixedCardCount,
                                                     sr_weight           = @SrWeight,
                                                     ssr_weight          = @SsrWeight,
                                                     ur_weight           = @UrWeight
                                                 WHERE id = @Id
                                                 """;
    }
}
