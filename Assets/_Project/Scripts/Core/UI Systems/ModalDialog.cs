using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Generic modal dialog built into a UIDocument. Its buttons are clickable with the virtual cursor
    /// because UIManager makes the document active. Blocks clicks underneath while visible.
    /// </summary>
    public class ModalDialog : IDisposable
    {
        private VisualElement _overlay;
        private Label _titleLabel;
        private Label _bodyLabel;
        private VisualElement _buttonRow;
        private Action<int> _onChoice;

        public void Build(VisualElement root)
        {
            _overlay = new VisualElement { name = "modal-overlay" };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0f;
            _overlay.style.right = 0f;
            _overlay.style.top = 0f;
            _overlay.style.bottom = 0f;
            _overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            // Blocks clicks to the game and HUD underneath while visible.
            _overlay.pickingMode = PickingMode.Position;

            var box = new VisualElement { name = "modal-box" };
            box.style.width = 460f;
            box.style.paddingTop = 20f;
            box.style.paddingBottom = 20f;
            box.style.paddingLeft = 24f;
            box.style.paddingRight = 24f;
            box.style.backgroundColor = new Color(0.12f, 0.1f, 0.08f, 0.97f);
            SetRadius(box, 6f);
            SetBorder(box, 2f, new Color(0.8f, 0.6f, 0.25f));

            _titleLabel = new Label();
            _titleLabel.style.fontSize = 20f;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = new Color(1f, 0.85f, 0.5f);
            _titleLabel.style.marginBottom = 10f;
            box.Add(_titleLabel);

            _bodyLabel = new Label();
            _bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            _bodyLabel.style.fontSize = 14f;
            _bodyLabel.style.color = new Color(0.92f, 0.92f, 0.92f);
            _bodyLabel.style.marginBottom = 14f;
            box.Add(_bodyLabel);

            _buttonRow = new VisualElement();
            _buttonRow.style.flexDirection = FlexDirection.Row;
            _buttonRow.style.justifyContent = Justify.SpaceBetween;
            box.Add(_buttonRow);

            _overlay.Add(box);
            _overlay.style.display = DisplayStyle.None;
            root.Add(_overlay);
        }

        public void Show(string title, string body, string[] buttonLabels, Action<int> onChoice)
        {
            _onChoice = onChoice;
            _titleLabel.text = title;
            _bodyLabel.text = body;

            _buttonRow.Clear();
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                int index = i; // capture for the lambda
                var button = new Button(() => Resolve(index)) { text = buttonLabels[i] };
                button.style.flexGrow = 1f;
                button.style.height = 36f;
                button.style.marginLeft = 6f;
                button.style.marginRight = 6f;
                button.style.fontSize = 14f;
                _buttonRow.Add(button);
            }

            _overlay.style.display = DisplayStyle.Flex;
        }

        private void Resolve(int index)
        {
            _overlay.style.display = DisplayStyle.None;
            var callback = _onChoice;
            _onChoice = null;
            callback?.Invoke(index);
        }

        private static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r;
            e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r;
            e.style.borderBottomRightRadius = r;
        }

        private static void SetBorder(VisualElement e, float width, Color c)
        {
            e.style.borderTopWidth = width;
            e.style.borderBottomWidth = width;
            e.style.borderLeftWidth = width;
            e.style.borderRightWidth = width;
            e.style.borderTopColor = c;
            e.style.borderBottomColor = c;
            e.style.borderLeftColor = c;
            e.style.borderRightColor = c;
        }

        public void Dispose()
        {
            _overlay?.RemoveFromHierarchy();
            _overlay = null;
            _onChoice = null;
        }
    }
}
