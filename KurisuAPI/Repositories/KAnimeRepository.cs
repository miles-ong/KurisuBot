using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Data;

namespace KurisuAPI.Repositories
{
    public sealed class KAnimeRepository(DbConnectionFactory factory) : KDataSearchRepository<KAnimeData>(factory)
    {
        protected override string TableName => "animeinfo";
        protected override string SearchColumn => "title";
        protected override string InsertColumns => "title, description";
        protected override string InsertValues => "@Title, @Description";
        protected override string UpdateQuery => """
                                                 UPDATE animeinfo
                                                 SET title       = @Title,
                                                     description = @Description
                                                 WHERE id = @Id
                                                 """;
    }
}
