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

    public class KurisuVariant
    {
        public int Id { get; private set; }
        public KurisuCharacter Character { get; private set; }
        public KurisuAnime Anime { get; private set; }
        public CardRarity Rarity { get; private set; }
        public string Title { get; private set; }

        public KurisuVariant(
            int id, 
            KurisuCharacter character, 
            KurisuAnime anime, 
            CardRarity rarity,
            string title
        )
        {
            Id = id;
            Character = character;
            Anime = anime;
            Rarity = rarity;
            Title = title;
        }

        public KurisuVariant(int id)
        {
            Id = id;
            Character = new KurisuCharacter(0);
            Anime = new KurisuAnime(0);
            Rarity = CardRarity.R;
            Title = "Unknown";
        }

        public async Task<KurisuResult> SetCharacterAsync(KurisuCharacter character, KurisuAnime? origin = null)
        {
            if(Character.Equals(character))
            {
                return KurisuResult.VariantCharacterAlreadyMatches;
            }

            KurisuCharacter oldCharacter = Character;
            KurisuAnime oldAnime = Anime;

            if (origin is null && !character.Animes.Contains(Anime))
            {
                Anime = character.Animes.First();
            }
            else if(origin is not null)
            {
                if(!character.Animes.Contains(origin))
                {
                    return KurisuResult.CharacterIsNotFromAnime;
                }

                Anime = origin;
            }

            KurisuResult result = await KurisuVariantCache.UpdateVariantAsync(this);

            if(result != KurisuResult.Completed)
            {
                Character = oldCharacter;
                Anime = oldAnime;

                return result;
            }

            Character.RemoveVariantInternal(this);
            Character = character;
            character.AddVariantInternal(this);

            return KurisuResult.Completed;
        }

        public async Task<KurisuResult> SetAnimeAsync(KurisuAnime anime)
        {
            if(Anime.Equals(anime))
            {
                return KurisuResult.VariantAnimeAlreadyMatches;
            }

            if(!Character.Animes.Contains(anime))
            {
                return KurisuResult.CharacterIsNotFromAnime;
            }

            KurisuAnime oldAnime = Anime;
            Anime = anime;
            KurisuResult result = await KurisuVariantCache.UpdateVariantAsync(this);

            if(result != KurisuResult.Completed)
            {
                Anime = oldAnime;
            }

            return result;
        }

        public async Task<KurisuResult> SetRarityAsync(CardRarity rarity)
        {
            if(Rarity == rarity)
            {
                return KurisuResult.VariantRarityAlreadyMatches;
            }

            CardRarity oldRarity = Rarity;
            Rarity = rarity;
            KurisuResult result = await KurisuVariantCache.UpdateVariantAsync(this);

            if(result != KurisuResult.Completed)
            {
                Rarity = oldRarity;
            }

            return result;
        }

        public async Task<KurisuResult> SetTitleAsync(string title)
        {
            if(Title.Equals(title))
            {
                return KurisuResult.VariantTitleAlreadyMatches;
            }

            string oldTitle = Title;
            Title = title;
            KurisuResult result = await KurisuVariantCache.UpdateVariantAsync(this);

            if(result != KurisuResult.Completed)
            {
                Title = oldTitle;
            }

            return result;
        }

        internal string ConsoleToString()
        {
            return $"{Rarity.ToString()} [{Title}] {Character.Name} (ID: {Id})";
        }
    }

    public static class KurisuVariantCache
    {
        private static string _connectionString = "";
        private static ConcurrentDictionary<int, KurisuVariant> _cache = new();

        public static async Task InitializeAsync(string connectionString)
        {
            _connectionString = connectionString;
            await LoadAllAsync();
            Console.WriteLine("[KurisuVariantCache] Initialized");
        }

        public static KurisuVariant? SearchByID(int id)
        {
            _cache.TryGetValue(id, out var cardBlueprint);

            return cardBlueprint;
        }

        public static async Task<Result<KurisuVariant>> AddVariantAsync(KurisuCharacter character, KurisuAnime anime, CardRarity rarity, string title)
        {
            if(!character.Animes.Contains(anime))
            {
                return Result<KurisuVariant>.Fail(KurisuResult.CharacterIsNotFromAnime);
            }

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    INSERT INTO variant (characterid, animeid, rarity, title)
                    VALUES (@characterid, @animeid, @rarity, @title)
                    RETURNING id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);

                command.Parameters.AddWithValue("@characterid", character.Id);
                command.Parameters.AddWithValue("@animeid", anime.Id);
                command.Parameters.AddWithValue("@rarity", (short)rarity);
                command.Parameters.AddWithValue("@title", title);

                long idLong = (long)(await command.ExecuteScalarAsync())!;
                int id = (int)idLong;

                KurisuVariant variant = new KurisuVariant(id, character, anime, rarity, title);
                character.AddVariantInternal(variant);
                _cache.TryAdd(id, variant);

                Console.WriteLine($"[KurisuVariantCache] Successfully added variant (ID: {id})");
                return Result<KurisuVariant>.Ok(variant);
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[KurisuVariantCache] Error adding variant: {ex.Message}");
                return Result<KurisuVariant>.Fail(KurisuResult.DatabaseFailure);
            }
        }

        public static async Task<KurisuResult> UpdateVariantAsync(KurisuVariant variant, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
        {
            NpgsqlConnection? connection = conn;
            NpgsqlTransaction? transaction = tran;
            bool ownsConnection = false;
            bool ownsTransaction = false;

            try
            {
                if (connection is null)
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
                    UPDATE variant
                    SET characterid     = @characterid,
                        animeid         = @animeid,
                        rarity          = @rarity,
                        title           = @title
                    WHERE id = @id
                ";

                await using (var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", variant.Id);
                    command.Parameters.AddWithValue("@characterid", variant.Character.Id);
                    command.Parameters.AddWithValue("@animeid", variant.Anime.Id);
                    command.Parameters.AddWithValue("@rarity", (short)variant.Rarity);
                    command.Parameters.AddWithValue("@title", variant.Title);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected <= 0)
                    {
                        if (ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuVariantCache] No variant found with ID: {variant.Id}");
                        return KurisuResult.CannotFindVariant;
                    }
                }

                if (ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                Console.WriteLine($"[KurisuVariantCache] Successfully updated variant {variant.ConsoleToString()}");
                return KurisuResult.Completed;
            }
            catch (PostgresException ex)
            {
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                Console.WriteLine($"[KurisuVariantCache] Error updating variant {variant.ConsoleToString()}: {ex.Message}");
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

        public static async Task<KurisuResult> DeleteVariantAsync(KurisuVariant variant, NpgsqlConnection? conn = null, NpgsqlTransaction? tran = null)
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
                    DELETE FROM variant
                    WHERE id = @id
                ";

                await using(var command = new NpgsqlCommand(sqlQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", variant.Id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if(rowsAffected <= 0)
                    {
                        if(ownsTransaction)
                        {
                            await transaction.RollbackAsync();
                        }

                        Console.WriteLine($"[KurisuVariantCache] No variant found with ID: {variant.Id}");
                        return KurisuResult.CannotFindVariant;
                    }
                }

                if(ownsTransaction)
                {
                    await transaction.CommitAsync();
                }

                _cache.TryRemove(variant.Id, out _);
                KurisuCharacter character = variant.Character;
                character.RemoveVariantInternal(variant);

                Console.WriteLine($"[KurisuVariantCache] Successfully deleted variant {variant.ConsoleToString()}");
                return KurisuResult.Completed;
            }
            catch (PostgresException ex)
            {
                if(ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                Console.WriteLine($"[KurisuVariantCache] Error deleting variant {variant.ConsoleToString()}");
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
                           characterid,
                           animeid,
                           rarity,
                           title
                    FROM variant
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("id"));
                    int characterId = reader.GetInt32(reader.GetOrdinal("characterid"));
                    int animeId = reader.GetInt32(reader.GetOrdinal("animeid"));
                    int rarityValue = reader.GetInt16(reader.GetOrdinal("rarity"));
                    string title = reader.GetString(reader.GetOrdinal("title"));

                    Result<KurisuCharacter> characterResult = KurisuCharacterCache.SearchByID(characterId);

                    if(!characterResult.Success)
                    {
                        KurisuVariant characterDeleteVariant = new KurisuVariant(id);
                        await DeleteVariantAsync(characterDeleteVariant);

                        Console.WriteLine($"[KurisuVariantCache] No character exists with ID: {characterId}, removing variant (ID: {id})");
                        continue;
                    }

                    Result<KurisuAnime> animeResult = KurisuAnimeCache.SearchByID(animeId);

                    if (!animeResult.Success)
                    {
                        KurisuVariant animeDeleteVariant = new KurisuVariant(id);
                        await DeleteVariantAsync(animeDeleteVariant);

                        Console.WriteLine($"[KurisuCardBlueprintCache] No anime exists with ID: {animeId}, removing variant (ID: {id})");
                        continue;
                    }

                    KurisuCharacter character = characterResult.Value;
                    KurisuAnime anime = animeResult.Value;
                    CardRarity rarity = (CardRarity)rarityValue;
                    KurisuVariant variant = new KurisuVariant(id, character, anime, rarity, title);
                    KurisuResult addVariantResult = character.AddVariantInternal(variant);

                    if(addVariantResult != KurisuResult.Completed)
                    {
                        Console.WriteLine($"[KurisuVariantCache] Error adding variant (ID: {variant.Id}) to character '{character.Name}' (ID: {character.Id}), skipping");
                        continue;
                    }

                    _cache.TryAdd(id, variant);
                }

                Console.WriteLine($"[KurisuVariantCache] Successfully loaded {_cache.Count} variants");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuVariantCache] Error loading variants: {ex.Message}");
                throw;
            }
        }
    }
}
