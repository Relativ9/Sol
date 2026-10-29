using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Glows a world item while it is the pickup target. Works with any material whose shader exposes an emission colour
    /// (for example a URP Shader Graph with an Emission input referenced as "_EmissionColor"). Uses a
    /// MaterialPropertyBlock, so the item's shared material is never changed or duplicated.
    /// Driven by PlayerPickup through SetTargeted, so the glow always matches what E would pick up.
    /// </summary>
    [RequireComponent(typeof(WorldItem))]
    public class WorldItemHighlight : MonoBehaviour
    {
        [Tooltip("Emission colour at full glow. Use an HDR colour for a visible bloom in URP.")]
        [SerializeField] private Color _glowColor = new Color(0.9f, 0.75f, 0.35f) * 2.5f;

        [Tooltip("Renderers to glow. Leave empty to glow every renderer on this object and its children.")]
        [SerializeField] private Renderer[] _renderers;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private bool _targeted;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>();

            // The shared material can ship with an emission value. Start with no glow, so every item begins dark.
            Apply();
        }

        /// <summary>Turns the glow on or off. Called every frame by PlayerPickup with whether this item is the target.</summary>
        public void SetTargeted(bool targeted)
        {
            if (_targeted == targeted) return;
            _targeted = targeted;
            Apply();
        }

        void OnDisable()
        {
            // A disabled item must not keep a glow the next time it is shown.
            _targeted = false;
            Apply();
        }

        private void Apply()
        {
            if (_renderers == null) return;
            Color emission = _targeted ? _glowColor : Color.black;

            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
