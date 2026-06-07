using KurisuAPI.Cache;
using KurisuAPI.Records;
using KurisuAPI.Repositories;
using KurisuCore.Models;

namespace KurisuAPI.Services;

public sealed class KCardService(
    KCardRepository repository, 
    KCardCache cache,
    KVariantService variantService
    )
{
    private readonly int _saveLoopIntervalMinutes = 1;
    private CancellationTokenSource _cts = new();
    private const string _consoleTag = "[KCardService]";

    public void Initialize()
    {
        _ = StartSaveLoopAsync();
        Console.WriteLine($"{_consoleTag} Initialized");
    }

    public async Task<IEnumerable<KCard>> LoadUserCardsAsync(KUser user)
    {
        List<KCardRow> cards = await repository.GetByOwnerAsync(user.Id);

        foreach(KCardRow c in cards)
        {
            KVariant? variant = await variantService.GetByIdAsync(c.VariantId);

            if (variant is null) continue;

            KCard card = new(
                c.Id,
                user,
                variant,
                c.TimeObtained,
                c.BorderId,
                c.OverlayId
            );

            user.AddCard(card);
        }

        return user.Cards;
    }

    public void MarkDirty(KCard card)
    {
        cache.MarkDirty(card);
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
        HashSet<KCard> cards = cache.TakeDirty();

        if (cards.Count == 0) return;

        try
        {
            await repository.BulkUpsertAsync(cards, token);
            Console.WriteLine($"{_consoleTag} Saved {cards.Count} cards' data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to save {cards.Count} cards' data: {ex.Message}");

            foreach(KCard card in cards)
            {
                cache.MarkDirty(card);
            }
        }
    }
}
