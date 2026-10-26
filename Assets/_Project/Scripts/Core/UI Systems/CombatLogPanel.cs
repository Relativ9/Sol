using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Rolling combat/stats log in the top-left of the HUD. Shows the newest entries from ICombatLog,
    /// with the newest line at full brightness and older lines fading out.
    /// </summary>
    public class CombatLogPanel : IDisposable
    {
        private const int MaxVisibleLines = 8;

        private ICombatLog _log;
        private VisualElement _lines;

        public void Build(VisualElement hudRoot, ICombatLog log)
        {
            _log = log;

            var panel = new VisualElement { name = "combat-log-panel" };
            panel.pickingMode = PickingMode.Ignore;
            panel.style.position = Position.Absolute;
            panel.style.top = 24f;
            panel.style.left = 24f;
            panel.style.width = 420f;
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 8f;
            panel.style.paddingRight = 8f;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            panel.style.borderTopLeftRadius = 4f;
            panel.style.borderTopRightRadius = 4f;
            panel.style.borderBottomLeftRadius = 4f;
            panel.style.borderBottomRightRadius = 4f;

            var header = new Label("Combat Log");
            header.pickingMode = PickingMode.Ignore;
            header.style.fontSize = 12f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.8f, 0.75f, 0.6f);
            header.style.marginBottom = 4f;
            panel.Add(header);

            _lines = new VisualElement();
            _lines.pickingMode = PickingMode.Ignore;
            _lines.style.flexDirection = FlexDirection.Column;
            panel.Add(_lines);

            hudRoot.Add(panel);

            if (_log != null)
            {
                _log.Changed += Refresh;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_lines == null || _log == null) return;

            _lines.Clear();
            var entries = _log.Entries;
            int start = Math.Max(0, entries.Count - MaxVisibleLines);
            int visible = entries.Count - start;

            for (int i = start; i < entries.Count; i++)
            {
                CombatLogEntry entry = entries[i];

                // Oldest visible line is dimmest, newest is full brightness.
                float t = visible <= 1 ? 1f : (float)(i - start) / (visible - 1);
                float alpha = Mathf.Lerp(0.45f, 1f, t);

                var label = new Label(entry.Message);
                label.pickingMode = PickingMode.Ignore;
                label.style.fontSize = 13f;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginBottom = 2f;
                label.style.color = WithAlpha(ColorFor(entry.Kind), alpha);
                _lines.Add(label);
            }
        }

        private static Color ColorFor(CombatLogKind kind)
        {
            return kind switch
            {
                CombatLogKind.Damage => new Color(1f, 0.45f, 0.40f),
                CombatLogKind.Restore => new Color(0.55f, 1f, 0.60f),
                CombatLogKind.Warning => new Color(1f, 0.80f, 0.30f),
                _ => new Color(0.92f, 0.92f, 0.92f)
            };
        }

        private static Color WithAlpha(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        public void Dispose()
        {
            if (_log != null)
                _log.Changed -= Refresh;
            _log = null;
        }
    }
}
