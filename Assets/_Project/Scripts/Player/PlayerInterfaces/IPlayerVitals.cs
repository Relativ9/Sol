namespace Sol
{
    public interface IPlayerVitals
    {
        bool IsInitialized { get; }

        float CurrentHealth { get; }
        float MaxHealth { get; }
        float CurrentEnergy { get; }
        float MaxEnergy { get; }
        float CurrentHunger { get; }
        float MaxHunger { get; }
        float CurrentThirst { get; }
        float MaxThirst { get; }

        // source = null means silent (e.g. per-frame drain). A non-null source is written to the combat log.
        void ApplyHealthDelta(float delta, string source = null);
        void ApplyEnergyDelta(float delta, string source = null);
        void ApplyHungerDelta(float delta, string source = null);
        void ApplyThirstDelta(float delta, string source = null);
    }
}
