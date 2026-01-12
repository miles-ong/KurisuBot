using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public class KurisuCard
    {
        public int Id { get; private set; }
        public int OwnerId { get; private set; }
        public KurisuCardBlueprint Blueprint { get; private set; }
        public int? ImageKey { get; private set; }
        public int? BorderId { get; private set; }
        public int? OverlayId { get; private set; }

        public KurisuCard(int id, int ownerId, KurisuCardBlueprint blueprint, int? imageKey, int? borderId, int? overlayId)
        {
            Id = id;
            OwnerId = ownerId;
            Blueprint = blueprint;
            ImageKey = imageKey;
            BorderId = borderId;
            OverlayId = overlayId;
        }
    }

    public static class KurisuCardCache
    {
        private static readonly int _saveLoopInterval = 5; // Every n minutes, update DB

        private static string _connectionString = "";
        private static ConcurrentDictionary<int, KurisuCard> _cache = new();
        private static HashSet<KurisuCard> _dirtyCards = new();

        private static CancellationTokenSource _cts = new();
        private static readonly object _lock = new();

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            Console.WriteLine("[KurisuCardCache] Initialized");
        }

        public static async Task StartSaveLoopAsync()
        {
            _cts = new();

            TimeSpan interval = TimeSpan.FromMinutes(_saveLoopInterval);
            DateTime nextRun = DateTime.UtcNow + interval;
            Console.WriteLine("[KurisuCardCache] Started save loop");

            while(!_cts.IsCancellationRequested)
            {
                //

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
            //
            Console.WriteLine("[KurisuCardCache] Stopped save loop");
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
                    ";

                    await using NpgsqlCommand tempTableCommand = new NpgsqlCommand(tempTableSQLQuery, connection, transaction);
                    await tempTableCommand.ExecuteNonQueryAsync();
                    NpgsqlBinaryImporter? writer = null;

                    try
                    {
                        writer = await connection.BeginBinaryImportAsync(@"
                        ");

                        foreach(KurisuCard card in cards)
                        {

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
