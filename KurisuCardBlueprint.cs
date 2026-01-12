using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public enum CardRarity
    {
        R = 1,
        SR = 2,
        SSR = 3,
        UR = 4
    }

    public class KurisuCardBlueprint
    {
        public int Id { get; private set; }
        public KurisuCharacter Character { get; private set; }
        public KurisuAnime Anime { get; private set; }
        public int? ImageKey { get; private set; }
        public CardRarity Rarity { get; private set; }
        public string Variant { get; private set; }
        public int Desires { get; private set; }

        public KurisuCardBlueprint(
            int id, 
            KurisuCharacter character, 
            KurisuAnime anime, 
            CardRarity rarity,
            string variant,
            int? imageKey,
            int desires = 0
        )
        {
            Id = id;
            Character = character;
            Anime = anime;
            Rarity = rarity;
            Variant = variant;
            ImageKey = imageKey;
            Desires = desires;
        }
    }

    public static class KurisuCardBlueprintCache
    {
        private static string _connectionString = "";
        private static ConcurrentDictionary<int, KurisuCardBlueprint> _cache = new();

        public static async Task InitializeAsync(string connectionString)
        {
            _connectionString = connectionString;
            await LoadAllAsync();
            Console.WriteLine("[KurisuCardBlueprintCache] Initialized");
        }

        public static KurisuCardBlueprint? SearchByID(int id)
        {
            _cache.TryGetValue(id, out var cardBlueprint);
            return cardBlueprint;
        }

        public static async Task<KurisuCardBlueprint?> AddCardBlueprintAsync(KurisuCharacter character, KurisuAnime anime, CardRarity rarity, string variant, int? imageKey = null)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO cardblueprint (characterid, animeid, imagekey, rarity, variant)
                    VALUES (@characterid, @animeid, @imagekey, @rarity, @variant)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@characterid", character.Id);
                command.Parameters.AddWithValue("@animeid", anime.Id);
                command.Parameters.AddWithValue("@imagekey", imageKey.HasValue ? imageKey.Value : DBNull.Value);
                command.Parameters.AddWithValue("@rarity", (short)rarity);
                command.Parameters.AddWithValue("@variant", variant);
                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuCardBlueprint cardBlueprint = new KurisuCardBlueprint(id, character, anime, rarity, variant, imageKey);

                if (!character.AddVariant(cardBlueprint))
                {
                    Console.WriteLine($"[KurisuCardBlueprintCache] Error adding card blueprint (ID: {id}) to '{character.Name}''s variants, returning null");
                    return null;
                }

                _cache.TryAdd(id, cardBlueprint);

                Console.WriteLine($"[KurisuCardBlueprintCache] Added card blueprint (ID: {id}) to Supabase");
                return cardBlueprint;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCardBlueprintCache] Error adding card blueprint to Supabase: {ex.Message}");
                throw;
            }
        }

        public static async Task UpdateCardBlueprintAsync(KurisuCardBlueprint cardBlueprint)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    UPDATE cardblueprint
                    SET characterid     = @characterid,
                        animeid         = @animeid,
                        imagekey        = @imagekey,
                        rarity          = @rarity,
                        variant         = @variant,
                        desires         = @desires
                    WHERE id = @id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@id", cardBlueprint.Id);
                command.Parameters.AddWithValue("@characterid", cardBlueprint.Character.Id);
                command.Parameters.AddWithValue("@animeid", cardBlueprint.Anime.Id);
                command.Parameters.AddWithValue("@imagekey", cardBlueprint.ImageKey.HasValue ? cardBlueprint.ImageKey.Value : DBNull.Value);
                command.Parameters.AddWithValue("@rarity", (short)cardBlueprint.Rarity);
                command.Parameters.AddWithValue("@variant", cardBlueprint.Variant);
                command.Parameters.AddWithValue("@desires", cardBlueprint.Desires);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"[KurisuCardBlueprintCache] Updated card blueprint (ID: {cardBlueprint.Id}) in Supabase");
                }
                else
                {
                    Console.WriteLine($"[KurisuCardBlueprintCache] No card blueprint found with ID: {cardBlueprint.Id} in Supabase");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCardBlueprintCache] Error updating card blueprint in Supabase: {ex.Message}");
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
                           characterid,
                           animeid,
                           imagekey,
                           rarity,
                           variant,
                           desires
                    FROM cardblueprint
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    int characterId = reader.GetInt32(reader.GetOrdinal("characterid"));
                    int animeId = reader.GetInt32(reader.GetOrdinal("animeid"));
                    int imageKeyOrdinal = reader.GetOrdinal("imagekey");
                    int? imageKey = reader.IsDBNull(imageKeyOrdinal)
                        ? null
                        : reader.GetInt32(imageKeyOrdinal);
                    int rarityValue = reader.GetInt16(reader.GetOrdinal("rarity"));
                    string variant = reader.GetString(reader.GetOrdinal("variant"));
                    int desires = reader.GetInt32(reader.GetOrdinal("desires"));

                    KurisuCharacter? character = KurisuCharacterCache.SearchByID(characterId);

                    if(character == null)
                    {
                        Console.WriteLine($"[KurisuCardBlueprintCache] No character exists in Supabase with ID: {characterId}, removing ID: {id} from Supabase");
                        continue;
                    }

                    KurisuAnime? anime = KurisuAnimeCache.SearchByID(animeId);

                    if (anime == null)
                    {
                        Console.WriteLine($"[KurisuCardBlueprintCache] No anime exists in Supabase with ID: {animeId}, removing ID: {id} from Supabase");
                        continue;
                    }

                    CardRarity rarity = (CardRarity)rarityValue;
                    KurisuCardBlueprint cardBlueprint = new KurisuCardBlueprint(id, character, anime, rarity, variant, imageKey, desires);

                    if(!character.AddVariant(cardBlueprint))
                    {
                        Console.WriteLine($"[KurisuCardBlueprintCache] Error adding card blueprint (ID: {id}) to '{character.Name}''s variants, skipping");
                        continue;
                    }

                    _cache.TryAdd(id, cardBlueprint);
                    
                }

                Console.WriteLine($"[KurisuCardBlueprintCache] Loaded {_cache.Count} card blueprints from Supabase");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuCardBlueprintCache] Error loading card blueprints from Supabase: {ex.Message}");
                throw;
            }
        }
    }
}
