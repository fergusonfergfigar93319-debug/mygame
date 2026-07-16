using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Game.UI.Lobby
{
    internal sealed class LobbyModalStack
    {
        private readonly List<Entry> entries = new();
        private readonly Action<GameObject> destroy;

        public LobbyModalStack(Action<GameObject> destroy = null)
        {
            this.destroy = destroy ?? UnityEngine.Object.Destroy;
        }

        public bool HasOpenModal
        {
            get
            {
                PruneDestroyedEntries();
                return entries.Count > 0;
            }
        }

        public void Register(GameObject overlay, Action onDismiss = null)
        {
            if (overlay == null) return;
            RemoveEntry(overlay, false);
            entries.Add(new Entry(overlay, onDismiss));
        }

        public bool Dismiss(GameObject overlay)
        {
            if (overlay == null) return false;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Overlay != overlay) continue;
                var entry = entries[i];
                entries.RemoveAt(i);
                entry.OnDismiss?.Invoke();
                if (entry.Overlay != null) destroy(entry.Overlay);
                return true;
            }
            return false;
        }

        public bool TryDismissTop()
        {
            PruneDestroyedEntries();
            if (entries.Count == 0) return false;

            var index = entries.Count - 1;
            var entry = entries[index];
            entries.RemoveAt(index);
            entry.OnDismiss?.Invoke();
            if (entry.Overlay != null) destroy(entry.Overlay);
            return true;
        }

        private void RemoveEntry(GameObject overlay, bool invokeDismiss)
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Overlay != overlay) continue;
                var entry = entries[i];
                entries.RemoveAt(i);
                if (invokeDismiss) entry.OnDismiss?.Invoke();
            }
        }

        private void PruneDestroyedEntries()
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Overlay != null) continue;
                var entry = entries[i];
                entries.RemoveAt(i);
                entry.OnDismiss?.Invoke();
            }
        }

        private readonly struct Entry
        {
            public Entry(GameObject overlay, Action onDismiss)
            {
                Overlay = overlay;
                OnDismiss = onDismiss;
            }

            public GameObject Overlay { get; }
            public Action OnDismiss { get; }
        }
    }
}
