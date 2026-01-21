using Npgsql;

namespace KurisuBot
{
    public class KurisuCard
    {
        public long Id { get; private set; }
        public KurisuUser? Owner { get; private set; }
        public KurisuVariant Blueprint { get; private set; }
        public DateTime TimeObtained { get; private set; }
        public int? ImageKey { get; private set; } 
        public int? BorderId { get; private set; }
        public int? OverlayId { get; private set; }

        public KurisuCard(
            long id, 
            KurisuUser? owner, 
            KurisuVariant blueprint, 
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

        public void SetOwner(KurisuUser owner)
        {
            Owner = owner;
            KurisuCardDatabase.MarkCardDirty(this);
        }

        public void SetTimeObtained(DateTime newTime)
        {
            TimeObtained = newTime;
            KurisuCardDatabase.MarkCardDirty(this);
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

    public static class KurisuCardDatabase
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

        public static void MarkCardForDelete(KurisuCard card)
        {
            lock(_lock)
            {
                _dirtyCards.Remove(card);
                _deleteCards.Add(card);
            }
        }

        public static void MarkCardForDelete(long id)
        {
            KurisuCard deleteCard = new(
                id,
                null!,
                null!,
                DateTime.MinValue,
                null,
                null,
                null
            );

            MarkCardForDelete(deleteCard);
        }

        public static async Task StartSaveLoopAsync()
        {
            _cts = new();

            TimeSpan interval = TimeSpan.FromMinutes(_saveLoopInterval);
            Console.WriteLine("[KurisuCardCache] Started save loop");

            while(!_cts.IsCancellationRequested)
            {
                try
                {
                    await SaveCardsDataAsync(_cts.Token);
                }
                catch (OperationCanceledException) { }

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

            try
            {
                await SaveCardsDataAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) { }

            Console.WriteLine("[KurisuCardCache] Stopped save loop");
        }

        public static async Task<KurisuResult> LoadUserCardsDataAsync(KurisuUser user)
        {
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

                int totalCards = 0;
                int addedCards = 0;

                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(reader.GetOrdinal("id"));
                    int blueprintId = reader.GetInt32(reader.GetOrdinal("blueprintid"));
                    KurisuVariant? blueprint = KurisuVariantCache.SearchByID(blueprintId);

                    if (blueprint == null)
                    {
                        MarkCardForDelete(id);

                        Console.WriteLine($"[KurisuCardCache] No variant found with ID: {blueprintId}, deleting card (ID: {id})");
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

                    if(user.AddCard(card) == KurisuResult.Completed)
                    {
                        addedCards++;
                    }

                    totalCards++;
                }

                Console.WriteLine($"[KurisuCardCache] Successfully loaded {addedCards} of {totalCards} cards for user '{user.Username}' (ID: {user.Id})");
                return KurisuResult.Completed;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[KurisuCardCache] Error loading cards for user '{user.Username}' (ID: {user.Id}): {ex.Message}");
                return KurisuResult.DatabaseFailure;
            }
        }

        private static async Task SaveCardsDataAsync(CancellationToken token)
        {
            HashSet<KurisuCard> dirtyCards;
            HashSet<KurisuCard> deleteCards;

            lock(_lock)
            {
                dirtyCards = [.. _dirtyCards];
                deleteCards = [.. _deleteCards];
                _dirtyCards.Clear();
                _deleteCards.Clear();
            }

            if(!dirtyCards.Any() && !deleteCards.Any())
            {
                return;
            }

            try
            {
                await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(token);
                await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

                try
                {
                    if(deleteCards.Any())
                    {
                        const string tempDeleteTableSQLQuery = @"
                            CREATE TEMP TABLE temp_deletecarddata (
                                id BIGINT PRIMARY KEY
                            ) ON COMMIT DROP;
                        ";

                        await using NpgsqlCommand tempDeleteTableCommand = new NpgsqlCommand(tempDeleteTableSQLQuery, connection, transaction);
                        await tempDeleteTableCommand.ExecuteNonQueryAsync(token);
                        NpgsqlBinaryImporter? deleteWriter = null;

                        try
                        {
                            deleteWriter = await connection.BeginBinaryImportAsync(@"
                                COPY temp_deletecarddata (id)
                                FROM STDIN (FORMAT BINARY)
                            ");

                            foreach(KurisuCard card in deleteCards)
                            {
                                await deleteWriter.StartRowAsync();
                                await deleteWriter.WriteAsync(card.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                            }

                            await deleteWriter.CompleteAsync(token);
                        }
                        finally
                        {
                            deleteWriter?.Dispose();
                        }

                        const string deleteSQLQuery = @"
                            DELETE FROM carddata
                            WHERE id IN (SELECT id FROM temp_deletecarddata)
                        ";

                        await using NpgsqlCommand deleteTableCommand = new NpgsqlCommand(deleteSQLQuery, connection, transaction);
                        await deleteTableCommand.ExecuteNonQueryAsync(token);
                    }

                    if(dirtyCards.Any())
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
                        await tempTableCommand.ExecuteNonQueryAsync(token);
                        NpgsqlBinaryImporter? writer = null;

                        try
                        {
                            writer = await connection.BeginBinaryImportAsync(@"
                                COPY temp_carddata (id, ownerid, blueprintid, timeobtained, imagekey, borderid, overlayid)
                                FROM STDIN (FORMAT BINARY)
                            ");

                            foreach (KurisuCard card in dirtyCards)
                            {
                                await writer.StartRowAsync();
                                await writer.WriteAsync(card.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                                await writer.WriteAsync(card.Owner.Id, NpgsqlTypes.NpgsqlDbType.Bigint);
                                await writer.WriteAsync(card.Blueprint.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                                await writer.WriteAsync(DateTime.SpecifyKind(card.TimeObtained, DateTimeKind.Utc), NpgsqlTypes.NpgsqlDbType.TimestampTz);

                                if (card.ImageKey.HasValue)
                                {
                                    await writer.WriteAsync(card.ImageKey.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                                }
                                else
                                {
                                    await writer.WriteNullAsync();
                                }

                                if (card.BorderId.HasValue)
                                {
                                    await writer.WriteAsync(card.BorderId.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                                }
                                else
                                {
                                    await writer.WriteNullAsync();
                                }

                                if (card.OverlayId.HasValue)
                                {
                                    await writer.WriteAsync(card.OverlayId.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                                }
                                else
                                {
                                    await writer.WriteNullAsync();
                                }
                            }

                            await writer.CompleteAsync(token);
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
                        await upsertTableCommand.ExecuteNonQueryAsync(token);
                    }

                    await transaction.CommitAsync(token);

                    if(dirtyCards.Any())
                    {
                        Console.WriteLine($"[KurisuCardCache] Successfully saved {dirtyCards.Count} cards");
                    }

                    if(deleteCards.Any())
                    {
                        Console.WriteLine($"[KurisuCardCache] Successfully deleted {deleteCards.Count} cards");
                    }
                }
                catch
                {
                    Console.WriteLine($"[KurisuCardCache] Rolling back transaction...");
                    await transaction.RollbackAsync(token);
                    throw;
                }
            }
            catch(OperationCanceledException)
            {
                Console.WriteLine("[KurisuCardCache] Save cancelled");
                throw;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[KurisuCardCache] Error saving/deleting cards: {ex.Message}");
            }
        }
    }
}
