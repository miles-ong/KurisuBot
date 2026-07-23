using KurisuAPI.Caches.Bases;
using KurisuCore.Models;

namespace KurisuAPI.Caches
{
    public class KPackCache : KIndexedCache<KPack>
    {
        public bool TryGetByName(string name, out KPack? pack) => TryGetByKey(name, out pack);
        public void UpdateName(KPack pack, string oldName) => UpdateKey(pack, oldName);
        
        protected override string GetKey(KPack pack) => pack.Name;
    }
}
