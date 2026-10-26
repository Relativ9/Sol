using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Bottom-left vitals bars (Health, Energy, Hunger, Thirst).
    /// Built once into the existing "hud-root" container in GlobalUI.uxml, then updated every frame from IPlayerVitals.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        private class VitalBar
        {
            public VisualElement Fill;
            public Label ValueLabel;
            public Func<IPlayerVitals, float> Current;
            public Func<IPlayerVitals, float> Max;
        }

        private readonly List<VitalBar> _bars = new();
        private IPlayerVitals _vitals;
        private CombatLogPanel _combatLog;

        public void Build(VisualElement root)
        {
            var hudRoot = root.Q("hud-root");
            if (hudRoot == null)
            {
                Debug.LogError("[PlayerHUD] 'hud-root' not found in the UI document.");
                return;
            }

            // HUD must never block clicks on the talent panel or the virtual cursor.
            hudRoot.pickingMode = PickingMode.Ignore;
            hudRoot.Clear();
            _bars.Clear();

            _combatLog = new CombatLogPanel();
            _combatLog.Build(hudRoot, ServiceLocator.Get<ICombatLog>());

            var panel = new VisualElement { name = "vitals-panel" };
            panel.pickingMode = PickingMode.Ignore;
            panel.style.position = Position.Absolute;
            panel.style.left = 24f;
            panel.style.bottom = 24f;
            panel.style.width = 300f;
            panel.style.flexDirection = FlexDirection.Column;
            hudRoot.Add(panel);

            _bars.Add(CreateBar(panel, "Health", new Color(0.78f, 0.16f, 0.16f), v => v.CurrentHealth, v => v.MaxHealth));
            _bars.Add(CreateBar(panel, "Energy", new Color(0.24f, 0.55f, 0.92f), v => v.CurrentEnergy, v => v.MaxEnergy));
            _bars.Add(CreateBar(panel, "Hunger", new Color(0.75f, 0.50f, 0.20f), v => v.CurrentHunger, v => v.MaxHunger));
            _bars.Add(CreateBar(panel, "Thirst", new Color(0.27f, 0.75f, 0.78f), v => v.CurrentThirst, v => v.MaxThirst));
        }

        private static VitalBar CreateBar(VisualElement parent, string label, Color color,
            Func<IPlayerVitals, float> current, Func<IPlayerVitals, float> max)
        {
            var row = new VisualElement();
            row.pickingMode = PickingMode.Ignore;
            row.style.marginBottom = 8f;

            var header = new VisualElement();
            header.pickingMode = PickingMode.Ignore;
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 2f;

            var nameLabel = new Label(label);
            nameLabel.pickingMode = PickingMode.Ignore;
            nameLabel.style.fontSize = 13f;
            nameLabel.style.color = new Color(0.95f, 0.92f, 0.85f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var valueLabel = new Label("0 / 0");
            valueLabel.pickingMode = PickingMode.Ignore;
            valueLabel.style.fontSize = 12f;
            valueLabel.style.color = new Color(0.85f, 0.85f, 0.85f);

            header.Add(nameLabel);
            header.Add(valueLabel);
            row.Add(header);

            // Track (background)
            var track = new VisualElement();
            track.pickingMode = PickingMode.Ignore;
            track.style.height = 14f;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            track.style.borderTopLeftRadius = 3f;
            track.style.borderTopRightRadius = 3f;
            track.style.borderBottomLeftRadius = 3f;
            track.style.borderBottomRightRadius = 3f;
            track.style.borderTopWidth = 1f;
            track.style.borderBottomWidth = 1f;
            track.style.borderLeftWidth = 1f;
            track.style.borderRightWidth = 1f;
            track.style.borderTopColor = new Color(1f, 1f, 1f, 0.25f);
            track.style.borderBottomColor = new Color(1f, 1f, 1f, 0.25f);
            track.style.borderLeftColor = new Color(1f, 1f, 1f, 0.25f);
            track.style.borderRightColor = new Color(1f, 1f, 1f, 0.25f);

            // Fill (foreground), width driven as a percentage of the track
            var fill = new VisualElement();
            fill.pickingMode = PickingMode.Ignore;
            fill.style.height = Length.Percent(100f);
            fill.style.width = Length.Percent(100f);
            fill.style.backgroundColor = color;
            fill.style.borderTopLeftRadius = 3f;
            fill.style.borderBottomLeftRadius = 3f;

            track.Add(fill);
            row.Add(track);
            parent.Add(row);

            return new VitalBar { Fill = fill, ValueLabel = valueLabel, Current = current, Max = max };
        }

        void OnDestroy()
        {
            _combatLog?.Dispose();
        }

        void Update()
        {
            if (_vitals == null)
                _vitals = ServiceLocator.TryGet<IPlayerVitals>(); // per-frame until available, so no error logging
            if (_vitals == null || !_vitals.IsInitialized) return;

            foreach (var bar in _bars)
            {
                float cur = bar.Current(_vitals);
                float max = bar.Max(_vitals);
                float pct = max > 0f ? Mathf.Clamp01(cur / max) : 0f;

                bar.Fill.style.width = Length.Percent(pct * 100f);
                bar.ValueLabel.text = $"{Mathf.FloorToInt(cur)} / {Mathf.FloorToInt(max)}";
            }
        }
    }
}
