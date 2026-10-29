using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Player Stats tab of the K menu. The layout comes from PlayerStatsTab.uxml and MenuTheme.uss. This class fills the
    /// named containers: attributes with spend and refund, vitals, and derived stats as flowing category sections.
    /// Only Attribute and Derived stats are shown (see StatRoles). Tuning stats never appear here.
    /// The only writes are the attribute buttons, which call ProgressionService.
    /// </summary>
    public class PlayerStatsTab : IMenuTab
    {
        public string Title => "Player Stats";
        public string ContentName => "stats-tab-content";

        // The layout is a serialized asset, handed in by UIManager (the same way its other templates are supplied).
        private readonly VisualTreeAsset _layout;

        public PlayerStatsTab(VisualTreeAsset layout)
        {
            _layout = layout;
        }

        // Cap for the armour set count. A full set is 5 pieces, shown as n/5 and never as a stat cap.
        private const int MaxArmourPieces = 5;

        private static readonly StatTypeEnum[] Attributes =
        {
            StatTypeEnum.Brawn, StatTypeEnum.Finesse, StatTypeEnum.Insight,
            StatTypeEnum.Focus, StatTypeEnum.Vigor, StatTypeEnum.Willpower
        };

        // Each attribute's primary effect, from "Player Stats and Levels".
        private static readonly Dictionary<StatTypeEnum, string> AttributeDescriptions = new()
        {
            { StatTypeEnum.Brawn, "Melee damage, carry capacity, intimidation" },
            { StatTypeEnum.Finesse, "Archery damage, sprint speed, fall damage, stealth, lock-picking" },
            { StatTypeEnum.Insight, "Spell power, critical multiplier, magic resistance, knowledge" },
            { StatTypeEnum.Focus, "Critical chance, projectile range, parry/block timing, hidden loot" },
            { StatTypeEnum.Vigor, "Maximum health, energy regen, environmental and trap damage" },
            { StatTypeEnum.Willpower, "Maximum energy, global cooldown, spell cost, charm" }
        };

        // Derived stats shown on the tab, grouped in order. Format: F = decimals, P = percent, X = multiplier, N = n/5.
        // Every row is filtered through StatRoles.IsVisible before it is shown, so a Tuning stat can never appear here.
        private static readonly (string Group, StatTypeEnum Stat, string Label, string Format)[] DerivedRows =
        {
            ("Offense", StatTypeEnum.MeleeDamage, "Melee damage", "F1"),
            ("Offense", StatTypeEnum.ProjectileDamage, "Projectile damage", "F1"),
            ("Offense", StatTypeEnum.ProjectileRange, "Projectile range", "F1"),
            ("Offense", StatTypeEnum.SpellPower, "Spell power", "F1"),
            ("Offense", StatTypeEnum.CritChance, "Crit chance", "P"),
            ("Offense", StatTypeEnum.CritMultiplier, "Crit multiplier", "X"),
            ("Offense", StatTypeEnum.Penetration, "Penetration", "F1"),
            ("Defense", StatTypeEnum.ArmorValue, "Armor", "F0"),
            ("Defense", StatTypeEnum.DeflectionChance, "Deflection", "P"),
            ("Defense", StatTypeEnum.MagicResistance, "Magic resistance", "F0"),
            ("Defense", StatTypeEnum.FireResistance, "Fire resistance", "F0"),
            ("Defense", StatTypeEnum.IceResistance, "Ice resistance", "F0"),
            ("Defense", StatTypeEnum.VoidResistance, "Void resistance", "F0"),
            ("Defense", StatTypeEnum.SonicResistance, "Sonic resistance", "F0"),
            ("Defense", StatTypeEnum.SoulResistance, "Soul resistance", "F0"),
            ("Defense", StatTypeEnum.StaggerResistance, "Stagger resistance", "P"),
            ("Defense", StatTypeEnum.BleedResistance, "Bleed resistance", "P"),
            ("Defense", StatTypeEnum.EnvironmentalResistance, "Environmental resistance", "F0"),
            ("Armour set", StatTypeEnum.PlateCount, "Plate pieces", "N"),
            ("Armour set", StatTypeEnum.FlexCount, "Flex pieces", "N"),
            ("Armour set", StatTypeEnum.ClothCount, "Cloth pieces", "N"),
            ("Survival", StatTypeEnum.MaxHealth, "Max health", "F0"),
            ("Survival", StatTypeEnum.HealthRegen, "Health regen /s", "F1"),
            ("Survival", StatTypeEnum.MaxEnergy, "Max energy", "F0"),
            ("Survival", StatTypeEnum.EnergyRegen, "Energy regen /s", "F1"),
            ("Utility", StatTypeEnum.CarryCapacity, "Carry capacity (kg)", "F0"),
            ("Utility", StatTypeEnum.InventorySpace, "Inventory space", "F0"),
            ("Utility", StatTypeEnum.LootMultiplier, "Loot multiplier", "X"),
        };

        private IStatsService _stats;
        private IProgressionService _progression;
        private IPlayerVitals _vitals;

        private Label _pointsLabel;
        private VisualElement _vitalsRow;
        private readonly Label[] _vitalLabels = new Label[4];
        private ScrollView _scroll;
        private VisualElement _attributeList;
        private VisualElement _derivedFlow;

        private const float ScrollStepPixels = 40f;

        private readonly Dictionary<StatTypeEnum, Label> _attributeValueLabels = new();
        private readonly Dictionary<StatTypeEnum, Label> _attributeAllocLabels = new();
        private readonly Dictionary<StatTypeEnum, Button> _spendButtons = new();
        private readonly Dictionary<StatTypeEnum, Button> _refundButtons = new();
        private readonly List<(StatTypeEnum Stat, Label Value)> _derivedLabels = new();

        public void Build(VisualElement content)
        {
            content.Clear();
            _stats = ServiceLocator.Get<IStatsService>();
            _progression = ServiceLocator.Get<IProgressionService>();
            _vitals = ServiceLocator.TryGet<IPlayerVitals>();

            // The layout is authored in UXML. Cloning it fills this tab's content element, and the stylesheet applies.
            if (_layout == null)
            {
                Debug.LogError($"[{nameof(PlayerStatsTab)}] No layout assigned on UIManager. The stats tab cannot be built.");
                return;
            }
            _layout.CloneTree(content);

            _scroll = content.Q<ScrollView>("stats-scroll");
            _pointsLabel = content.Q<Label>("stats-points");
            _vitalsRow = content.Q("stats-vitals");
            for (int i = 0; i < _vitalLabels.Length; i++)
            {
                _vitalLabels[i] = Text("", "stats-vital");
                _vitalsRow.Add(_vitalLabels[i]);
            }
            _attributeList = content.Q("stats-attributes");
            _derivedFlow = content.Q("stats-derived");

            BuildAttributes();
            BuildDerived();
        }

        public void OnShow() => Refresh();

        public void OnHide() { }

        // The virtual cursor sends no wheel events, so UIManager forwards its scroll notches here while this tab is open.
        // Inverted to match the talent tree: wheel up moves the list up, wheel down moves it down.
        public void ScrollDerivedBy(int notches)
        {
            if (_scroll == null || notches == 0) return;
            Vector2 offset = _scroll.scrollOffset;
            offset.y = Mathf.Max(0f, offset.y - notches * ScrollStepPixels);
            _scroll.scrollOffset = offset;
        }

        private void BuildAttributes()
        {
            foreach (StatTypeEnum attr in Attributes)
            {
                // Only Attribute-role stats may be spent on. Checked here, so a role change is caught in this tab.
                if (!StatRoles.IsAttribute(attr)) continue;
                _attributeList.Add(BuildAttributeRow(attr));
            }
        }

        // Each derived category is its own section in a wrapping row, so Defense sits beside Offense and Armour set
        // beside Defense while there is room, and wraps only when the panel runs out of width.
        private void BuildDerived()
        {
            var sections = new Dictionary<string, VisualElement>();
            foreach (var row in DerivedRows)
            {
                if (!StatRoles.IsVisible(row.Stat)) continue;

                if (!sections.TryGetValue(row.Group, out VisualElement section))
                {
                    section = new VisualElement();
                    section.AddToClassList("stats-category");
                    section.Add(Text(row.Group, "stats-category-title"));
                    sections[row.Group] = section;
                    _derivedFlow.Add(section);
                }

                var value = Text("", "menu-value");
                _derivedLabels.Add((row.Stat, value));
                section.Add(StatRow(row.Label, value));
            }
        }

        private VisualElement BuildAttributeRow(StatTypeEnum attr)
        {
            var row = new VisualElement();
            row.AddToClassList("stats-attribute-row");

            var name = Text(attr.ToString(), "stats-attribute-name");

            var value = Text("", "stats-attribute-value");
            _attributeValueLabels[attr] = value;

            var minus = new Button(() => OnRefund(attr)) { text = "-" };
            minus.AddToClassList("menu-button");
            _refundButtons[attr] = minus;

            var plus = new Button(() => OnSpend(attr)) { text = "+" };
            plus.AddToClassList("menu-button");
            _spendButtons[attr] = plus;

            var alloc = Text("", "stats-attribute-alloc");
            _attributeAllocLabels[attr] = alloc;

            var desc = Text(AttributeDescriptions[attr], "stats-attribute-desc");

            row.Add(name);
            row.Add(value);
            row.Add(minus);
            row.Add(plus);
            row.Add(alloc);
            row.Add(desc);
            return row;
        }

        // Attribute changes are state. They are written by the next quicksave or autosave, not here.
        private void OnSpend(StatTypeEnum attr)
        {
            _progression.TrySpendAttributePoint(attr);
            Refresh();
        }

        private void OnRefund(StatTypeEnum attr)
        {
            _progression.TryRefundAttributePoint(attr);
            Refresh();
        }

        private void Refresh()
        {
            if (_stats == null || _progression == null) return;

            int available = _progression.GetAvailableAttributePoints();
            int total = _progression.GetTotalAttributePoints();
            _pointsLabel.text = $"Attribute points: {available} available / {total} total";

            foreach (StatTypeEnum attr in Attributes)
            {
                if (!_attributeValueLabels.ContainsKey(attr)) continue;
                int allocated = _progression.GetAllocatedAttributePoints(attr);
                _attributeValueLabels[attr].text = _stats.GetBaseStat(attr).ToString("0");
                _attributeAllocLabels[attr].text = $"+{allocated} allocated";
                _spendButtons[attr].SetEnabled(available > 0);
                _refundButtons[attr].SetEnabled(allocated > 0);
            }

            if (_vitals != null && _vitals.IsInitialized)
            {
                _vitalLabels[0].text = $"Health {_vitals.CurrentHealth:0} / {_vitals.MaxHealth:0}";
                _vitalLabels[1].text = $"Energy {_vitals.CurrentEnergy:0} / {_vitals.MaxEnergy:0}";
                _vitalLabels[2].text = $"Hunger {_vitals.CurrentHunger:0} / {_vitals.MaxHunger:0}";
                _vitalLabels[3].text = $"Thirst {_vitals.CurrentThirst:0} / {_vitals.MaxThirst:0}";
            }
            else
            {
                _vitalLabels[0].text = "Vitals not initialized.";
                for (int i = 1; i < _vitalLabels.Length; i++) _vitalLabels[i].text = "";
            }

            foreach (var entry in _derivedLabels)
            {
                entry.Value.text = StatCaps.IsCapped(entry.Stat)
                    ? $"{_stats.GetStat(entry.Stat):0}/{StatCaps.ResistanceCap:0}"
                    : FormatDerived(entry.Stat, _stats.GetStat(entry.Stat));
            }
        }

        private static string FormatDerived(StatTypeEnum stat, float value)
        {
            foreach (var row in DerivedRows)
            {
                if (row.Stat != stat) continue;
                return row.Format switch
                {
                    "P" => $"{value:0.0}%", // Chance stats are stored as points (5 = 5%), so no scaling here.
                    "N" => $"{value:0}/{MaxArmourPieces}",
                    "X" => $"{value:0.00}x",
                    "F0" => $"{value:0}",
                    "F1" => $"{value:0.0}",
                    "F2" => $"{value:0.00}",
                    _ => $"{value:0.##}"
                };
            }
            return $"{value:0.##}";
        }

        // A label and its value, side by side. The label takes the free width, and the value keeps its size.
        private static VisualElement StatRow(string text, Label value)
        {
            var row = new VisualElement();
            row.AddToClassList("stats-row");
            var name = Text(text, "menu-label");
            name.style.flexGrow = 1f;
            name.style.flexShrink = 1f;
            row.Add(name);
            row.Add(value);
            return row;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
