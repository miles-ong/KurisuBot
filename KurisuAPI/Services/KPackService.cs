using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services
{
    public sealed class KPackService(
        KPackRepository repo,
        KPackCache cache,
        KEntityFactory factory) 
        : KDataSearchService<KPack, KPackData>(repo, cache, factory)
    {
        protected override string ModelName => nameof(KPack);
        protected override string ConsoleTag => "[KPackService]";
        protected override KDetail NotFoundDetail => KDetail.PackNotFound;

        public Task<KDetail> SetEnabledAsync(int id, bool newEnabled) => SetAsync(
            id,
            p => p.Enabled,
            (p, v) => p.Enabled = v,
            newEnabled,
            "enabled state");

        public Task<KDetail> SetNameAsync(int id, string newName) => SetAsync(
            id,
            p => p.Name,
            (p, v) => p.Name = v,
            newName.Trim(),
            "name",
            validate: (_, v) => string.IsNullOrWhiteSpace(v) ? KDetail.InvalidPackName : KDetail.Completed,
            onPersisted: cache.UpdateName);

        public Task<KDetail> SetTypeAsync(int id, KPackType newPackType) => SetAsync(
            id,
            p => p.PackType,
            (p, v) => p.PackType = v,
            newPackType,
            "type");

        public Task<KDetail> SetPriceAsync(int id, int newPrice) => SetAsync(
            id,
            p => p.Price,
            (p, v) => p.Price = v,
            newPrice,
            "price",
            validate: (_, v) => v < 0 ? KDetail.InvalidPackPrice : KDetail.Completed);

        public Task<KDetail> SetDiscountAsync(int id, int newDiscount) => SetAsync(
            id,
            p => p.Discount,
            (p, v) => p.Discount = v,
            newDiscount,
            "discount",
            validate: (_, v) => v < 0 ? KDetail.InvalidPackDiscount : KDetail.Completed);

        public Task<KDetail> SetFixedCardCountAsync(int id, int newFixedCardCount) => SetAsync(
            id,
            p => p.FixedCardCount,
            (p, v) => p.FixedCardCount = v,
            newFixedCardCount,
            "fixed card count",
            validate: (_, v) => v <= 0 ? KDetail.InvalidPackFixedCardCount : KDetail.Completed);

        public Task<KDetail> SetSrWeightAsync(int id, int newSrWeight) => SetAsync(
            id,
            p => p.SrWeight,
            (p, v) => p.SrWeight = v,
            newSrWeight,
            "sr weight",
            validate: (p, v) => ValidateWeights(v, p.SsrWeight, p.UrWeight));
        
        public Task<KDetail> SetSsrWeightAsync(int id, int newSsrWeight) => SetAsync(
            id,
            p => p.SsrWeight,
            (p, v) => p.SsrWeight = v,
            newSsrWeight,
            "ssr weight",
            validate: (p, v) => ValidateWeights(p.SrWeight, v, p.UrWeight));
        
        public Task<KDetail> SetUrWeightAsync(int id, int newUrWeight) => SetAsync(
            id,
            p => p.UrWeight,
            (p, v) => p.UrWeight = v,
            newUrWeight,
            "ur weight",
            validate: (p, v) => ValidateWeights(p.SrWeight, p.SsrWeight, v));

        private static KDetail ValidateWeights(int sr, int ssr, int ur)
        {
            var under = sr < 0 || ssr < 0 || ur < 0;
            var over = sr + ssr + ur > 100;

            return under || over ? KDetail.InvalidPackWeights : KDetail.Completed;
        }
    }
}
