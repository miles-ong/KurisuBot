using Npgsql;

namespace KurisuBot
{
    public class KurisuCard
    {
        public long Id { get; private set; }
        public KurisuUser Owner { get; private set; }
        public KurisuCardBlueprint Blueprint { get; private set; }
        public DateTime TimeObtained { get; private set; }
        public int? ImageKey { get; private set; } 
        public int? BorderId { get; private set; }
        public int? OverlayId { get; private set; } 

        public KurisuCard(
            long id, 
            KurisuUser owner, 
            KurisuCardBlueprint blueprint, 
            DateTime timeObtained,
            int? imageKey, 
            int? borderId, 
            int? overlayId
        )
        {
            Id = id;
            Owner = owner;
            Blueprint = blueprint;
            TimeObtained = timeObtained;
            ImageKey = imageKey;
            BorderId = borderId;
            OverlayId = overlayId;
        }

        public bool TransferOwnership(KurisuUser other)
        {
            if(Owner == other)
            {
                return false;
            }

            KurisuUser prevOwner = Owner;
            Owner = other;

            if(!other.AddCard(this))
            {
                Console.WriteLine($"[KurisuCard] Error transferring ownership of card (ID: {Id}) from '{Owner.Username}' (ID: {Owner.Id}) to '{other.Username}' (ID: {other.Id})");
                return false;
            }

            prevOwner.RemoveCard(this);
            KurisuCardCache.MarkCardDirty(this);

            Console.WriteLine($"[KurisuCard] Successfully transferred ownership of card (ID: {Id}) from '{prevOwner.Username}' (ID: {prevOwner.Id}) to '{Owner.Username}' (ID: {Owner.Id})");
            return true;
        }

        public void SetTimeObtained(DateTime newTime)
        {
            TimeObtained = newTime;
            KurisuCardCache.MarkCardDirty(this);
        }

        public override bool Equals(object? obj)
        {
            if(obj is not KurisuCard other)
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

    public static class KurisuCardCache
    {
        private static readonly int _saveLoopInterval = 5; // Every n minutes, update DB

        private static string _connectionString = "";
        private static HashSet<KurisuCard> _dirtyCards = new();
        private static HashSet<KurisuCard> _deleteCards = new();

        private static CancellationTokenSource _cts = new();
        private static readonly object _lock = new();

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            _ = StartSaveLoopAsync();
            Console.WriteLine("[KurisuCardCache] Initialized");
        }

        public static void MarkCardDirty(KurisuCard card)
        {
            lock(_lock)
            {
                _dirtyCards.Add(card);
            }
        }

        public static async Task StartSaveLoopAsync()
        {
            _cts = new();

            TimeSpan interval = TimeSpan.FromMinutes(_saveLoopInterval);
            Console.WriteLine("[KurisuCardCache] Started save loop");

            while(!_cts.IsCancellationRequested)
            {
                await SaveCardsDataAsync();

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
            await SaveCardsDataAsync();
            Console.WriteLine("[KurisuCardCache] Stopped save loop");
        }

        public static async Task<List<KurisuCard>> LoadUserCardsDataAsync(KurisuUser user)
        {
            List<KurisuCard> cards = new();

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sqlQuery = @"
                    SELECT id,
                           ownerid,
                           blueprintid,
                           timeobtained,
                           imagekey,
                           borderid,
                           overlayid
                    FROM carddata
                    WHERE ownerid = @ownerid
                ";

                await using NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection);

                command.Parameters.AddWithValue("@ownerid", user.Id);

                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(reader.GetOrdinal("id"));
                    int blueprintId = reader.GetInt32(reader.GetOrdinal("blueprintid"));
                    KurisuCardBlueprint? blueprint = KurisuCardBlueprintCache.SearchByID(blueprintId);

                    if (blueprint == null)
                    {
                        Console.WriteLine($"[KurisuCardCache] No card blueprint found with ID: {blueprintId} in Supabase, removing card (ID: {id}) from Supabase...");
                        continue;
                    }

                    DateTime timeObtained = reader.GetDateTime(reader.GetOrdinal("timeobtained"));

                    int imageKeyOrdinal = reader.GetOrdinal("imagekey");
                    int? imageKey = reader.IsDBNull(imageKeyOrdinal)
                        ? null
                        : reader.GetInt32(imageKeyOrdinal);

                    int borderIdOrdinal = reader.GetOrdinal("borderid");
                    int? borderId = reader.IsDBNull(borderIdOrdinal)
                        ? null
                        : reader.GetInt32(borderIdOrdinal);

                    int overlayIdOrdinal = reader.GetOrdinal("overlayid");
                    int? overlayId = reader.IsDBNull(overlayIdOrdinal)
                        ? null
                        : reader.GetInt32(overlayIdOrdinal);

                    KurisuCard card = new KurisuCard(
                        id,
                        user,
                        blueprint,
                        timeObtained,
                        imageKey,
                        borderId,
                        overlayId
                    );

                    cards.Add(card);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KurisuCardCache] Error loading cards for user '{user.Username}' (ID: {user.Id}): {ex.Message}");
            }

