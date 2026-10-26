using System;
using System.Collections.Generic;

namespace Sol
{
    public enum CombatLogKind
    {
        Info,
        Damage,
        Restore,
        Warning
    }

    public readonly struct CombatLogEntry
    {
        public readonly string Message;
        public readonly CombatLogKind Kind;

        public CombatLogEntry(string message, CombatLogKind kind)
        {
            Message = message;
            Kind = kind;
        }
    }

    public interface ICombatLog
    {
        event Action Changed;
        IReadOnlyList<CombatLogEntry> Entries { get; }
        void Add(string message, CombatLogKind kind = CombatLogKind.Info);
    }
}
