using KurisuCore.Enums;
using KurisuCore.Result;
using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KUser(
        KSettings settings,
        DateTime lastQuizRegen,
        int quizTokens,
        int pity,
        int coins,
        int rDust,
        int srDust,
        int ssrDust,
        int urDust)
        : KEntity<long, KUserData>, IKLinkable<KCard>
    {
        public KSettings Settings { get; internal set; } = settings;
        public DateTime LastQuizRegen { get; internal set; } = lastQuizRegen;
        public override string DisplayName => "User";

        private int _quizTokens = quizTokens;
        private int _pity = pity;
        private int _coins = coins;
        private int _rDust = rDust;
        private int _srDust = srDust;
        private int _ssrDust = ssrDust;
        private int _urDust = urDust;
        private readonly HashSet<KCard> _cards = new();
        private readonly Lock _cardsLock = new();

        [JsonIgnore]
        public IEnumerable<KCard> Cards
        {
            get { lock (_cardsLock) return _cards.ToList(); }
        }

        void IKLinkable<KCard>.Link(KCard card)
        {
            lock (_cardsLock) _cards.Add(card);
        }

        void IKLinkable<KCard>.Unlink(KCard card)
        {
            lock (_cardsLock) _cards.Remove(card);
        }

        internal bool ContainsCard(KCard card)
        {
            lock (_cardsLock) return _cards.Contains(card);
        }

        public int GetBalance(KBalance balance)
        {
            if (balance is KBalance.QuizTokens) CheckQuizTimer();

            return balance switch
            {
                KBalance.QuizTokens => _quizTokens,
                KBalance.Pity => _pity,
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
                case KBalance.QuizTokens: _quizTokens = amount; break;
                case KBalance.Pity: _pity = amount; break;
                case KBalance.Coins: _coins = amount; break;
                case KBalance.RDust: _rDust = amount; break;
                case KBalance.SRDust: _srDust = amount; break;
                case KBalance.SSRDust: _ssrDust = amount; break;
                case KBalance.URDust: _urDust = amount; break;
                default: return;
            }
        }

        internal void ModifyBalance(KBalance balance, KOperation operation, int amount)
        {
            var current = GetBalance(balance);

            var modified = operation switch
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

            var elapsed = DateTime.UtcNow - LastQuizRegen;
            var intervals = (int)(elapsed.TotalMinutes / Settings.QuizTokenRefreshInterval);

            if (intervals > 0)
            {
                SetBalance(KBalance.QuizTokens, Math.Min(_quizTokens + intervals, Settings.MaxQuizTokens));
                LastQuizRegen = LastQuizRegen.AddMinutes(intervals * Settings.QuizTokenRefreshInterval);
            }
        }

        internal override KUserData ToData() => new(Id, Settings, LastQuizRegen, _quizTokens, _pity, _coins, _rDust,
            _srDust, _ssrDust, _urDust);
        public override string ToString() => $"User (ID: {Id})";
    }
}
