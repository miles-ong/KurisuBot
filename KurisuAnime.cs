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
        public string? ImageKey { get; private set; }
        public List<KurisuCharacter> Characters { get; private set; } = new();

        public KurisuAnime(int id, string title, string description, string? imageKey = null)
        {
            Id = id;
            Title = title;
            Description = description;
            ImageKey = imageKey;
        }

        public async Task<bool> SetTitle(string newTitle)
        {
            string oldTitle = Title;
            Title = newTitle;

            try
            {
                await KurisuAnimeCache.UpdateAnime(this);
                return true;
            }
            catch
            {
                Title = oldTitle;
                return false;
            }
        }

        public async Task<bool> SetDescription(string newDescription)
        {
            string oldDescription = Description;
            Description = newDescription;

            try
            {
                await KurisuAnimeCache.UpdateAnime(this);
                return true;
            }
            catch
            {
                Description = oldDescription;
                return false;
            }
        }

        public async Task<bool> SetImageKey(string? newImageKey)
        {
            string? oldImageKey = ImageKey;
            ImageKey = newImageKey;

            try
            {
                await KurisuAnimeCache.UpdateAnime(this);
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

        public static async Task Initialize(string connectionString)
        {
            _connectionString = connectionString;
            await LoadAll();
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

            var results = allAnime
                .Select(anime => new
                {
                    Anime = anime,
                    Score = Fuzz.PartialRatio(title.ToLower(), anime.Title.ToLower())
                })
                .Where(x => x.Score > 60)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Anime)
                .ToList();

            return results;
        }

        public static async Task<KurisuAnime> Add(string title, string description, string? imageKey = null)
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
                command.Parameters.AddWithValue("@imagekey", imageKey ?? (object)DBNull.Value);

                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuAnime anime = new KurisuAnime(id, title, description, imageKey);
                _cache.TryAdd(id, anime);

                Console.WriteLine($"[KurisuAnimeCache] Added anime '{title}' with ID {id}");
                return anime;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error adding anime: {ex.Message}");
                throw;
            }
        }

        public static async Task UpdateAnime(KurisuAnime anime)
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
                command.Parameters.AddWithValue("@imagekey", anime.ImageKey ?? (object)DBNull.Value);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if(rowsAffected > 0)
                {
                    Console.WriteLine($"[KurisuAnimeCache] Updated anime '{anime.Title}' (ID: {anime.Id})");
                }
                else
                {
                    Console.WriteLine($"[KurisuAnimeCache] Warning: No anime found with ID {anime.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuAnimeCache] Error updating anime: {ex.Message}");
                throw;
            }
        }

        private static async Task LoadAll()
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
                    string? imageKey = reader.IsDBNull(imageKeyOrdinal)
                        ? null
                        : reader.GetString(imageKeyOrdinal);

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