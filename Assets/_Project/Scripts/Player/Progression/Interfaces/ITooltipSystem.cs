using UnityEngine;

namespace Sol
{
    public interface ITooltipSystem
    {
        void Show(ITooltipContent content, Vector2 position);
        void Hide();
        void UpdatePosition(Vector2 position);
    }
}
