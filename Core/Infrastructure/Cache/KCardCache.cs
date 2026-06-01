using KurisuBot.Core.Models;

namespace KurisuBot.Core.Infrastructure.Cache
{
    internal static class KCardCache
    {
        private static readonly HashSet<KCard> _dirtyCards = new();
        private static readonly object _lock = new();

        public static void MarkDirty(KCard card)
        {
            lock (_lock) _dirtyCards.Add(card);
        }

        public static HashSet<KCard> TakeDirty()
        {
            lock (_lock)
            {
                HashSet<KCard> snapshot = [.. _dirtyCards];
                _dirtyCards.Clear();

                return snapshot;
            }
        }
    }
}
