using FuzzySharp;
using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public class KurisuCharacter
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public List<KurisuAnime> Animes { get; private set; }
        public int? ImageKey { get; private set; }
        public int Variants { get; set; }

        public KurisuCharacter(int id, string name, List<KurisuAnime> animes, int? imageKey = null)
        {
            Id = id;
            Name = name;
            Animes = animes;
            ImageKey = imageKey;
        }

        public async Task<bool> SetNameAsync(string newName)
        {
            string oldName = Name;
            Name = newName;

            try
            {
                await KurisuCharacterCache.UpdateCharacterAsync(this);
                return true;
            }
            catch
            {
                Name = newName;
                return false;
            }
        }

        public async Task<bool> AddAnimeAsync(KurisuAnime anime)
        {
            if(Animes.Contains(anime))
            {
                return false;
            }

            List<KurisuAnime> oldAnimes = [.. Animes];
            Animes.Add(anime);

            try
            {
                await KurisuCharacterCache.UpdateCharacterAsync(this);
                anime.Characters.Add(this);
                return true;
            }
            catch
            {
                Animes = oldAnimes;
                return false;
            }
        }

        public async Task<bool> RemoveAnimeAsync(KurisuAnime anime)
        {
            if(!Animes.Contains(anime) || Animes.Count <= 1)
            {
                return false;
            }

            List<KurisuAnime> oldAnimes = [.. Animes];
            Animes.Remove(anime);

            try
            {
                await KurisuCharacterCache.UpdateCharacterAsync(this);
                anime.Characters.Remove(this);
                return true;
            }
            catch
            {
                Animes = oldAnimes;
                return false;
            }
        }

        public async Task<bool> SetImageKeyAsync(int? newImageKey)
        {
            int? oldImageKey = ImageKey;
            ImageKey = newImageKey;

            try
            {
                await KurisuCharacterCache.UpdateCharacterAsync(this);
                return true;
            }
            catch
            {
                newImageKey = oldImageKey;
                return false;
            }
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

        public static KurisuCharacter? SearchByID(int id)
        {
            _cache.TryGetValue(id, out var character);
            return character;
        }

        public static List<KurisuCharacter> SearchByName(string name, int maxResults = 10)
        {
            IEnumerable<KurisuCharacter> allCharacters = _cache.Values;

            if(!allCharacters.Any())
            {
                return new();
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

            return results;
        }

        public static KurisuCharacter? SearchByNameBestMatch(string name, int similarityStrength = 60)
        {
            return _cache.Values
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Character)
                .FirstOrDefault();
        }

        public static async Task<KurisuCharacter> AddCharacterAsync(string name, KurisuAnime anime, int? imageKey = null)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO character (name, animeids, imagekey)
                    VALUES (@name, @animeids, @imagekey)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@animeids", new int[] { anime.Id });
                command.Parameters.AddWithValue("@imagekey", imageKey.HasValue ? imageKey.Value : DBNull.Value);
                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuCharacter character = new KurisuCharacter(id, name, new List<KurisuAnime> { anime }, imageKey);
                _cache.TryAdd(id, character);
                anime.Characters.Add(character);

                Console.WriteLine($"[KurisuAnimeCharacter] Added character '{name}' (ID: {character.Id})");
                return character;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCharacter] Error adding character: {ex.Message}");
                throw;
            }
        }

        public static async Task UpdateCharacterAsync(KurisuCharacter character)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    UPDATE character
                    SET name = @name,
                        animeids = @animeids,
                        imagekey = @imagekey
                    WHERE id = @id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@id", character.Id);
                command.Parameters.AddWithValue("@name", character.Name);
                command.Parameters.AddWithValue("@animeids", character.Animes.Select(a => a.Id).ToArray());
                command.Parameters.AddWithValue("@imagekey", character.ImageKey.HasValue ? character.ImageKey.Value : DBNull.Value);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"[KurisuCharacterCache] Updated character '{character.Name}' (ID: {character.Id})");
                }
                else
                {
                    Console.WriteLine($"[KurisuCharacterCache] No character found with ID: {character.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCharacterCache] Error updating character: {ex.Message}");
                throw;
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
                           animeids,
                           imagekey
                    FROM character
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    string name = reader.GetString(reader.GetOrdinal("name"));
                    int[] animeIds = (int[])reader.GetValue(reader.GetOrdinal("animeids"));
                    int imageKeyOrdinal = reader.GetOrdinal("imagekey");
                    int? imageKey = reader.IsDBNull(imageKeyOrdinal)
                        ? null
                        : reader.GetInt32(imageKeyOrdinal);

                    List<KurisuAnime> animes = new();

                    foreach(int animeId in animeIds)
                    {
                        KurisuAnime? anime = KurisuAnimeCache.SearchByID(animeId);

                        if(anime == null)
                        {
                            Console.WriteLine($"[KurisuCharacterCache] No anime exists in Supabase with ID: {animeId}, removing character '{name}' (ID: {id}) from Supabase");
                            continue;
                        }

                        animes.Add(anime);
                    }

                    KurisuCharacter character = new KurisuCharacter(id, name, animes, imageKey);
                    _cache.TryAdd(id, character);

                    foreach(KurisuAnime anime in animes)
                    {
                        anime.Characters.Add(character);
                    }
                }

                Console.WriteLine($"[KurisuCharacterCache] Loaded {_cache.Count} characters from Supabase");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCharacterCache] Error loading characters from Supabase: {ex.Message}");
                throw;
            }
        }
    }
}
