using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Thin health bar that follows a TargetHealth in the world. Drawn into the shared HUD document, positioned
    /// each frame from the target's screen point. Hidden until the target is damaged, and hidden at 0 health.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [Tooltip("Height above the target's pivot, in metres.")]
        [SerializeField] private float _heightOffset = 2f;

        [SerializeField] private float _barWidth = 80f;
        [SerializeField] private float _barHeight = 5f;
        [Tooltip("How long the bar stays visible after the last hit, in seconds.")]
        [SerializeField] private float _visibleSeconds = 4f;

        private TargetHealth _target;
        private Camera _camera;
        private VisualElement _bar;
        private VisualElement _fill;
        private float _hideAt;

        public void Attach(TargetHealth target, VisualElement hudRoot)
        {
            _target = target;
            _camera = Camera.main;

            _bar = new VisualElement { name = $"world-hp-{target.GetInstanceID()}" };
            _bar.pickingMode = PickingMode.Ignore;
            _bar.style.position = Position.Absolute;
            _bar.style.width = _barWidth;
            _bar.style.height = _barHeight;
            _bar.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _bar.style.borderTopWidth = 1f;
            _bar.style.borderBottomWidth = 1f;
            _bar.style.borderLeftWidth = 1f;
            _bar.style.borderRightWidth = 1f;
            _bar.style.borderTopColor = new Color(1f, 1f, 1f, 0.4f);
            _bar.style.borderBottomColor = new Color(1f, 1f, 1f, 0.4f);
            _bar.style.borderLeftColor = new Color(1f, 1f, 1f, 0.4f);
            _bar.style.borderRightColor = new Color(1f, 1f, 1f, 0.4f);
            _bar.style.display = DisplayStyle.None;

            _fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
            _fill.style.height = Length.Percent(100f);
            _fill.style.backgroundColor = new Color(0.78f, 0.16f, 0.16f);
            _bar.Add(_fill);

            hudRoot.Add(_bar);
            _target.Changed += OnChanged;
        }

        void OnChanged(float _)
        {
            _hideAt = Time.time + _visibleSeconds;
        }

        void OnDestroy()
        {
            if (_target != null) _target.Changed -= OnChanged;
            _bar?.RemoveFromHierarchy();
        }

        void LateUpdate()
        {
            if (_bar == null || _target == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            bool visible = Time.time < _hideAt && !_target.IsDead;
            _bar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            // Screen point above the target, in UI Toolkit coordinates (origin top-left).
            Vector3 world = transform.position + Vector3.up * _heightOffset;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                _bar.style.display = DisplayStyle.None;
                return;
            }

            _bar.style.left = screen.x - _barWidth * 0.5f;
            _bar.style.top = Screen.height - screen.y - _barHeight;

            float pct = _target.Max > 0f ? Mathf.Clamp01(_target.Current / _target.Max) : 0f;
            _fill.style.width = Length.Percent(pct * 100f);
        }
    }
}
