using Sol;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public class TooltipSystem : ITooltipSystem
    {
        private readonly VisualElement _tooltipRoot;
        private readonly VisualElement _tooltipPanel;
    
        private readonly Label _titleLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _subTextLabel;
        private readonly VisualElement _iconLabel;
    
        public TooltipSystem(VisualElement tooltipRoot)
        {
            _tooltipRoot = tooltipRoot;
            _tooltipPanel = tooltipRoot.Q<VisualElement>("tooltip-panel"); 
            _titleLabel = tooltipRoot.Q<Label>("tooltip-title");
            _descriptionLabel = tooltipRoot.Q<Label>("tooltip-description");
            _subTextLabel = tooltipRoot.Q<Label>("tooltip-subtext");
            _iconLabel = tooltipRoot.Q<VisualElement>("tooltip-icon"); 
        }
    
        public void Show(ITooltipContent content, Vector2 position)
        {
            Debug.Log("show");
            _titleLabel.text = content.Title;
            _descriptionLabel.text = content.Description;
            _subTextLabel.text = content.SubText;
            _iconLabel.style.backgroundImage = new StyleBackground(content.Icon);
            _tooltipRoot.style.display = DisplayStyle.Flex;
            _tooltipPanel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _tooltipRoot.style.display = DisplayStyle.None;
            _tooltipPanel.style.display = DisplayStyle.None;
        }

        public void UpdatePosition(Vector2 position)
        {
            _tooltipPanel.style.left = position.x + 15; // offset so it doesn't sit under the cursor
            _tooltipPanel.style.top = position.y + 15;
        }
    }
}

