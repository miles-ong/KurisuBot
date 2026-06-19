using KurisuAPI.Cache;
using KurisuAPI.Records;
using KurisuAPI.Repositories;
using KurisuCore.Models;

namespace KurisuAPI.Services
{
    public sealed class KEntityLoader(
        KAnimeRepository animeRepository,
        KCharacterRepository characterRepository,
        KVariantRepository variantRepository,
        KCharacterAnimeLinkRepository linkRepository,
        KAnimeCache animeCache,
        KCharacterCache characterCache,
        KVariantCache variantCache)
    {
        public async Task<KAnime?> LoadAnimeAsync(int id)
        {
            if (animeCache.TryGet(id, out KAnime? cached)) return cached;

            KAnime? anime = await animeRepository.GetByIdAsync(id);

            if (anime is null) return null;

            animeCache.Add(anime);
            await LoadCharacterLinksAsync(anime);

            return anime;
        }

        public async Task<KCharacter?> LoadCharacterAsync(int id)
        {
            if (characterCache.TryGet(id, out KCharacter? cached)) return cached;

            KCharacter? character = await characterRepository.GetByIdAsync(id);

            if (character is null) return null;

            characterCache.Add(character);
            await LoadAnimeLinksAsync(character);
            await LoadVariantsAsync(character);

            return character;
        }

        private async Task LoadAnimeLinksAsync(KCharacter character)
        {
            List<int> aids = await linkRepository.GetByCharacterIdAsync(character.Id);

            foreach (int aid in aids)
            {
                KAnime? anime = await LoadAnimeAsync(aid);

                if (anime is null) continue;

                anime.LinkCharacter(character);
                character.LinkAnime(anime);
            }
        }

        private async Task LoadCharacterLinksAsync(KAnime anime)
        {
            List<int> cids = await linkRepository.GetByAnimeIdAsync(anime.Id);

            foreach(int cid in cids)
            {
                KCharacter? character = await LoadCharacterAsync(cid);

                if (character is null) continue;

                anime.LinkCharacter(character);
                character.LinkAnime(anime);
            }
        }

        private async Task LoadVariantsAsync(KCharacter character)
        {
            List<KVariantRow> rows = await variantRepository.GetByCharacterIdAsync(character.Id);

            foreach (KVariantRow r in rows)
            {
                KAnime? anime = await LoadAnimeAsync(r.AnimeId);

                if (anime is null) continue;

                KVariant variant = new(r.Id, character, anime, r.Rarity, r.Title);
                variantCache.Add(variant);
                character.AddVariant(variant);
            }
        }
    }
}
