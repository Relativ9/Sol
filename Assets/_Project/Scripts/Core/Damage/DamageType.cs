namespace Sol
{
    /// <summary>
    /// Kinds of damage. Identity only: display data and the resistance stat each type uses live in DamageTypeTableSO.
    /// Saved enums are stored as integers, so new members go only at the end. Never insert, reorder, or rename.
    /// Environmental is deliberately not a type: traps deal real types and carry DamageTag.Environmental.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Void,
        Sonic,
        Soul,
        Magic,
    }
}
