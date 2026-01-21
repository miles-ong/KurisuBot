using FuzzySharp;
using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public class KurisuCharacter
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public IReadOnlyCollection<KurisuAnime> Animes => _animes;
        public IReadOnlyCollection<KurisuVariant> Variants => _variants;

        private readonly HashSet<KurisuAnime> _animes = new();

        private readonly HashSet<KurisuVariant> _variants = new();

        public KurisuCharacter(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public KurisuCharacter(int id)
        {
            Id = id;
            Name = "Unknown";
        }

        public async Task<KurisuResult> SetNameAsync(string newName)
        {
            string oldName = Name;
            Name = newName;

            KurisuResult result = await KurisuCharacterCache.UpdateCharacterAsync(this);

            if(result != KurisuResult.Completed)
            {
                Name = oldName;
            }

            return result;
        }

        internal void AddAnimeInternal(KurisuAnime anime)
        {
            _animes.Add(anime);
        }

        internal void RemoveAnimeInternal(KurisuAnime anime)
        {
            _animes.Remove(anime);
        }

        internal void AddVariantInternal(KurisuVariant variant)
        {
            _variants.Add(variant);
        }

        internal void RemoveVariantInternal(KurisuVariant variant)
        {
            _variants.Remove(variant);
        }

        internal string ConsoleToString()
        {
            return $"'{Name}' (ID: {Id})";
        }
    }

    public static class KurisuCharacterCache
    {
        private static string _connectionString = "";
        private static ConcurrentDictionary<int, KurisuCharacter> _cache = new();

        public static async Task InitializeAsync(string connectionString)
        {
            _connectionString = connectionString;
            await LoadAllAsync();
            Console.WriteLine("[KurisuCharacterCache] Initialized");
        }

        public static Result<KurisuCharacter> SearchByID(int id)
        {
            if(_cache.TryGetValue(id, out var character))
            {
                return Result<KurisuCharacter>.Ok(character);
            }

            return Result<KurisuCharacter>.Fail(KurisuResult.CannotFindCharacter);
        }

        public static Result<List<KurisuCharacter>> SearchByName(string name, int maxResults = 10)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return Result<List<KurisuCharacter>>.Fail(KurisuResult.InvalidCharacterName);
            }

            IEnumerable<KurisuCharacter> allCharacters = _cache.Values;

            if(!allCharacters.Any())
            {
                return Result<List<KurisuCharacter>>.Fail(KurisuResult.CacheEmpty);
            }

            List<KurisuCharacter> results = allCharacters
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score > 60)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Character)
                .ToList();

            if(!results.Any())
            {
                return Result<List<KurisuCharacter>>.Fail(KurisuResult.NoSimilarCharacterNames);
            }

            return Result<List<KurisuCharacter>>.Ok(results);
        }

        public static Result<KurisuCharacter> SearchByNameBestMatch(string name, int similarityStrength = 60)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return Result<KurisuCharacter>.Fail(KurisuResult.InvalidCharacterName);
            }

            KurisuCharacter? character = _cache.Values
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Character)
                .FirstOrDefault();

            if(character == null)
            {
                return Result<KurisuCharacter>.Fail(KurisuResult.NoSimilarCharacterNames);
            }

            return Result<KurisuCharacter>.Ok(character);
        }

        public static Result<(int, KurisuCharacter)> CheckForCharacterNameSimilarity(KurisuAnime anime, string name, int similarityStrength = 60)
        {
            if(string.IsNullOrEmpty(name))
            {
                return Result<(int, KurisuCharacter)>.Fail(KurisuResult.InvalidCharacterName);
            }

            var results = anime.Characters
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .ToList();

            if(!results.Any())
            {
                return Result<(int, KurisuCharacter)>.Fail(KurisuResult.NoSimilarCharacterNames);
            }

            var first = results.First();
            int similarityScore = first.Score;
            KurisuCharacter character = first.Character;

            return Result<(int, KurisuCharacter)>.Ok((similarityScore, character));
        }

        public static async Task<Result<KurisuCharacter>> AddCharacterAsync(string name, KurisuAnime anime)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return Result<KurisuCharacter>.Fail(KurisuResult.InvalidCharacterName);
            }

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO character (name, animeids)
                    VALUES (@name, @animeids)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);

                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@animeids", new int[] { anime.Id });

                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuCharacter character = new KurisuCharacter(id, name);

                _cache.TryAdd(id, character);
                character.AddAnimeInternal(anime);
                anime.AddCharacterInternal(character);

                Console.WriteLine($"[KurisuCharacterCache] Successfully added character {character.ConsoleToString()}");
                return Result<KurisuCharacter>.Ok(character);
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[KurisuCharacterCache] Error adding character '{name}': {ex.Message}");
                return Result<KurisuCharacter>.Fail(KurisuResult.DatabaseFailure);
            }
        }

        public static async Task<KurisuResult> UpdateCharacterAsync(KurisuCharacter character, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
        {
            NpgsqlConnection? connection = conn;
            NpgsqlTransaction? transaction = tran;
            bool ownsConnection = false;
            bool ownsTransaction = false;

            try
            {
                if(connection is null)
                {
                    connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();
                    ownsConnection = true;
                }

                if (transaction is null)
                {
                    transaction = await connection.BeginTransactionAsync();
                    ownsTransaction = true;
                }

                const string sqlQuery = @"
                    UPDATE character
                    SET name            = @name,
                        animeids        = @animeids
                    WHERE id = @id
                ";

                await using(var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", character.Id);
                    command.Parameters.AddWithValue("@name", character.Name);
                    command.Parameters.AddWithValue("@animeids", character.Animes.Select(a => a.Id).ToArray());

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected <= 0)
                    {
                        if(ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuCharacterCache] No character found with ID: {character.Id}");
                        return KurisuResult.CannotFindCharacter;
                    }
                }

                if(ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                Console.WriteLine($"[KurisuCharacterCache] Successfully updated character {character.ConsoleToString()}");
                return KurisuResult.Completed;
            }
            catch (PostgresException ex)
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                Console.WriteLine($"[KurisuCharacterCache] Error updating character {character.ConsoleToString()}: {ex.Message}");
                return KurisuResult.DatabaseFailure;
            }
            finally
            {
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if (ownsConnection && connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }
        
        public static async Task<KurisuResult> DeleteCharacterAsync(KurisuCharacter character, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
        {
            NpgsqlConnection? connection = conn;
            NpgsqlTransaction? transaction = tran;
            bool ownsConnection = false;
            bool ownsTransaction = false;

            try
            {
                if(connection is null)
                {
                    connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();
                    ownsConnection = true;
                }

                if(transaction is null)
                {
                    transaction = await connection.BeginTransactionAsync();
                    ownsTransaction = true;
                }

                const string sqlQuery = @"
                    DELETE FROM character
                    WHERE id = @id
                ";

                await using(var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", character.Id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if(rowsAffected <= 0)
                    {
                        if(ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuCharacterCache] No character found with ID: {character.Id}");
                        return KurisuResult.CannotFindCharacter;
                    }
                }

                if(ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                _cache.TryRemove(character.Id, out _);

                foreach(KurisuAnime anime in character.Animes.ToList())
                {
                    anime.RemoveCharacterInternal(character);
                }

                Console.WriteLine($"[KurisuCharacterCache] Successfully deleted character {character.ConsoleToString()}, removed from {character.Animes.Count} animes");
                return KurisuResult.Completed;
            }
            catch(PostgresException ex)
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                Console.WriteLine($"[KurisuCharacterCache] Error deleting character {character.ConsoleToString()}: {ex.Message}");
                return KurisuResult.DatabaseFailure;
            }
            finally
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if (ownsConnection && connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        private static async Task LoadAllAsync()
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    SELECT id,
                           name,
                           animeids
                    FROM character
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    string name = reader.GetString(reader.GetOrdinal("name"));
                    int[] animeIds = (int[])reader.GetValue(reader.GetOrdinal("animeids"));

                    KurisuCharacter character = new KurisuCharacter(id, name);
                    bool characterDirty = false;

                    foreach (int animeId in animeIds)
                    {
                        Result<KurisuAnime> animeResult = KurisuAnimeCache.SearchByID(animeId);

                        if(!animeResult.Success)
                        {
                            characterDirty = true;

                            Console.WriteLine($"[KurisuCharacterCache] No anime exists with ID: {animeId}, removing anime ID");
                            continue;
                        }

                        KurisuAnime anime = animeResult.Value;
                        character.AddAnimeInternal(anime);
                        anime.AddCharacterInternal(character);
                    }

                    if(characterDirty)
                    {
                        await UpdateCharacterAsync(character);
                    }

                    _cache.TryAdd(id, character);
                    Console.WriteLine($"[KurisuCharacterCache] Successfully loaded character {character.ConsoleToString()}, added to {character.Animes.Count} of {animeIds.Length} animes");
                }

                Console.WriteLine($"[KurisuCharacterCache] Successfully loaded {_cache.Count} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCharacterCache] Error loading characters: {ex.Message}");
                throw;
            }
        }
    }
}
