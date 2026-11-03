using System;

namespace Sol
{
    /// <summary>
    /// The kind of status an effect is, used for immunities. One flag covers every variant of a kind (every stagger, every bleed).
    /// Flags, so a target can be immune to several kinds. Set by each effect type, not per asset.
    /// </summary>
    [Flags]
    public enum StatusKind
    {
        None = 0,
        Stagger = 1 << 0,
        Bleed = 1 << 1,
    }
}
