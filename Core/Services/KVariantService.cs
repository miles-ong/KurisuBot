using KurisuBot.Core.Infrastructure.Cache;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Infrastructure.Repositories;
using KurisuBot.Core.Models;
using Npgsql;

namespace KurisuBot.Core.Services
{
    public sealed class KVariantService
    {
        private readonly KVariantRepository _repository;
        private const string _consoleTag = "[KVariantService]";

        public KVariantService(string connectionString)
        {
            _repository = new KVariantRepository(connectionString);
        }

        public async Task InitializeAsync()
        {
            var all = await _repository.GetAllAsync();
            int count = 0;

            foreach(var (id, cid, aid, rarity, title) in all)
            {
                if(!KCharacterCache.TryGet(cid, out KCharacter? character))
                {
                    Console.WriteLine($"{_consoleTag} Skipping variant (ID: {id}), failed to find character (ID: {cid}) in cache.");
                    continue;
                }

                if(!KAnimeCache.TryGet(aid, out KAnime? anime))
                {
                    Console.WriteLine($"{_consoleTag} Skipping variant (ID: {id}), failed to find anime (ID: {aid}) in cache.");
                    continue;
                }

                if (!character!.ContainsAnime(anime!))
                {
                    Console.WriteLine($"{_consoleTag} Skipping variant (ID: {id}), character {character} is not from anime {anime}.");
                    continue;
                }

                KVariant variant = new KVariant(id, character, anime!, rarity, title);
                KVariantCache.Add(variant);
                character.AddVariant(variant);
                count++;
            }

            Console.WriteLine($"{_consoleTag} Initialized {count} variants!");
        }

        public bool TryGet(int id, out KVariant? variant)
        {
            return KVariantCache.TryGet(id, out variant);
        }

        public async Task<Result<KVariant>> AddAsync(int cid, int aid, KRarity rarity, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return Result<KVariant>.Fail(KResult.InvalidVariantTitle);
            if (!KCharacterCache.TryGet(cid, out KCharacter? character)) return Result<KVariant>.Fail(KResult.CannotFindCharacter);
            if (!KAnimeCache.TryGet(aid, out KAnime? anime)) return Result<KVariant>.Fail(KResult.CannotFindAnime);
            if (!character!.ContainsAnime(anime!)) return Result<KVariant>.Fail(KResult.CannotFindCharacterAnimeLink);

            try
            {
                int id = await _repository.InsertAsync(cid, aid, rarity, title);
                KVariant variant = new KVariant(id, character, anime!, rarity, title);
                KVariantCache.Add(variant);
                character.AddVariant(variant);

                return Result<KVariant>.Ok(variant);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                Console.WriteLine($"{_consoleTag} Duplicate variant (CID: {cid}) '{title}': {ex.Message}");

                return Result<KVariant>.Fail(KResult.VariantAlreadyExists);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to add variant (CID: {cid}) '{title}': {ex.Message}");

                return Result<KVariant>.Fail(KResult.DatabaseFailure);
            }
        }

        public async Task<KResult> SetRarityAsync(int id, KRarity newRarity)
        {
            if (!KVariantCache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

            KRarity oldRarity = variant!.Rarity;
            KResult modelResult = variant.SetRarity(newRarity);

            if (modelResult != KResult.Completed) return modelResult;

            try
            {
                await _repository.UpdateAsync(variant);

                return KResult.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{_consoleTag} {ex.Message}");

                return KResult.CannotFindVariant;
            }
            catch (Exception ex)
            {
                variant.SetRarity(oldRarity);
                Console.WriteLine($"{_consoleTag} Failed to update variant rarity for {variant}: {ex.Message}");

                return KResult.DatabaseFailure;
            }
        }

        public async Task<KResult> SetTitleAsync(int id, string newTitle)
        {
            if (!KVariantCache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

            string oldTitle = variant!.Title;
            KResult modelResult = variant.SetTitle(newTitle);

            if(modelResult != KResult.Completed) return modelResult; try
            {
                await _repository.UpdateAsync(variant);

                return KResult.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{_consoleTag} {ex.Message}");

                return KResult.CannotFindVariant;
            }
            catch (Exception ex)
            {
                variant.SetTitle(oldTitle);
                Console.WriteLine($"{_consoleTag} Failed to update variant title for {variant}: {ex.Message}");

                return KResult.DatabaseFailure;
            }
        }

        public async Task<KResult> DeleteAsync(int id)
        {
            if (!KVariantCache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

            try
            {
                await _repository.DeleteAsync(id);
                KVariantCache.Remove(id);
                variant!.Character.RemoveVariant(variant);

                return KResult.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{_consoleTag} {ex.Message}");

                return KResult.CannotFindVariant;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to delete variant {variant}: {ex.Message}");

                return KResult.DatabaseFailure;
            }
        }
    }
}
