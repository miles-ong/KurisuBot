using KurisuBot.Core.Infrastructure.Cache;
using KurisuBot.Core.Infrastructure.Repositories;
using KurisuBot.Core.Models;

namespace KurisuBot.Core.Services
{
    public sealed class KCardService
    {
        private readonly KCardRepository _repository;
        private readonly int _saveLoopIntervalMinutes = 1;
        private CancellationTokenSource _cts = new();
        private const string _consoleTag = "[KCardService]";

        public KCardService(string connectionString)
        {
            _repository = new KCardRepository(connectionString);
        }

        public void Initialize()
        {
            _ = StartSaveLoopAsync();
            Console.WriteLine($"{_consoleTag} Initialized");
        }

        public async Task<IEnumerable<KCard>> LoadUserCardsAsync(KUser user)
        {
            List<(long id, int variantId, DateTime timeObtained, int? borderId, int? overlayId)> cards = await _repository.GetByOwnerAsync(user.Id);

            foreach(var c in cards)
            {
                if (!Program.VariantService.TryGet(c.variantId, out KVariant? variant))
                {
                    Console.WriteLine($"{_consoleTag} '{c.variantId}' Variant ID does not exist, skipping...");
                    continue;
                }

                KCard card = new KCard(c.id, user, variant!, c.timeObtained, c.borderId, c.overlayId);
                user.AddCard(card);
            }

            return user.Cards;
        }

        public void MarkDirty(KCard card)
        {
            KCardCache.MarkDirty(card);
        }

        public async Task StartSaveLoopAsync()
        {
            _cts = new();
            Console.WriteLine($"{_consoleTag} Started save loop");

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_saveLoopIntervalMinutes), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await SaveDirtyCardsAsync(_cts.Token);
            }
        }

        public async Task StopSaveLoopAsync()
        {
            _cts.Cancel();
            await SaveDirtyCardsAsync(CancellationToken.None);
            Console.WriteLine($"{_consoleTag} Stopped save loop");
        }

        private async Task SaveDirtyCardsAsync(CancellationToken token)
        {
            HashSet<KCard> cards = KCardCache.TakeDirty();

            if (cards.Count == 0) return;

            try
            {
                await _repository.BulkUpsertAsync(cards, token);
                Console.WriteLine($"{_consoleTag} Saved {cards.Count} cards' data");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to save {cards.Count} cards' data: {ex.Message}");

                foreach(KCard card in cards)
                {
                    KCardCache.MarkDirty(card);
                }
            }
        }
    }
}
