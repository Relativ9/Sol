using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Base for every item: identity, weight, and its grid footprint. Every item has a weight and a footprint.
    /// Armour, weapons and consumables all inherit from here, so one rule covers all of them.
    /// Footprint is the unrotated shape in cells: width across, height down. e.g. a 1-handed club is 1 x 2, a
    /// 2-handed sword is 1 x 3, a plate tunic is 2 x 3.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Sol/Item Data/Item")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable ID written to saves. Never change it once an item has been saved, or the item is lost.")]
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ItemCategory _itemCategory;

        [Header("Inventory")]
        [Tooltip("Weight in kg. Every item has one. Armour weight feeds the derived resistances.")]
        [SerializeField, Min(0f)] private float _weight;
        [Tooltip("Largest number of this item in one stack. 1 for anything that does not stack.")]
        [SerializeField, Min(1)] private int _maxStackSize = 1;

        [Tooltip("Whether the item can be turned 90 degrees to fit. Rigid items usually can; long items may not.")]
        [SerializeField] private bool _rotatable = true;

        [Header("Material")]
        [Tooltip("Material tier. Sets the material efficiency that scales armour value (Iron 0.8 up to Aetheric Weave 2.0).")]
        [SerializeField] private MaterialTier _materialTier = MaterialTier.Steel;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;

        [Tooltip("The model spawned when this item is dropped in the world. Its root needs a WorldItem component. Without it the item cannot be dropped.")]
        [SerializeField] private GameObject _worldPrefab;

        public string ItemId => _itemId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public ItemCategory Category => _itemCategory;
        public float Weight => _weight;
        public int MaxStackSize => _maxStackSize;
        // A footprint is a rule of the item's type, never a per-asset value. Equipment derives its size from its slot, weapon
        // or offhand from its type (see EquipmentSilhouette). Items that are not equipment have no footprint rule yet, so
        // they take this single cell. That is a code default, not something a designer sets.
        public virtual int GridWidth => 1;
        public virtual int GridHeight => 1;
        public bool Rotatable => _rotatable;
        public Sprite Icon => _icon;
        public GameObject WorldPrefab => _worldPrefab;
        public MaterialTier Material => _materialTier;
        public float MaterialEfficiency => _materialTier.Efficiency();
    }
}
