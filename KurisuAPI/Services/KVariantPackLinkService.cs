using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services
{
    public sealed class KVariantPackLinkService(
        KVariantPackLinkRepository repo,
        KEntityFactory factory) 
        : KLinkService<KVariant, KVariantData, KPack, KPackData>(repo, factory)
    {
        protected override string ModelName => "VariantPackLink";
        protected override string ConsoleTag => "[KVariantPackLinkService]";
        protected override KDetail NotFoundDetail => KDetail.VariantPackLinkNotFound;
        protected override KDetail FirstNotFoundDetail => KDetail.VariantNotFound;
        protected override KDetail SecondNotFoundDetail => KDetail.PackNotFound;
    }
}
