using KurisuAPI.Caches.Bases;
using KurisuCore.Models;

namespace KurisuAPI.Caches
{
    public sealed class KUserCache : KSaveCache<KUser>
    {
        protected override int CacheExpirationHours => 3;
    }
}
