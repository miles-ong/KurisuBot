using KurisuAPI.Cache;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuAPI.Services;

public sealed class KUserService
{
    private readonly KUserRepository _repository;
    private readonly KCardService _cardService;
    private readonly Func<long, Task<string>> _usernameResolver;
    private readonly int _saveLoopIntervalMinutes = 1;
    private CancellationTokenSource _cts = new();
    private const string _consoleTag = "[KUserService]";

    public KUserService(string connectionString, KCardService cardService, Func<long, Task<string>>? usernameResolver = null)
    {
        _repository = new KUserRepository(connectionString);
        _cardService = cardService;
        _usernameResolver = usernameResolver ?? (_ => Task.FromResult(string.Empty));
    }

    public void Initialize()
    {
        _ = StartSaveLoopAsync();
        Console.WriteLine($"{_consoleTag} Initialized");
    }

    public async Task<KUser> GetOrCreateAsync(long id)
    {
        if (KUserCache.TryGet(id, out KUser? cached)) return cached!;

        KUser? user = await _repository.GetByIdAsync(id);

        if(user is null)
        {
            Console.WriteLine($"{_consoleTag} '{id}' User ID does not exist, creating new user...");
            user = await CreateUserAsync(id);
        }
        else
        {
            string username = await _usernameResolver(id);
            user.SetUsername(username);
            await _cardService.LoadUserCardsAsync(user);
        }

        KUserCache.Add(user);

        return user;
    }

    public void MarkDirty(KUser user)
    {
        KUserCache.MarkDirty(user);
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

            await SaveDirtyUsersAsync(_cts.Token);
        }
    }

    public async Task StopSaveLoopAsync()
    {
        _cts.Cancel();
        await SaveDirtyUsersAsync(CancellationToken.None);
        Console.WriteLine($"{_consoleTag} Stopped save loop");
    }

    private async Task<KUser> CreateUserAsync(long id)
    {
        string username = await _usernameResolver(id);
        KUser user = new KUser(id, username, new(), DateTime.UtcNow);

        user.SetBalance(KBalance.QuizTokens, user.Settings.MaxQuizTokens);

        KUserCache.MarkDirty(user);
        Console.WriteLine($"{_consoleTag} Created new user {user}");

        return user;
    }

    private async Task SaveDirtyUsersAsync(CancellationToken token)
    {
        HashSet<KUser> users = KUserCache.TakeDirty();

        if (users.Count == 0) return;

        try
        {
            await _repository.BulkUpsertAsync(users, token);
            Console.WriteLine($"{_consoleTag} Saved {users.Count} users' data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to save {users.Count} users' data: {ex.Message}");

            foreach(KUser user in users)
            {
                KUserCache.MarkDirty(user);
            }
        }
    }
}
