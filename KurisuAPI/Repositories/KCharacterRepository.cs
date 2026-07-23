using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Data;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterRepository(DbConnectionFactory factory) : KDataSearchRepository<KCharacterData>(factory)
    {
        protected override string TableName => "characterinfo";
        protected override string SearchColumn => "name";
        protected override string InsertColumns => "name";
        protected override string InsertValues => "@Name";
        protected override string UpdateQuery => """
                                                 UPDATE characterinfo
                                                 SET name = @Name
                                                 WHERE id = @Id
                                                 """;
    }
}
