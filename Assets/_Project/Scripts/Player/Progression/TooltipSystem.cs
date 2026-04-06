using Sol;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipSystem : ITooltipSystem
{
    private readonly VisualElement _tooltipRoot;
    
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _subTextLabel;
    private readonly VisualElement _iconLabel;
    
    public TooltipSystem(VisualElement tooltipRoot)
    {
        _tooltipRoot = tooltipRoot;
        _titleLabel = tooltipRoot.Q<Label>("tooltip-title");
        _descriptionLabel = tooltipRoot.Q<Label>("tooltip-description");
        _subTextLabel = tooltipRoot.Q<Label>("tooltip-sub-text");
        _iconLabel = tooltipRoot.Q<VisualElement>("tooltip-icon");
    }
    
    public void Show(ITooltipContent content, Vector2 position)
    {
        Debug.Log(content);
    }

    public void Hide()
    {
        Debug.Log("hide");
    }

    public void UpdatePosition(Vector2 position)
    {
        Debug.Log(position);
    }
}
