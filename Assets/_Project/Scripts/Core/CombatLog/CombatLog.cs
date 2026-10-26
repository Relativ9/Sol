using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Keeps a rolling history of gameplay events (vitals changes, environmental triggers).
    /// Registers itself as ICombatLog. The HUD listens to Changed to redraw.
    /// </summary>
    public class CombatLog : MonoBehaviour, ICombatLog
    {
        [SerializeField] private int _capacity = 50;

        private readonly List<CombatLogEntry> _entries = new();

        public event Action Changed;

        public IReadOnlyList<CombatLogEntry> Entries => _entries;

        void Awake()
        {
            ServiceLocator.RegisterService<ICombatLog>(this);
        }

        public void Add(string message, CombatLogKind kind = CombatLogKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;

            _entries.Add(new CombatLogEntry(message, kind));
            while (_entries.Count > _capacity)
                _entries.RemoveAt(0);

            Changed?.Invoke();
        }
    }
}
