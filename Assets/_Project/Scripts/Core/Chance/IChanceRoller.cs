namespace Sol
{
    /// <summary>
    /// The single source of randomness for every chance in the game: deflection, procs, status applications,
    /// crits, and any future chance. Route every probability check through here so it is tunable, testable
    /// (inject a seeded implementation), and easy to audit.
    /// </summary>
    public interface IChanceRoller
    {
        /// <summary>True with the given probability (0 = never, 1 = always).</summary>
        bool Roll(float chance);
    }
}
