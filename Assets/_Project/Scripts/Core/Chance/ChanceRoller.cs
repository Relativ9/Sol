using System;

namespace Sol
{
    /// <summary>
    /// Default IChanceRoller backed by System.Random. Pass a seed to make results reproducible in tests.
    /// Plain C#: registered with ServiceLocator by GameInitializer, so no scene object is needed.
    /// </summary>
    public class ChanceRoller : IChanceRoller
    {
        private readonly Random _random;

        public ChanceRoller() : this(Environment.TickCount) { }

        public ChanceRoller(int seed)
        {
            _random = new Random(seed);
        }

        public bool Roll(float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return _random.NextDouble() < chance;
        }
    }
}
