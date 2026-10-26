using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Player Stats tab of the K menu. Shows the six attributes (with spend/refund), derived stats,
    /// vitals and point pools. Reads through services; the only writes are the attribute buttons,
    /// which call the existing ProgressionService methods.
    /// </summary>
    public class PlayerStatsTab : IMenuTab
    {
        public string Title => "Player Stats";
        public string ContentName => "stats-tab-content";

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

        // Derived stats shown on the tab, grouped. Format: F = number with N decimals, P = percent, X = multiplier.
        private static readonly (string Group, StatTypeEnum Stat, string Label, string Format)[] DerivedRows =
        {
            ("Offense", StatTypeEnum.MeleeDamage, "Melee damage", "F1"),
            ("Offense", StatTypeEnum.ProjectileDamage, "Projectile damage", "F1"),
            ("Offense", StatTypeEnum.ProjectileRange, "Projectile range", "F1"),
            ("Offense", StatTypeEnum.SpellPower, "Spell power", "F1"),
            ("Offense", StatTypeEnum.CritChance, "Crit chance", "P"),
            ("Offense", StatTypeEnum.CritMultiplier, "Crit multiplier", "X"),
            ("Defense", StatTypeEnum.ArmorValue, "Armor", "F0"),
            ("Defense", StatTypeEnum.DeflectionChance, "Deflection", "P"),
            ("Defense", StatTypeEnum.MagicResistance, "Magic resistance", "F0"),
            ("Defense", StatTypeEnum.EnvironmentalResistance, "Environmental resistance", "F0"),
            ("Survival", StatTypeEnum.MaxHealth, "Max health", "F0"),
            ("Survival", StatTypeEnum.HealthRegen, "Health regen /s", "F1"),
            ("Survival", StatTypeEnum.MaxEnergy, "Max energy", "F0"),
            ("Survival", StatTypeEnum.EnergyRegen, "Energy regen /s", "F1"),
            ("Utility", StatTypeEnum.CarryCapacity, "Carry capacity (kg)", "F0"),
            ("Utility", StatTypeEnum.InventorySpace, "Inventory space", "F0"),
            ("Utility", StatTypeEnum.LootMultiplier, "Loot multiplier", "X"),
            ("Movement", StatTypeEnum.MoveSpeed, "Move speed", "F2"),
            ("Movement", StatTypeEnum.RunMultiplier, "Run multiplier", "X"),
        };

        private IStatsService _stats;
        private IProgressionService _progression;
        private IPlayerVitals _vitals;

        private Label _pointsLabel;
        private Label _vitalsLabel;
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

            var scroll = new ScrollView { name = "stats-scroll" };
            scroll.style.flexGrow = 1f;
            content.Add(scroll);

            _pointsLabel = Text("", 18, new Color(1f, 0.85f, 0.5f), bold: true);
            scroll.Add(_pointsLabel);

            scroll.Add(Header("Attributes"));
            foreach (StatTypeEnum attr in Attributes)
                scroll.Add(BuildAttributeRow(attr));

            scroll.Add(Header("Vitals"));
            _vitalsLabel = Text("", 14, new Color(0.92f, 0.92f, 0.92f));
            scroll.Add(_vitalsLabel);

            // Derived stats get their own scroll area that fills the remaining height,
            // so a long list scrolls without moving the attributes and vitals above it.
            scroll.Add(Header("Derived Stats"));
            var derivedScroll = new ScrollView(ScrollViewMode.Vertical) { name = "derived-scroll" };
            derivedScroll.style.flexGrow = 1f;
            derivedScroll.style.minHeight = 120f;
            scroll.Add(derivedScroll);
            BuildDerivedGrid(derivedScroll);
        }

        // Lays derived stats out in columns, grouped by category. Columns wrap to use the panel width.
        private void BuildDerivedGrid(VisualElement parent)
        {
            var grid = new VisualElement { name = "derived-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.width = Length.Percent(100f);
            parent.Add(grid);

            string currentGroup = null;
            VisualElement group = null;
            foreach (var row in DerivedRows)
            {
                if (row.Group != currentGroup)
                {
                    currentGroup = row.Group;
                    group = new VisualElement();
                    // Four columns across; the grid wraps extra groups onto new rows below.
                    group.style.width = Length.Percent(100f / DerivedColumnsPerRow);
                    group.style.marginRight = 24f;
                    group.style.marginBottom = 12f;
                    group.Add(Text(currentGroup, 14, new Color(0.8f, 0.6f, 0.3f), bold: true));
                    grid.Add(group);
                }

                var label = Text("", 13, new Color(0.9f, 0.9f, 0.9f));
                _derivedLabels.Add((row.Stat, label));
                group.Add(WithLabelPrefix(row.Label, label));
            }
        }

        // Number of category columns per row. Extra categories wrap onto new rows, so the list grows downward.
        private const int DerivedColumnsPerRow = 4;

        public void OnShow() => Refresh();

        public void OnHide() { }



        private VisualElement BuildAttributeRow(StatTypeEnum attr)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6f;

            var name = Text(attr.ToString(), 15, new Color(1f, 0.9f, 0.6f), bold: true);
            name.style.width = 110f;

            var value = Text("", 15, new Color(0.95f, 0.95f, 0.95f));
            value.style.width = 60f;
            _attributeValueLabels[attr] = value;

            var alloc = Text("", 12, new Color(0.7f, 0.7f, 0.7f));
            alloc.style.width = 110f;
            _attributeAllocLabels[attr] = alloc;

            var minus = new Button(() => OnRefund(attr)) { text = "-" };
            StyleSmallButton(minus);
            _refundButtons[attr] = minus;

            var plus = new Button(() => OnSpend(attr)) { text = "+" };
            StyleSmallButton(plus);
            _spendButtons[attr] = plus;

            var desc = Text(AttributeDescriptions[attr], 12, new Color(0.7f, 0.7f, 0.7f));
            desc.style.flexGrow = 1f;
            desc.style.marginLeft = 10f;
            desc.style.whiteSpace = WhiteSpace.Normal;

            row.Add(name);
            row.Add(value);
            row.Add(minus);
            row.Add(plus);
            row.Add(alloc);
            row.Add(desc);
            return row;
        }

        private void OnSpend(StatTypeEnum attr)
        {
            _progression.TrySpendAttributePoint(attr);
            RefreshAndSave();
        }

        private void OnRefund(StatTypeEnum attr)
        {
            _progression.TryRefundAttributePoint(attr);
            RefreshAndSave();
        }

        // Attribute changes must persist, like talent changes do.
        private void RefreshAndSave()
        {
            ServiceLocator.Get<ISaveManager>()?.RequestLazySave();
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
                int allocated = _progression.GetAllocatedAttributePoints(attr);
                _attributeValueLabels[attr].text = _stats.GetBaseStat(attr).ToString("0");
                _attributeAllocLabels[attr].text = $"+{allocated} allocated";
                _spendButtons[attr].SetEnabled(available > 0);
                _refundButtons[attr].SetEnabled(allocated > 0);
            }

            _vitalsLabel.text = _vitals != null && _vitals.IsInitialized
                ? $"Health {_vitals.CurrentHealth:0} / {_vitals.MaxHealth:0}    " +
                  $"Energy {_vitals.CurrentEnergy:0} / {_vitals.MaxEnergy:0}    " +
                  $"Hunger {_vitals.CurrentHunger:0} / {_vitals.MaxHunger:0}    " +
                  $"Thirst {_vitals.CurrentThirst:0} / {_vitals.MaxThirst:0}"
                : "Vitals not initialized.";

            foreach (var entry in _derivedLabels)
            {
                entry.Value.text = FormatDerived(entry.Stat, _stats.GetStat(entry.Stat));
            }
        }

        private static string FormatDerived(StatTypeEnum stat, float value)
        {
            foreach (var row in DerivedRows)
            {
                if (row.Stat != stat) continue;
                return row.Format switch
                {
                    "P" => $"{value * 100f:0.0}%",
                    "X" => $"{value:0.00}x",
                    "F0" => $"{value:0}",
                    "F1" => $"{value:0.0}",
                    "F2" => $"{value:0.00}",
                    _ => $"{value:0.##}"
                };
            }
            return $"{value:0.##}";
        }

        // Label + value shown side by side in one row.
        private static VisualElement WithLabelPrefix(string text, Label value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2f;

            var name = Text(text, 13, new Color(0.75f, 0.75f, 0.75f));
            name.style.width = 230f;
            row.Add(name);
            row.Add(value);
            return row;
        }

        private static Label Header(string text)
        {
            var label = Text(text, 16, new Color(0.85f, 0.7f, 0.4f), bold: true);
            label.style.marginTop = 14f;
            label.style.marginBottom = 6f;
            return label;
        }

        private static Label Text(string text, int size, Color color, bool bold = false)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static void StyleSmallButton(Button button)
        {
            button.style.width = 26f;
            button.style.height = 24f;
            button.style.marginLeft = 4f;
            button.style.marginRight = 4f;
        }
    }
}


