using FuzzySharp;
using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public class KurisuAnime
    {
        public int Id { get; private set; }
        public string Title { get; private set; } = "";
        public string Description { get; private set; } = "";
        public int? ImageKey { get; private set; }
        public List<KurisuCharacter> Characters { get; private set; } = new();

        public KurisuAnime(int id, string title, string description, int? imageKey = null)
        {
            Id = id;
            Title = title;
            Description = description;
            ImageKey = imageKey;
        }

        public (int, KurisuCharacter)? CheckForCharacterNameSimilarity(string name, int similarityStrength = 60)
        {
            var results = Characters
                .Select(character => new
                {
                    Character = character,
                    Score = Fuzz.WeightedRatio(name.ToLower(), character.Name.ToLower())
                })
                .Where(x => x.Score > similarityStrength)
                .OrderByDescending(x => x.Score)
                .ToList();

            if(!results.Any())
            {
                return null;
            }

            int similarityScore = results.Select(x => x.Score).ToList().First();
            KurisuCharacter character = results.Select(x => x.Character).ToList().First();

            return (similarityScore, character);
        }

        public async Task<bool> SetTitleAsync(string newTitle)
        {
            string oldTitle = Title;
            Title = newTitle;

            try
            {
                await KurisuAnimeCache.UpdateAnimeAsync(this);
                return true;
            }
            catch
            {
                Title = oldTitle;
                return false;
            }
        }

        public async Task<bool> SetDescriptionAsync(string newDescription)
        {
            string oldDescription = Description;
            Description = newDescription;

            try
            {
                await KurisuAnimeCache.UpdateAnimeAsync(this);
                return true;
            }
            catch
            {
                Description = oldDescription;
                return false;
            }
        }

        public async Task<bool> SetImageKeyAsync(int? newImageKey)
        {
            int? oldImageKey = ImageKey;
            ImageKey = newImageKey;

            try
            {
                await KurisuAnimeCache.UpdateAnimeAsync(this);
                return true;
            }
            catch
            {
                newImageKey = oldImageKey;
                return false;
            }
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
        }

        public static KurisuAnime? SearchByID(int id)
        {
            _cache.TryGetValue(id, out var anime);
            return anime;
        }

        public static List<KurisuAnime> SearchByTitle(string title, int maxResults = 10)
        {
            IEnumerable<KurisuAnime> allAnime = _cache.Values;

            if(!allAnime.Any())
            {
                return new();
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

            return results;
        }

        public static KurisuAnime? SearchByTitleBestMatch(string title, int similarityStrength = 60)
        {
            return _cache.Values
                .Select(anime => new
                {
                    Anime = anime,
                    Score = Fuzz.WeightedRatio(title.ToLower(), anime.Title.ToLower())
                })
                .Where(x => x.Score >= similarityStrength)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Anime)
                .FirstOrDefault();
        }

        public static (int, KurisuAnime)? CheckForAnimeTitleSimilarity(string title, int similarityStrength = 60)
        {
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
                return null;
            }

            int similarityScore = results.Select(x => x.Score).ToList().First();
            KurisuAnime anime = results.Select(x => x.Anime).ToList().First();

            return (similarityScore, anime);
        }

        public static async Task<KurisuAnime> AddAnimeAsync(string title, string description, int? imageKey = null)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO anime (title, description, imagekey)
                    VALUES (@title, @description, @imagekey)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@imagekey", imageKey.HasValue ? imageKey.Value : DBNull.Value);
                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuAnime anime = new KurisuAnime(id, title, description, imageKey);
                _cache.TryAdd(id, anime);

                Console.WriteLine($"[KurisuAnimeCache] Added anime '{title}' (ID: {anime.Id})");
                return anime;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error adding anime: {ex.Message}");
                throw;
            }
        }

        public static async Task UpdateAnimeAsync(KurisuAnime anime)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    UPDATE anime
                    SET title = @title,
                        description = @description,
                        imagekey = @imagekey
                    WHERE id = @id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@id", anime.Id);
                command.Parameters.AddWithValue("@title", anime.Title);
                command.Parameters.AddWithValue("@description", anime.Description);
                command.Parameters.AddWithValue("@imagekey", anime.ImageKey.HasValue ? anime.ImageKey.Value : DBNull.Value);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if(rowsAffected > 0)
                {
                    Console.WriteLine($"[KurisuAnimeCache] Updated anime '{anime.Title}' (ID: {anime.Id})");
                }
                else
                {
                    Console.WriteLine($"[KurisuAnimeCache] No anime found with ID: {anime.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error updating anime: {ex.Message}");
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
                           title,
                           description,
                           imagekey
                    FROM anime
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    string title = reader.GetString(reader.GetOrdinal("title"));
                    string description = reader.GetString(reader.GetOrdinal("description"));
                    int imageKeyOrdinal = reader.GetOrdinal("imagekey");
                    int? imageKey = reader.IsDBNull(imageKeyOrdinal)
                        ? null
                        : reader.GetInt32(imageKeyOrdinal);

                    KurisuAnime anime = new KurisuAnime(id, title, description, imageKey);
                    _cache.TryAdd(id, anime);
                }

                Console.WriteLine($"[KurisuAnimeCache] Loaded {_cache.Count} animes from Supabase");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error loading anime from Supabase: {ex.Message}");
                throw;
            }
        }
    }
}