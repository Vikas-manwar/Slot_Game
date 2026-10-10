using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.EditorTools
{
    internal static class UIFactory
    {
        public static readonly Color Gold = new Color32(255, 214, 102, 255);
        public static readonly Color Ice = new Color32(200, 225, 255, 255);
        public static readonly Color Navy = new Color32(25, 6, 87, 255);
        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.75f);

        public static Font Font { get; set; }

        public static RectTransform Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform PlacePixels(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Sprite sprite, bool sliced = false, bool raycast = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text AddText(RectTransform rt, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool shadow = true)
        {
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(8, size / 3);
            text.resizeTextMaxSize = size;

            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = ShadowColor;
                s.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        public static Text AddLabel(RectTransform parent, string name, string content, int size, Color color, float padding = 0f)
        {
            var rt = Stretch(Create(name, parent));
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return AddText(rt, content, size, color);
        }

        public static Button AddSpriteSwapButton(RectTransform rt, Sprite normal, Sprite highlighted, Sprite pressed, Sprite disabled)
        {
            var image = AddImage(rt, normal, raycast: true);
            image.preserveAspect = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                selectedSprite = normal,
                pressedSprite = pressed,
                disabledSprite = disabled != null ? disabled : normal,
            };
            return button;
        }

        public static Button AddTintButton(RectTransform rt, Sprite sprite)
        {
            var image = AddImage(rt, sprite, sliced: true, raycast: true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.75f, 1f);
            colors.pressedColor = new Color(0.75f, 0.65f, 0.55f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }
    }
}
