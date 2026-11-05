using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace QuickPing.Utilities
{
    /// <summary>
    /// Valheim-styled UI widgets built from the game's own sprites and fonts.
    /// </summary>
    internal static class GameUI
    {
        private static readonly Color PlaceholderColor = Color.grey;
        private static readonly Color CheckmarkColor = new Color(1f, 0.678f, 0.103f, 1f);

        private static readonly ColorBlock ToggleColors = new ColorBlock
        {
            normalColor = new Color(0.61f, 0.61f, 0.61f, 1f),
            highlightedColor = Color.white,
            pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };

        private static readonly Dictionary<string, Object> assets = new();
        private static GameObject canvas;

        /// <summary>
        /// Canvas drawn above the game GUI, created on first use in a world.
        /// </summary>
        public static Transform Canvas
        {
            get
            {
                if (!canvas) canvas = CreateCanvas();
                return canvas ? canvas.transform : null;
            }
        }

        public static GameObject CreateWoodPanel(Transform parent, float width, float height)
        {
            GameObject panel = DefaultControls.CreatePanel(Resources());
            panel.layer = 5;
            panel.transform.SetParent(parent, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Image image = panel.GetComponent<Image>();
            image.sprite = GetSprite("woodpanel_trophys");
            image.type = Image.Type.Sliced;
            image.material = Get<Material>("litpanel");
            image.color = Color.white;

            panel.AddComponent<DragWindow>();
            return panel;
        }

        public static InputField CreateInputField(Transform parent, string placeholder, int fontSize, float width, float height)
        {
            GameObject go = DefaultControls.CreateInputField(Resources());
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(width, height);

            InputField field = go.GetComponent<InputField>();
            field.contentType = InputField.ContentType.Standard;
            if (field.targetGraphic is Image image)
            {
                image.color = Color.white;
                image.sprite = GetSprite("text_field");
            }

            if (field.placeholder is Text placeholderText)
            {
                placeholderText.text = placeholder;
                StyleText(placeholderText, PlaceholderColor, fontSize, false);
            }

            StyleText(field.textComponent, Color.white, fontSize, true);
            return field;
        }

        /// <summary>
        /// Checkbox with a "Label" text child.
        /// </summary>
        public static Toggle CreateToggle(Transform parent, string label, float size)
        {
            GameObject go = DefaultControls.CreateToggle(Resources());
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
            ((RectTransform)go.transform.Find("Background")).sizeDelta = new Vector2(size, size);
            ((RectTransform)go.transform.Find("Background/Checkmark")).sizeDelta = new Vector2(size, size);

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.toggleTransition = Toggle.ToggleTransition.Fade;
            toggle.colors = ToggleColors;
            if (toggle.targetGraphic is Image background)
                background.sprite = GetSprite("checkbox");
            if (toggle.graphic is Image checkmark)
            {
                checkmark.sprite = GetSprite("checkbox_marker");
                checkmark.color = CheckmarkColor;
            }

            Text text = go.transform.Find("Label").GetComponent<Text>();
            text.text = label;
            StyleText(text, Color.white, 16, true);
            // The label sits right of the box, wider than the toggle itself
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return toggle;
        }

        private static void StyleText(Text text, Color color, int fontSize, bool outline)
        {
            text.font = Get<Font>("AveriaSerifLibre-Bold");
            text.fontSize = fontSize;
            text.color = color;
            // Unity's default controls leave less height than this font needs, and a line that doesn't fit is hidden
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleLeft;
            if (outline && !text.GetComponent<Outline>())
                text.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        private static DefaultControls.Resources Resources()
        {
            return new DefaultControls.Resources
            {
                standard = GetSprite("button"),
                background = GetSprite("text_field"),
                inputField = GetSprite("text_field"),
                knob = GetSprite("checkbox_marker"),
                checkmark = GetSprite("checkbox_marker"),
                dropdown = GetSprite("checkbox_marker"),
            };
        }

        /// <summary>
        /// Game UI sprite, from the UI atlas when it has it.
        /// </summary>
        private static Sprite GetSprite(string name)
        {
            SpriteAtlas atlas = Get<SpriteAtlas>("UIAtlas");
            Sprite sprite = atlas ? atlas.GetSprite(name) : null;
            return sprite ? sprite : Get<Sprite>(name);
        }

        /// <summary>
        /// Loaded game asset by name, cached until it is unloaded.
        /// </summary>
        private static T Get<T>(string name) where T : Object
        {
            string key = typeof(T).Name + "/" + name;
            if (assets.TryGetValue(key, out Object cached) && cached)
                return (T)cached;

            T asset = UnityEngine.Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(a => a.name == name);
            if (!asset)
                QuickPingPlugin.Log.LogWarning($"UI asset not found: {key}");
            assets[key] = asset;
            return asset;
        }

        private static GameObject CreateCanvas()
        {
            Transform root = SceneManager.GetActiveScene().GetRootGameObjects()
                .FirstOrDefault(go => go.name == "_GameMain")?.transform.Find("LoadingGUI");
            if (!root)
            {
                QuickPingPlugin.Log.LogError("Game GUI not found");
                return null;
            }

            var go = new GameObject("QuickPingGUI", typeof(RectTransform), typeof(GuiPixelFix),
                typeof(UnityEngine.Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(root, false);
            go.transform.SetAsLastSibling();

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var c = go.GetComponent<UnityEngine.Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.overrideSorting = true;
            c.sortingOrder = 2000;
            c.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            go.GetComponent<CanvasScaler>().referencePixelsPerUnit = 50f;
            return go;
        }

        /// <summary>
        /// Lets the player move a window by dragging it.
        /// </summary>
        private class DragWindow : MonoBehaviour, IDragHandler
        {
            public void OnDrag(PointerEventData eventData)
            {
                var c = GetComponentInParent<UnityEngine.Canvas>();
                ((RectTransform)transform).anchoredPosition += eventData.delta / (c ? c.scaleFactor : 1f);
            }
        }
    }
}
