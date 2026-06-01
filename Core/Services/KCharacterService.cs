using KurisuBot.Core.Infrastructure.Cache;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Infrastructure.Repositories;
using KurisuBot.Core.Models;
using Npgsql;

namespace KurisuBot.Core.Services
{
    public sealed class KCharacterService
    {
        private readonly KCharacterRepository _repository;
        private const string _consoleTag = "[KCharacterService]";

        public KCharacterService(string connectionString)
        {
            _repository = new KCharacterRepository(connectionString);
        }

        public async Task InitializeAsync()
        {
            List<KCharacter> all = await _repository.GetAllAsync();

            foreach(KCharacter character in all)
            {
                KCharacterCache.Add(character);
            }

            Console.WriteLine($"{_consoleTag} Initialized {all.Count} characters");
        }

        public bool TryGet(int id, out KCharacter? character)
        {
            return KCharacterCache.TryGet(id, out character);
        }

        public IEnumerable<KCharacter> Search(string query, int limit = 10)
        {
            return KCharacterCache.Search(query, limit);
        }

        public async Task<Result<KCharacter>> AddAsync(string name)
        {
            if (KCharacterCache.TryGetByName(name, out _)) return Result<KCharacter>.Fail(KResult.IdenticalCharacterName);

            try
            {
                KCharacter character = await _repository.InsertAsync(name);
                KCharacterCache.Add(character);

                return Result<KCharacter>.Ok(character);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                Console.WriteLine($"{_consoleTag} Duplicate character '{name}': {ex.Message}");

                return Result<KCharacter>.Fail(KResult.CharacterAlreadyExists);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to add character '{name}': {ex.Message}");

                return Result<KCharacter>.Fail(KResult.DatabaseFailure);
            }
        }

        public async Task<KResult> SetNameAsync(int id, string newName)
        {
            if (!KCharacterCache.TryGet(id, out KCharacter? character)) return KResult.CannotFindCharacter;

            string oldName = character!.Name;
            KResult modelResult = character.SetName(newName);

            if (modelResult != KResult.Completed) return modelResult;

            try
            {
                await _repository.UpdateAsync(character);
                KCharacterCache.UpdateName(character);

                return KResult.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{_consoleTag} {ex.Message}");

                return KResult.CannotFindCharacter;
            }
            catch (Exception ex)
            {
                character.SetName(oldName);
                Console.WriteLine($"{_consoleTag} Failed to update character name for {character}: {ex.Message}");

                return KResult.DatabaseFailure;
            }
        }

        public async Task<KResult> DeleteAsync(int id)
        {
            if (!KCharacterCache.TryGet(id, out KCharacter? character)) return KResult.CannotFindCharacter;

            try
            {
                await _repository.DeleteAsync(id);
                KCharacterCache.Remove(id);

                return KResult.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{_consoleTag} {ex.Message}");

                return KResult.CannotFindCharacter;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to delete character {character}: {ex.Message}");

                return KResult.DatabaseFailure;
            }
        }
    }
}
