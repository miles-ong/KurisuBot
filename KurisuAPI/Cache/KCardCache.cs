using KurisuCore.Models;

namespace KurisuAPI.Cache
{
    public sealed class KCardCache
    {
        private readonly HashSet<KCard> _dirtyCards = new();
        private readonly object _lock = new();

        public void MarkDirty(KCard card)
        {
            lock (_lock) _dirtyCards.Add(card);
        }

        public HashSet<KCard> TakeDirty()
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
