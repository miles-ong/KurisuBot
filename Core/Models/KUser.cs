using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    public sealed class KUser
    {
        public long Id { get; }
        public string Username { get; private set; }
        public KSettings Settings { get; private set; }
        public DateTime LastQuizRegen { get; private set; }

        private readonly HashSet<KCard> _cards = new();
        private readonly object _cardsLock = new();
        private int _pity;
        private int _quizTokens;
        private int _coins;
        private int _rDust;
        private int _srDust;
        private int _ssrDust;
        private int _urDust;
        public IEnumerable<KCard> Cards
        {
            get
            {
                lock (_cardsLock) return _cards.ToList();
            }
        }

        internal KUser(long id, string username, KSettings settings, DateTime lastQuizRegen)
        {
            Id = id;
            Username = username;
            Settings = settings;
            LastQuizRegen = lastQuizRegen;
        }

        internal KResult SetUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return KResult.InvalidUsername;

            Username = username;

            return KResult.Completed;
        }

        internal KResult AddCard(KCard card)
        {
            if (_cards.Contains(card)) return KResult.UserAlreadyContainsCard;
            if (_cards.Count >= Settings.MaxBinderSize) return KResult.UserMaxCardsReached;

            // fix

            return KResult.Completed;
        }

        internal KResult RemoveCard(KCard card)
        {
            if (!_cards.Contains(card)) return KResult.UserDoesNotContainCard;

            lock (_cardsLock) _cards.Remove(card);

            return KResult.Completed;
        }

        public int GetBalance(KBalance balance)
        {
            if (balance is KBalance.QuizTokens) CheckQuizTimer();

            return balance switch
            {
                KBalance.Pity => _pity,
                KBalance.QuizTokens => _quizTokens,
                KBalance.Coins => _coins,
                KBalance.RDust => _rDust,
                KBalance.SRDust => _srDust,
                KBalance.SSRDust => _ssrDust,
                KBalance.URDust => _urDust,
                _ => _coins
            };
        }

        internal void SetBalance(KBalance balance, int amount)
        {
            amount = Math.Max(amount, 0);

            switch (balance)
            {
                case KBalance.Pity: _pity = amount; break;
                case KBalance.QuizTokens: _quizTokens = amount; break;
                case KBalance.Coins: _coins = amount; break;
                case KBalance.RDust: _rDust = amount; break;
                case KBalance.SRDust: _srDust = amount; break;
                case KBalance.SSRDust: _ssrDust = amount; break;
                case KBalance.URDust: _urDust = amount; break;
            }
        }

        internal void ModifyBalance(KBalance balance, KOperation operation, int amount)
        {
            int current = GetBalance(balance);

            int modified = operation switch
            {
                KOperation.Add => current + amount,
                KOperation.Subtract => current - amount,
                KOperation.Multiply => current * amount,
                KOperation.Divide => amount != 0 ? current / amount : current,
                _ => current
            };

            SetBalance(balance, modified);
        }

        public TimeSpan GetNextQuizRegen()
        {
            CheckQuizTimer();

            if (_quizTokens >= Settings.MaxQuizTokens) return TimeSpan.Zero;

            return LastQuizRegen.AddMinutes(Settings.QuizTokenRefreshInterval) - DateTime.UtcNow;
        }

        private void CheckQuizTimer()
        {
            if (_quizTokens >= Settings.MaxQuizTokens) return;

            TimeSpan elapsed = DateTime.UtcNow - LastQuizRegen;
            int intervals = (int)(elapsed.TotalMinutes / Settings.QuizTokenRefreshInterval);

            if (intervals > 0)
            {
                SetBalance(KBalance.QuizTokens, Math.Min(_quizTokens + intervals, Settings.MaxQuizTokens));
                LastQuizRegen = LastQuizRegen.AddMinutes(intervals * Settings.QuizTokenRefreshInterval);
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is KUser other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"'{Username}' (ID: {Id})";
        }
    }
}
