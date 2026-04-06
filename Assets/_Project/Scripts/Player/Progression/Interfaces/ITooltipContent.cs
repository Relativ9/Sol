using UnityEngine;

namespace Sol
{
    public interface ITooltipContent
    {
        string Title { get; }
        string Description { get; }
        string SubText { get; }
        Sprite Icon { get; }
        Color IconColor { get; }
        
        // float tooltipXSize { get; }
        // float tooltipYSize { get; }
    }
}
