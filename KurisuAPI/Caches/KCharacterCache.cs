using KurisuAPI.Caches.Bases;
using KurisuCore.Models;

namespace KurisuAPI.Caches
{
    public sealed class KCharacterCache : KIndexedCache<KCharacter>
    {
        public bool TryGetByName(string name, out KCharacter? character) => TryGetByKey(name, out character);
        public void UpdateName(KCharacter character, string oldName) => UpdateKey(character, oldName);
        
        protected override string GetKey(KCharacter character) => character.Name;
    }
}