            Console.WriteLine($"[KurisuCardCache] Successfully loaded {cards.Count} cards for user '{user.Username}' (ID: {user.Id})");
            return cards;
        }

        private static async Task SaveCardsDataAsync()
        {
            HashSet<KurisuCard> cards;

            lock(_lock)
            {
                cards = [.. _dirtyCards];
                _dirtyCards.Clear();
            }

            if(!cards.Any())
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
                        CREATE TEMP TABLE temp_carddata (
                            id BIGINT PRIMARY KEY,
                            ownerid BIGINT,
                            blueprintid INT,
                            timeobtained TIMESTAMPTZ,
                            imagekey INT,
                            borderid INT,
                            overlayid INT
                        ) ON COMMIT DROP;
                    ";

                    await using NpgsqlCommand tempTableCommand = new NpgsqlCommand(tempTableSQLQuery, connection, transaction);
                    await tempTableCommand.ExecuteNonQueryAsync();
                    NpgsqlBinaryImporter? writer = null;

                    try
                    {
                        writer = await connection.BeginBinaryImportAsync(@"
                            COPY temp_carddata (id, ownerid, blueprintid, timeobtained, imagekey, borderid, overlayid)
                            FROM STDIN (FORMAT BINARY)
                        ");

                        foreach(KurisuCard card in cards)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(card.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                            await writer.WriteAsync(card.Owner.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                            await writer.WriteAsync(card.Blueprint.Id, NpgsqlTypes.NpgsqlDbType.Integer);

                            await writer.WriteAsync(DateTime.SpecifyKind(card.TimeObtained, DateTimeKind.Utc), NpgsqlTypes.NpgsqlDbType.TimestampTz);

                            if(card.ImageKey.HasValue)
                            {
                                await writer.WriteAsync(card.ImageKey.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                            }
                            else
                            {
                                await writer.WriteNullAsync();
                            }

                            if(card.BorderId.HasValue)
                            {
                                await writer.WriteAsync(card.BorderId.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                            }
                            else
                            {
                                await writer.WriteNullAsync();
                            }

                            if(card.OverlayId.HasValue)
                            {
                                await writer.WriteAsync(card.OverlayId.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                            }
                            else
                            {
                                await writer.WriteNullAsync();
                            }
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
                        INSERT INTO carddata (id, ownerid, blueprintid, timeobtained, imagekey, borderid, overlayid)
                        SELECT id, ownerid, blueprintid, timeobtained, imagekey, borderid, overlayid
                        FROM temp_carddata
                        ON CONFLICT (id) DO UPDATE SET
                            ownerid         = EXCLUDED.ownerid,
                            blueprintid     = EXCLUDED.blueprintid,
                            timeobtained    = EXCLUDED.timeobtained,
                            imagekey        = EXCLUDED.imagekey,
                            borderid        = EXCLUDED.borderid,
                            overlayid       = EXCLUDED.overlayid
                    ";

                    await using NpgsqlCommand upsertTableCommand = new NpgsqlCommand(upsertTableSQLQuery, connection, transaction);
                    await upsertTableCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    Console.WriteLine($"[KurisuCardCache] Saved {cards.Count} cards' data");
                }
                catch
                {
                    Console.WriteLine($"[KurisuCardCache] Rolling back transaction...");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuCardCache] Error saving {cards.Count} cards' data: {ex.Message}");
            }
        }
    }
}
