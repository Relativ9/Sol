using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Base for every status effect asset. Holds the fields all effects share. Each subclass is one effect type
    /// (stagger, bleed, silence) and adds only the fields it needs. Create one asset per variant, e.g. "Large Stagger".
    /// Implements IStatusEffect so receivers never see the subclasses.
    /// </summary>
    public abstract class StatusEffectSO : ScriptableObject, IStatusEffect
    {
        [Header("Identity")]
        [Tooltip("Shown in the combat log on apply.")]
        [SerializeField] private string _displayName = "Status";
        [Tooltip("Written to the combat log when the status runs out. Empty = silent.")]
        [SerializeField] private string _expireMessage = "";

        [Header("Duration")]
        [Tooltip("Scaled game seconds the status lasts. Each new application refreshes it to this.")]
        [SerializeField] private float _durationSeconds = 1f;
        [SerializeField] private StatusStackingMode _stackingMode = StatusStackingMode.RefreshSingle;

        public string DisplayName => _displayName;
        public string ExpireMessage => _expireMessage;
        public float DurationSeconds => _durationSeconds;
        public StatusStackingMode StackingMode => _stackingMode;

        // Defaults for locks and per-application values. Subclasses override only what they use.
        public virtual bool LocksMovement => false;
        public virtual bool LocksCombat => false;
        public virtual float EnergyCostOnApply => 0f;
        public virtual float KnockbackSpeed => 0f;
        public virtual string AnimatorTrigger => "";
        public virtual float DamageFractionOfRaw => 0f;
        public virtual float TickIntervalSeconds => 1f;
    }
}
