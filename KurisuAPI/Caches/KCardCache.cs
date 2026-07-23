using KurisuAPI.Caches.Bases;
using KurisuCore.Models;

namespace KurisuAPI.Caches
{
    public sealed class KCardCache : KSaveCache<KCard>
    {
        protected override int CacheExpirationHours => 6;
    }
}
