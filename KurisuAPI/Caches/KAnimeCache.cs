using KurisuAPI.Caches.Bases;
using KurisuCore.Models;

namespace KurisuAPI.Caches
{
    public sealed class KAnimeCache : KIndexedCache<KAnime>
    {
        public bool TryGetByTitle(string title, out KAnime? anime) => TryGetByKey(title, out anime);
        public void UpdateTitle(KAnime anime, string oldTitle) => UpdateKey(anime, oldTitle);
        
        protected override string GetKey(KAnime anime) => anime.Title;
    }
}