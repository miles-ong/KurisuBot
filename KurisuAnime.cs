using FuzzySharp;
using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public class KurisuAnime
    {
        public int Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public IReadOnlyCollection<KurisuCharacter> Characters => _characters;
        public IReadOnlyCollection<KurisuVariant> Variants => _characters
            .SelectMany(c => c.Variants)
            .Where(v => v.Anime.Equals(this))
            .Distinct()
            .ToList()
            .AsReadOnly();
            
        private readonly HashSet<KurisuCharacter> _characters = new();
        
        public KurisuAnime(int id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }

        public KurisuAnime(int id)
        {
            Id = id;
            Title = "Unknown";
            Description = "Unknown";
        }

        public async Task<KurisuResult> SetTitleAsync(string newTitle)
        {
            string oldTitle = Title;
            Title = newTitle;

            KurisuResult result = await KurisuAnimeCache.UpdateAnimeAsync(this);

            if(result != KurisuResult.Completed)
            {
                Title = oldTitle;
            }

            return result;
        }

        public async Task<KurisuResult> SetDescriptionAsync(string newDescription)
        {
            string oldDescription = Description;
            Description = newDescription;

            KurisuResult result = await KurisuAnimeCache.UpdateAnimeAsync(this);

            if(result != KurisuResult.Completed)
            {
                Description = oldDescription;
            }

            return result;
        }

        public async Task<KurisuResult> AddCharacterAsync(KurisuCharacter character)
        {
            if (_characters.Contains(character))
            {
                return KurisuResult.AnimeAlreadyContainsCharacter;
            }

            character.AddAnimeInternal(this);

            KurisuResult result = await KurisuCharacterCache.UpdateCharacterAsync(character);

            if (result != KurisuResult.Completed)
            {
                character.RemoveAnimeInternal(this);

                return result;
            }

            _characters.Add(character);

            return KurisuResult.Completed;
        }

        public async Task<KurisuResult> RemoveCharacterAsync(KurisuCharacter character, bool allowOrphan = false)
        {
            if (!_characters.Contains(character))
            {
                return KurisuResult.AnimeDoesNotContainCharacter;
            }

            if(character.Animes.Count <= 1)
            {
                if(allowOrphan)
                {
                    KurisuResult deleteCharacterResult = await KurisuCharacterCache.DeleteCharacterAsync(character);

                    return deleteCharacterResult == KurisuResult.Completed
                        ? KurisuResult.DeletingCharacter
                        : deleteCharacterResult;
                }

                return KurisuResult.CharacterCannotBeOrphaned;
            }

            character.RemoveAnimeInternal(this);

            KurisuResult result = await KurisuCharacterCache.UpdateCharacterAsync(character);

            if (result != KurisuResult.Completed)
            {
                character.AddAnimeInternal(this);

                return result;
            }

            _characters.Remove(character);

            return KurisuResult.Completed;
        }

        public Result<(int, KurisuCharacter)> CheckForCharacterNameSimilarity(string name, int similarityStrength = 60)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<(int, KurisuCharacter)>.Fail(KurisuResult.InvalidCharacterName);
            }

            var results = _characters
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score > similarityStrength)
                .OrderByDescending(x => x.Score)
                .ToList();

            if (!results.Any())
            {
                return Result<(int, KurisuCharacter)>.Fail(KurisuResult.NoSimilarCharacterNames);
            }

            var first = results.First();
            int similarityScore = first.Score;
            KurisuCharacter character = first.Character;

            return Result<(int, KurisuCharacter)>.Ok((similarityScore, character));
        }

        internal void AddCharacterInternal(KurisuCharacter character)
        {
            _characters.Add(character);
        }

        internal void RemoveCharacterInternal(KurisuCharacter character)
        {
            _characters.Remove(character);
        }

        internal string ConsoleToString()
        {
            return $"'{Title}' (ID: {Id})";
        }
    }

    public static class KurisuAnimeCache
    {
        private static string _connectionString = "";
        private static ConcurrentDictionary<int, KurisuAnime> _cache = new();

        public static async Task InitializeAsync(string connectionString)
        {
            _connectionString = connectionString;
            await LoadAllAsync();
            Console.WriteLine("[KurisuAnimeCache] Initialized");
        }

        public static Result<KurisuAnime> SearchByID(int id)
        {
            if(_cache.TryGetValue(id, out var anime))
            {
                return Result<KurisuAnime>.Ok(anime);
            }

            return Result<KurisuAnime>.Fail(KurisuResult.CannotFindAnime);
        }

        public static Result<List<KurisuAnime>> SearchByTitle(string title, int maxResults = 10)
        {
            if(string.IsNullOrWhiteSpace(title))
            {
                return Result<List<KurisuAnime>>.Fail(KurisuResult.InvalidAnimeTitle);
            }

            IEnumerable<KurisuAnime> allAnime = _cache.Values;

            if(!allAnime.Any())
            {
                return Result<List<KurisuAnime>>.Fail(KurisuResult.CacheEmpty);
            }

            List<KurisuAnime> results = allAnime
                .Select(anime => new
                {
                    Anime = anime,
                    Score = Fuzz.WeightedRatio(title.ToLower(), anime.Title.ToLower())
                })
                .Where(x => x.Score > 60)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Anime)
                .ToList();

            if(!results.Any())
            {
                return Result<List<KurisuAnime>>.Fail(KurisuResult.NoSimilarAnimeTitles);
            }

            return Result<List<KurisuAnime>>.Ok(results);
        }

        public static Result<KurisuAnime> SearchByTitleBestMatch(string title, int similarityStrength = 60)
        {
            if(string.IsNullOrWhiteSpace(title))
            {
                return Result<KurisuAnime>.Fail(KurisuResult.InvalidAnimeTitle);
            }

            KurisuAnime? anime = _cache.Values
                .Select(anime => new
                {
                    Anime = anime,
                    Score = Fuzz.WeightedRatio(title.ToLower(), anime.Title.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Anime)
                .FirstOrDefault();

            if(anime == null)
            {
                return Result<KurisuAnime>.Fail(KurisuResult.NoSimilarAnimeTitles);
            }

            return Result<KurisuAnime>.Ok(anime);
        }

        public static Result<(int, KurisuAnime)> CheckForAnimeTitleSimilarity(string title, int similarityStrength = 60)
        {
            if(string.IsNullOrWhiteSpace(title))
            {
                return Result<(int, KurisuAnime)>.Fail(KurisuResult.InvalidAnimeTitle);
            }

            var results = _cache.Values
                .Select(anime => new
                {
                    Anime = anime,
                    Score = Fuzz.WeightedRatio(title.ToLower(), anime.Title.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .ToList();

            if(!results.Any())
            {
                return Result<(int, KurisuAnime)>.Fail(KurisuResult.NoSimilarAnimeTitles);
            }

            var first = results.First();
            int similarityScore = first.Score;
            KurisuAnime anime = first.Anime;

            return Result<(int, KurisuAnime)>.Ok((similarityScore, anime));
        }

        public static async Task<Result<KurisuAnime>> AddAnimeAsync(string title, string description)
        {
            if(string.IsNullOrWhiteSpace(title))
            {
                return Result<KurisuAnime>.Fail(KurisuResult.InvalidAnimeTitle);
            }

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO anime (title, description)
                    VALUES (@title, @description)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);

                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@description", description);

                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuAnime anime = new KurisuAnime(id, title, description);

                _cache.TryAdd(id, anime);

                Console.WriteLine($"[KurisuAnimeCache] Successfully added anime {anime.ConsoleToString()}");
                return Result<KurisuAnime>.Ok(anime);
            }
            catch(PostgresException ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error adding anime '{title}': {ex.Message}");
                return Result<KurisuAnime>.Fail(KurisuResult.DatabaseFailure);
            }
        }

        public static async Task<KurisuResult> UpdateAnimeAsync(KurisuAnime anime, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
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
                    UPDATE anime
                    SET title           = @title,
                        description     = @description
                    WHERE id = @id
                ";

                await using (var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", anime.Id);
                    command.Parameters.AddWithValue("@title", anime.Title);
                    command.Parameters.AddWithValue("@description", anime.Description);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if(rowsAffected <= 0)
                    {
                        if(ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuAnimeCache] No anime found with ID: {anime.Id}");
                        return KurisuResult.CannotFindAnime;
                    }
                }

                if(ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                Console.WriteLine($"[KurisuAnimeCache] Successfully updated anime {anime.ConsoleToString()}");
                return KurisuResult.Completed;
            }
            catch (PostgresException ex)
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                Console.WriteLine($"[KurisuAnimeCache] Error updating anime {anime.ConsoleToString()}: {ex.Message}");
                return KurisuResult.DatabaseFailure;
            }
            finally
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if(ownsConnection && connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public static async Task<KurisuResult> DeleteAnimeAsync(KurisuAnime anime, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
        {
            NpgsqlConnection? connection = conn;
            NpgsqlTransaction? transaction = tran;
            bool ownsConnection = false;
            bool ownsTransaction = false;
            List<KurisuCharacter> internalCharacterFallback = anime.Characters.ToList();
            List<KurisuVariant> internalVariantFallback = anime.Variants.ToList();

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
                    DELETE FROM anime
                    WHERE id = @id
                ";

                await using (var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", anime.Id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if(rowsAffected <= 0)
                    {
                        if(ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuAnimeCache] No anime found with ID: {anime.Id}");
                        return KurisuResult.CannotFindAnime;
                    }
                }

                foreach(KurisuCharacter character in anime.Characters.ToList())
                {
                    character.RemoveAnimeInternal(anime);
                    internalCharacterFallback.Add(character);

                    if(character.Animes.Count <= 0)
                    {
                        KurisuResult deleteCharacterResult = await KurisuCharacterCache.DeleteCharacterAsync(character, connection, transaction);

                        if(deleteCharacterResult != KurisuResult.Completed)
                        {
                            if(ownsTransaction)
                            {
                                await transaction.RollbackAsync();
                            }

                            foreach(KurisuCharacter fChar in internalCharacterFallback)
                            {
                                fChar.AddAnimeInternal(anime);
                            }

                            return deleteCharacterResult;
                        }
                    }
                    else
                    {
                        KurisuResult updateCharacterResult = await KurisuCharacterCache.UpdateCharacterAsync(character, connection, transaction);

                        if (updateCharacterResult != KurisuResult.Completed)
                        {
                            if(ownsTransaction)
                            {
                                await transaction.RollbackAsync();
                            }

                            foreach (KurisuCharacter fChar in internalCharacterFallback)
                            {
                                fChar.AddAnimeInternal(anime);
                            }

                            return updateCharacterResult;
                        }
                    }
                }

                if(ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                _cache.TryRemove(anime.Id, out _);

                Console.WriteLine($"[KurisuAnimeCache] Successfully deleted anime {anime.ConsoleToString()}, removed from {internalCharacterFallback.Count} characters");
                return KurisuResult.Completed;
            }
            catch(PostgresException ex)
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                foreach (KurisuCharacter fChar in internalCharacterFallback)
                {
                    fChar.AddAnimeInternal(anime);
                }

                Console.WriteLine($"[KurisuAnimeCache] Error deleting anime {anime.ConsoleToString()}: {ex.Message}");
                return KurisuResult.DatabaseFailure;
            }
            finally
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if(ownsConnection && connection is not null)
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
                           title,
                           description
                    FROM anime
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    string title = reader.GetString(reader.GetOrdinal("title"));
                    string description = reader.GetString(reader.GetOrdinal("description"));

                    KurisuAnime anime = new KurisuAnime(id, title, description);
                    _cache.TryAdd(id, anime);
                }

                Console.WriteLine($"[KurisuAnimeCache] Successfully loaded {_cache.Count} animes");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error loading animes: {ex.Message}");
                throw;
            }
        }
    }
}