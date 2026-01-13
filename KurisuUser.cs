using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KurisuBot
{
    public enum UserBalance
    {
        Pity,
        QuizTokens,
        Coins,
        RDust,
        SRDust,
        SSRDust,
        URDust
    }

    public class KurisuUser
    {
        public long Id { get; private set; }
        public string Username { get; private set; }
        public KurisuUserSettings Settings { get; private set; }
        public DateTime LastQuizRegen { get; private set; }

        private List<KurisuCard> _cards = new();
        private int _pity = 0;
        private int _quizTokens = 10;
        private int _coins = 0;
        private int _rDust = 0;
        private int _srDust = 0;
        private int _ssrDust = 0;
        private int _urDust = 0;

        public KurisuUser(
            long id, 
            string username, 
            KurisuUserSettings settings,
            DateTime lastQuizRegen
        )
        {
            Id = id;
            Username = username;
            Settings = settings;
            LastQuizRegen = lastQuizRegen;
        }

        public bool AddCard(KurisuCard card)
        {
            if(_cards.Count >= Settings.MaxBinderSize)
            {
                Console.WriteLine($"[KurisuUser] Error adding card (ID: {card.Id}) to '{Username}' (ID: {Id}), max binder size reached");
                return false;
            }

            if(_cards.Contains(card))
            {
                Console.WriteLine($"[KurisuUser] Error adding card (ID: {card.Id}) to '{Username}' (ID: {Id}), binder already contains card");
                return false;
            }

            if(card.Owner != this)
            {
                Console.WriteLine($"[KurisuUser] Error adding card (ID: {card.Id}) to '{Username}' (ID: {Id}), card owner '{card.Owner.Username}' (ID: {card.Owner.Id}) does not match");
                return false;
            }

            _cards.Add(card);

            Console.WriteLine($"[KurisuUser] Successfully added card (ID: {card.Id}) to '{Username}' (ID: {Id})");
            return true;
        }

        public void RemoveCard(KurisuCard card)
        {
            if(!_cards.Contains(card))
            {
                Console.WriteLine($"[KurisuUser] Error removing card (ID: {card.Id}) from '{Username}' (ID: {Id}), binder does not contain card");
                return;
            }

            _cards.Remove(card);
            Console.WriteLine($"[KurisuUser] Successfully removed card (ID: {card.Id}) from '{Username}' (ID: {Id})");
        }

        public int GetBalance(UserBalance balance)
        {
            if(balance is UserBalance.QuizTokens)
            {
                CheckQuizTimer();
            }

            return balance switch
            {
                UserBalance.Pity => _pity,
                UserBalance.QuizTokens => _quizTokens,
                UserBalance.Coins => _coins,
                UserBalance.RDust => _rDust,
                UserBalance.SRDust => _srDust,
                UserBalance.SSRDust => _ssrDust,
                UserBalance.URDust => _urDust,
                _ => _coins
            };
        }

        public void SetBalance(UserBalance balance, int amount, bool markDirty = true)
        {
            amount = Math.Max(amount, 0);

            switch(balance)
            {
                case UserBalance.Pity: _pity = amount; break;
                case UserBalance.QuizTokens: _quizTokens = amount; break;
                case UserBalance.Coins: _coins = amount; break;
                case UserBalance.RDust: _rDust = amount; break;
                case UserBalance.SRDust: _srDust = amount; break;
                case UserBalance.SSRDust: _ssrDust = amount; break;
                case UserBalance.URDust: _urDust = amount; break;
            }

            if (markDirty)
            {
                KurisuUserCache.MarkUserDirty(this);
            }
        }

        public bool ModifyBalance(UserBalance balance, OperationType operation, int amount)
        {
            int modifiedBalance = GetBalance(balance);

            switch(operation)
            {
                case OperationType.Add: modifiedBalance += amount; break;
                case OperationType.Subtract: modifiedBalance -= amount; break;
                case OperationType.Multiply: modifiedBalance *= amount; break;
                case OperationType.Divide:
                    if(amount != 0)
                    {
                        modifiedBalance /= amount;
                    }

                    break;
                case OperationType.Set: modifiedBalance = amount; break;
            }

            if(modifiedBalance < 0)
            {
                return false;
            }

            SetBalance(balance, modifiedBalance);

            return true;
        }

        public TimeSpan GetNextQuizRegen()
        {
            CheckQuizTimer();

            int maxQuizTokens = Settings.MaxQuizTokens;
            int quizTokenRefreshInterval = Settings.QuizTokenRefreshInterval;

            if(_quizTokens >= maxQuizTokens)
            {
                return TimeSpan.Zero;
            }

            DateTime nextRegen = LastQuizRegen.AddMinutes(quizTokenRefreshInterval);

            return nextRegen - DateTime.UtcNow;
        }

        private void CheckQuizTimer()
        {
            int maxQuizTokens = Settings.MaxQuizTokens;

            if(_quizTokens >= maxQuizTokens)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            TimeSpan elapsedTime = now - LastQuizRegen;
            int quizTokenRefreshInterval = Settings.QuizTokenRefreshInterval;
            int intervals = (int)(elapsedTime.TotalMinutes / quizTokenRefreshInterval);

            if(intervals > 0)
            {
                int newTokensAmount = Math.Min(_quizTokens + intervals, maxQuizTokens);
                SetBalance(UserBalance.QuizTokens, newTokensAmount);
                LastQuizRegen = LastQuizRegen.AddMinutes(intervals * quizTokenRefreshInterval);
            }
        }

        public override bool Equals(object? obj)
        {
            if(obj is not KurisuUser other)
            {
                return false;
            }

            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class KurisuUserSettings
    {
        public int CrushRewardMin { get; set; } = 3;
        public int CrushRewardMax { get; set; } = 5;
        public int MaxBinderSize { get; set; } = 50;

        public int MaxQuizTokens { get; set; } = 10;
        public int QuizTokenRefreshInterval { get; set; } = 15;
        public int QuizTimeToAnswer { get; set; } = 30;
        public int QuizRewardMin { get; set; } = 20;
        public int QuizRewardMax { get; set; } = 30;

        public string BinderDescription { get; set; } = "A collection of cards.";
        public string BinderEmbedColor { get; set; } = "4FC1E8";
        public string WalletEmbedColor { get; set; } = "4FC1E8";
    }

    public static class KurisuUserSettingsSerializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize(KurisuUserSettings settings)
        {
            return JsonSerializer.Serialize(settings, _options);
        }

        public static KurisuUserSettings Deserialize(string json)
        {
            return JsonSerializer.Deserialize<KurisuUserSettings>(json, _options) ?? new KurisuUserSettings();
        }
    }

    public static class KurisuUserCache
    {
        private static readonly int _saveLoopInterval = 5; // Every n minutes, update DB
        private static readonly int _cacheExpirationTime = 6; // After n hours of no interaction, remove

        private static string _connectionString = "";
        private static MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static HashSet<KurisuUser> _dirtyUsers = new();

        private static CancellationTokenSource _cts = new();
        private static readonly object _lock = new();

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            _ = StartSaveLoopAsync();
            Console.WriteLine("[KurisuUserCache] Initialized");
        }

        public static async Task<KurisuUser> GetUserAsync(long id)
        {
            if(!_cache.TryGetValue(id, out KurisuUser user))
            {
                user = await LoadUserDataAsync(id) ?? await CreateUserAsync(id);

                _cache.Set(id, user, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(_cacheExpirationTime)
                });
            }

            return user;
        }

        public static async Task<KurisuUser> GetUserAsync(CommandContext ctx)
        {
            return await GetUserAsync((long)ctx.User.Id);
        }

        public static void MarkUserDirty(KurisuUser user)
        {
            lock(_lock)
            {
                _dirtyUsers.Add(user);
            }
        }

        public static async Task StartSaveLoopAsync()
        {
            _cts = new();

            TimeSpan interval = TimeSpan.FromMinutes(_saveLoopInterval);
            Console.WriteLine("[KurisuUserCache] Started save loop");

            while(!_cts.IsCancellationRequested)
            {
                await SaveUsersDataAsync();

                try
                {
                    await Task.Delay(interval, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public static async Task StopSaveLoopAsync()
        {
            _cts.Cancel();
            await SaveUsersDataAsync();
            Console.WriteLine("[KurisuUserCache] Stopped save loop");
        }

        private static async Task<KurisuUser> CreateUserAsync(long id)
        {
            DiscordUser dUser = await Program.Client.GetUserAsync((ulong)id);
            string username = dUser.Username;

            KurisuUser user = new KurisuUser(
                id, 
                username,
                new(),
                DateTime.UtcNow
            );

            MarkUserDirty(user);

            Console.WriteLine($"[KurisuUserCache] Creating new user for '{username}' (ID: {id})");
            return user;
        }

        private static async Task<KurisuUser?> LoadUserDataAsync(long id)
        {
            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    SELECT id,
                           settings,
                           quiztokens,
                           lastquizregen,
                           pity,
                           coins,
                           rdust,
                           srdust,
                           ssrdust,
                           urdust
                    FROM userdata
                    WHERE id = @id
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);

                command.Parameters.AddWithValue("@id", id);

                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                if(await reader.ReadAsync())
                {
                    DiscordUser dUser = await Program.Client.GetUserAsync((ulong)id);
                    string username = dUser.Username;

                    int settingsOrdinal = reader.GetOrdinal("settings");
                    KurisuUserSettings settings = reader.IsDBNull(settingsOrdinal)
                        ? new()
                        : KurisuUserSettingsSerializer.Deserialize(reader.GetString(settingsOrdinal));

                    DateTime lastQuizRegen = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("lastquizregen")), DateTimeKind.Utc);

                    KurisuUser user = new KurisuUser(
                        id,
                        username,
                        settings,
                        lastQuizRegen
                    );

                    user.SetBalance(UserBalance.Pity, reader.GetInt32(reader.GetOrdinal("pity")), false);
                    user.SetBalance(UserBalance.QuizTokens, reader.GetInt32(reader.GetOrdinal("quiztokens")), false);
                    user.SetBalance(UserBalance.Coins, reader.GetInt32(reader.GetOrdinal("coins")), false);
                    user.SetBalance(UserBalance.RDust, reader.GetInt32(reader.GetOrdinal("rdust")), false);
                    user.SetBalance(UserBalance.SRDust, reader.GetInt32(reader.GetOrdinal("srdust")), false);
                    user.SetBalance(UserBalance.SSRDust, reader.GetInt32(reader.GetOrdinal("ssrdust")), false);
                    user.SetBalance(UserBalance.URDust, reader.GetInt32(reader.GetOrdinal("urdust")), false);

                    Console.WriteLine($"[KurisuUserCache] Loaded user '{username}' (ID: {id}) from Supabase");
                    return user;
                }

                Console.WriteLine($"[KurisuUserCache] Error finding user with ID: {id} in Supabase");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuUserCache] Error loading user data for ID: {id}: {ex.Message}");
                throw;
            }
        }

        private static async Task SaveUsersDataAsync()
        {
            HashSet<KurisuUser> users;

            lock(_lock)
            {
                users = [.. _dirtyUsers];
                _dirtyUsers.Clear();
            }

            if(!users.Any())
            {
                return;
            }

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    const string tempTableSQLQuery = @"
                        CREATE TEMP TABLE temp_userdata (
                            id BIGINT PRIMARY KEY,
                            settings JSONB,
                            quiztokens INT,
                            lastquizregen TIMESTAMPTZ,
                            pity INT,
                            coins INT,
                            rdust INT,
                            srdust INT,
                            ssrdust INT,
                            urdust INT
                        ) ON COMMIT DROP;    
                    ";

                    await using NpgsqlCommand tempTableCommand = new NpgsqlCommand(tempTableSQLQuery, connection, transaction);
                    await tempTableCommand.ExecuteNonQueryAsync();
                    NpgsqlBinaryImporter? writer = null;

                    try
                    {
                        writer = await connection.BeginBinaryImportAsync(@"
                            COPY temp_userdata (id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust)
                            FROM STDIN (FORMAT BINARY)
                        ");

                        foreach(KurisuUser user in users)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(user.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                            await writer.WriteAsync(KurisuUserSettingsSerializer.Serialize(user.Settings), NpgsqlTypes.NpgsqlDbType.Jsonb);
                            await writer.WriteAsync(user.GetBalance(UserBalance.QuizTokens), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(DateTime.SpecifyKind(user.LastQuizRegen, DateTimeKind.Utc), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                            await writer.WriteAsync(user.GetBalance(UserBalance.Pity), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(user.GetBalance(UserBalance.Coins), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(user.GetBalance(UserBalance.RDust), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(user.GetBalance(UserBalance.SRDust), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(user.GetBalance(UserBalance.SSRDust), NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(user.GetBalance(UserBalance.URDust), NpgsqlTypes.NpgsqlDbType.Integer);
                        }

                        await writer.CompleteAsync();
                    }
                    catch
                    {
                        if(writer != null)
                        {
                            try
                            {
                                await writer.CloseAsync();
                            }
                            catch { }
                        }

                        throw;
                    }
                    finally
                    {
                        writer?.Dispose();
                    }

                    const string upsertTableSQLQuery = @"
                        INSERT INTO userdata (id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust) 
                        SELECT id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust
                        FROM temp_userdata
                        ON CONFLICT (id) DO UPDATE SET
                            settings        = EXCLUDED.settings,
                            quiztokens      = EXCLUDED.quiztokens,
                            lastquizregen   = EXCLUDED.lastquizregen,
                            pity            = EXCLUDED.pity,
                            coins           = EXCLUDED.coins,
                            rdust           = EXCLUDED.rdust,
                            srdust          = EXCLUDED.srdust,
                            ssrdust         = EXCLUDED.ssrdust,
                            urdust          = EXCLUDED.urdust
                    ";

                    await using NpgsqlCommand upsertTableCommand = new NpgsqlCommand(upsertTableSQLQuery, connection, transaction);
                    await upsertTableCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    Console.WriteLine($"[KurisuUserCache] Saved {users.Count} users' data");
                }
                catch
                {
                    Console.WriteLine($"[KurisuUserCache] Rolling back transaction...");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuUserCache] Error saving {users.Count} users' data: {ex.Message}");
            }
        }
    }
}
