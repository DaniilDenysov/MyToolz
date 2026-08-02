using MyToolz.EditorToolz;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Plain margins/padding value (Odin-serialization-safe, unlike <see cref="RectOffset"/> which
    /// wraps native memory). Convert with <see cref="ToRectOffset"/> where Unity needs one.
    /// </summary>
    [Serializable]
    public struct Margins
    {
        //[HorizontalGroup("m"), LabelWidth(30)] 
        public int left;
        //[HorizontalGroup("m"), LabelWidth(38)] 
        public int right;
        //[HorizontalGroup("m"), LabelWidth(28)] 
        public int top;
        //[HorizontalGroup("m"), LabelWidth(48)] 
        public int bottom;

        public Margins(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public bool IsZero => left == 0 && right == 0 && top == 0 && bottom == 0;
        public RectOffset ToRectOffset() => new RectOffset(left, right, top, bottom);
        public static Margins All(int value) => new Margins(value, value, value, value);
    }

    /// <summary>
    /// Optional sizing for a content node, mapped to a <see cref="LayoutElement"/> at bake time.
    /// -1 means "unset" (Unity's LayoutElement convention). Grow/shrink apply in Flex containers only.
    /// </summary>
    [Serializable]
    public class SizeSettings
    {
        //[HorizontalGroup("min"), LabelText("Min W"), LabelWidth(50)] 
        public float minWidth = -1f;
        //[HorizontalGroup("min"), LabelText("Min H"), LabelWidth(50)] 
        public float minHeight = -1f;
        //[HorizontalGroup("pref"), LabelText("Pref W"), LabelWidth(50)] 
        public float preferredWidth = -1f;
        //[HorizontalGroup("pref"), LabelText("Pref H"), LabelWidth(50)] 
        public float preferredHeight = -1f;
        //[HorizontalGroup("flexi"), LabelText("Flex W"), LabelWidth(50)] 
        public float flexibleWidth = -1f;
        //[HorizontalGroup("flexi"), LabelText("Flex H"), LabelWidth(50)] 
        public float flexibleHeight = -1f;

        //[HorizontalGroup("flex"), LabelText("Grow"), LabelWidth(50)]
        [Tooltip("Flex containers only: share of free space this child takes.")]
        public float flexGrow;

        //[HorizontalGroup("flex"), LabelText("Shrink"), LabelWidth(50)]
        [Tooltip("Flex containers only: how much this child gives up when space is short.")]
        public float flexShrink = 1f;

        public bool HasLayoutElementValues =>
            minWidth >= 0f || minHeight >= 0f || preferredWidth >= 0f || preferredHeight >= 0f ||
            flexibleWidth >= 0f || flexibleHeight >= 0f;

        public void ApplyTo(LayoutElement element)
        {
            if (element == null) return;
            element.minWidth = minWidth;
            element.minHeight = minHeight;
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
        }
    }

    /// <summary>
    /// A color that is either a reference into the active template's <see cref="Palette"/> (so theme
    /// swaps re-tint it) or a hard custom value. Prefer palette roles for consistency (aim #3).
    /// </summary>
    [Serializable]
    public struct PaletteColor
    {
        //[HorizontalGroup("c"), HideLabel] 
        public PaletteRole role;
        //[HorizontalGroup("c"), HideLabel, ShowIf(nameof(role), PaletteRole.Custom)] 
        public Color custom;

        public PaletteColor(PaletteRole role)
        {
            this.role = role;
            custom = Color.white;
        }

        public PaletteColor(Color custom)
        {
            role = PaletteRole.Custom;
            this.custom = custom;
        }

        public Color Resolve(Palette palette)
        {
            if (role == PaletteRole.Custom || palette == null)
                return role == PaletteRole.Custom ? custom : Color.white;

            switch (role)
            {
                case PaletteRole.Primary: return palette.primary;
                case PaletteRole.Secondary: return palette.secondary;
                case PaletteRole.Accent: return palette.accent;
                case PaletteRole.Background: return palette.background;
                case PaletteRole.TextPrimary: return palette.textPrimary;
                case PaletteRole.TextSecondary: return palette.textSecondary;
                default: return custom;
            }
        }
    }

    /// <summary>Shared color palette for a visual template. The single place a theme's identity lives.</summary>
    [Serializable]
    public class Palette
    {
        //[ColorPalette] 
        public Color primary = new Color(0.20f, 0.55f, 0.95f);
        //[ColorPalette] 
        public Color secondary = new Color(0.15f, 0.18f, 0.22f);
        //[ColorPalette] 
        public Color accent = new Color(0.95f, 0.75f, 0.20f);
        //[ColorPalette] 
        public Color background = new Color(0.08f, 0.09f, 0.11f);
        //[ColorPalette]
        public Color textPrimary = Color.white;
        //[ColorPalette] 
        public Color textSecondary = new Color(0.75f, 0.78f, 0.82f);

        public Color Resolve(PaletteRole role)
        {
            switch (role)
            {
                case PaletteRole.Primary: return primary;
                case PaletteRole.Secondary: return secondary;
                case PaletteRole.Accent: return accent;
                case PaletteRole.Background: return background;
                case PaletteRole.TextPrimary: return textPrimary;
                case PaletteRole.TextSecondary: return textSecondary;
                default: return primary;
            }
        }
    }

    /// <summary>Typography for a single <see cref="UITextRole"/>. Applied to TMP text at template-apply time.</summary>
    [Serializable]
    public class TextStyle
    {
        [Required] public TMP_FontAsset font;
        public float fontSize = 36f;
        public PaletteColor color = new PaletteColor(PaletteRole.TextPrimary);
        public FontStyles fontStyle = FontStyles.Normal;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        public float characterSpacing;

        [Tooltip("TMP text margins inside the text rect: Left, Top, Right, Bottom.")]
        public Vector4 textMargins = Vector4.zero;

        public void ApplyTo(TMP_Text text, Palette palette)
        {
            if (text == null) return;
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = color.Resolve(palette);
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.characterSpacing = characterSpacing;
            text.margin = textMargins;
        }
    }

    /// <summary>
    /// Skin for a single <see cref="UIElementType"/>: sprite + tint, plus optional interaction-state
    /// colors written into the element's <see cref="Selectable.colors"/> ColorBlock.
    /// </summary>
    [Serializable]
    public class ElementSkin
    {
        public Sprite backgroundSprite;
        public PaletteColor backgroundTint = new PaletteColor(Color.white);
        public PaletteColor foregroundTint = new PaletteColor(Color.white);

        //[ToggleGroup(nameof(useInteractionStates), "Interaction States (Selectable ColorBlock)")]
        public bool useInteractionStates;
        //[ToggleGroup(nameof(useInteractionStates))] 
        public PaletteColor highlighted = new PaletteColor(new Color(0.92f, 0.92f, 0.92f));
        //[ToggleGroup(nameof(useInteractionStates))] 
        public PaletteColor pressed = new PaletteColor(new Color(0.75f, 0.75f, 0.75f));
        //[ToggleGroup(nameof(useInteractionStates))] 
        public PaletteColor selected = new PaletteColor(new Color(0.92f, 0.92f, 0.92f));
       //[ToggleGroup(nameof(useInteractionStates))] 
        public PaletteColor disabled = new PaletteColor(new Color(0.6f, 0.6f, 0.6f, 0.5f));
        //[ToggleGroup(nameof(useInteractionStates)), MinValue(1f)] 
        public float colorMultiplier = 1f;
        //[ToggleGroup(nameof(useInteractionStates)), MinValue(0f)] 
        public float fadeDuration = 0.1f;

        public void ApplyTo(Image graphic, Palette palette)
        {
            if (graphic == null) return;
            if (backgroundSprite != null) graphic.sprite = backgroundSprite;
            // With interaction states the block carries the colors; the graphic stays white so the
            // Selectable's CrossFadeColor multiplies cleanly.
            graphic.color = useInteractionStates ? Color.white : backgroundTint.Resolve(palette);
        }

        public void ApplyTo(Selectable selectable, Palette palette)
        {
            if (selectable == null || !useInteractionStates) return;

            var block = new ColorBlock
            {
                normalColor = backgroundTint.Resolve(palette),
                highlightedColor = highlighted.Resolve(palette),
                pressedColor = pressed.Resolve(palette),
                selectedColor = selected.Resolve(palette),
                disabledColor = disabled.Resolve(palette),
                colorMultiplier = colorMultiplier,
                fadeDuration = fadeDuration
            };
            selectable.transition = Selectable.Transition.ColorTint;
            selectable.colors = block;
        }
    }

    /// <summary>Background configuration for a screen root. Mirrors the relevant Unity Image knobs,
    /// showing only the ones that apply to the chosen <see cref="Image.Type"/>.</summary>
    [Serializable]
    public class BackgroundSettings
    {
        //[EnumToggleButtons] 
        public BackgroundMode mode = BackgroundMode.SolidColor;

        //[HideIf(nameof(mode), BackgroundMode.None)]
        public PaletteColor color = new PaletteColor(PaletteRole.Background);

        [ShowIf(nameof(showsImage))]
        //[PreviewField(56)] 
        public Sprite image;

        //[ShowIf(nameof(showsImage)), EnumToggleButtons]
        public Image.Type imageType = Image.Type.Simple;

        // --- Simple / Filled ---
        [ShowIf(nameof(showsSimpleOrFilled))]
        [Tooltip("Keep the sprite's aspect ratio, letterboxing inside the rect.")]
        public bool preserveAspect;

        // --- Sliced / Tiled ---
        [ShowIf(nameof(showsSlicedOrTiled))]
        [Tooltip("Draw the center piece of the sprite (uncheck for a border-only frame).")]
        public bool fillCenter = true;

        [ShowIf(nameof(showsSlicedOrTiled)), MinValue(0.01f)]
        [Tooltip("Scales the sprite borders/tiling. 1 = the sprite's authored pixels-per-unit.")]
        public float pixelsPerUnitMultiplier = 1f;

        // --- Filled ---
        [ShowIf(nameof(showsFilled))]
        public Image.FillMethod fillMethod = Image.FillMethod.Radial360;

        //[ShowIf(nameof(showsFilled)), PropertyRange(0f, 1f)]
        public float fillAmount = 1f;

        [ShowIf(nameof(showsFilled))]
        [Tooltip("Radial fills only; ignored for horizontal/vertical.")]
        public bool fillClockwise = true;

        [ShowIf(nameof(showsFilled))]
        [Tooltip("Start edge/corner of the fill. Meaning depends on Fill Method (see Image docs).")]
        public int fillOrigin;

        private bool showsImage => mode == BackgroundMode.Image || mode == BackgroundMode.ColorAndImage;
        private bool showsSimpleOrFilled => showsImage && (imageType == Image.Type.Simple || imageType == Image.Type.Filled);
        private bool showsSlicedOrTiled => showsImage && (imageType == Image.Type.Sliced || imageType == Image.Type.Tiled);
        private bool showsFilled => showsImage && imageType == Image.Type.Filled;

        public void ApplyTo(Image target, Palette palette)
        {
            if (target == null) return;

            switch (mode)
            {
                case BackgroundMode.None:
                    target.enabled = false;
                    return;
                case BackgroundMode.SolidColor:
                    target.enabled = true;
                    target.sprite = null;
                    target.type = Image.Type.Simple;
                    target.color = color.Resolve(palette);
                    break;
                case BackgroundMode.Image:
                    target.enabled = true;
                    target.sprite = image;
                    target.color = Color.white;
                    ApplyImageType(target);
                    break;
                case BackgroundMode.ColorAndImage:
                    target.enabled = true;
                    target.sprite = image;
                    target.color = color.Resolve(palette);
                    ApplyImageType(target);
                    break;
            }
        }

        private void ApplyImageType(Image target)
        {
            target.type = imageType;
            switch (imageType)
            {
                case Image.Type.Simple:
                    target.preserveAspect = preserveAspect;
                    break;
                case Image.Type.Sliced:
                case Image.Type.Tiled:
                    target.fillCenter = fillCenter;
                    target.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                    break;
                case Image.Type.Filled:
                    target.preserveAspect = preserveAspect;
                    target.fillMethod = fillMethod;
                    target.fillOrigin = fillOrigin;
                    target.fillAmount = fillAmount;
                    target.fillClockwise = fillClockwise;
                    break;
            }
        }
    }

    /// <summary>
    /// Layout applied to a container's children at bake time: classic stack/grid groups or the UILS
    /// <see cref="FlexLayoutGroup"/>. Pure data - the baker turns it into components.
    /// </summary>
    [Serializable]
    public class LayoutGroupSettings
    {
        //[EnumToggleButtons] 
        public LayoutGroupType type = LayoutGroupType.Vertical;

        //[HideIf(nameof(type), LayoutGroupType.None)]
        public Margins padding = new Margins(16, 16, 16, 16);

        // --- Stack (Horizontal / Vertical) ---
        [ShowIf(nameof(isStack))] public float spacing = 8f;
        [ShowIf(nameof(isStackOrGrid))] public TextAnchor childAlignment = TextAnchor.UpperCenter;
        [ShowIf(nameof(isStack))] public bool controlChildWidth = true;
        [ShowIf(nameof(isStack))] public bool controlChildHeight = true;
        [ShowIf(nameof(isStack))] public bool childForceExpandWidth = true;
        [ShowIf(nameof(isStack))] public bool childForceExpandHeight;

        // --- Grid ---
        //[ShowIf(nameof(type), LayoutGroupType.Grid)] 
        public Vector2 cellSize = new Vector2(160f, 48f);
        //[ShowIf(nameof(type), LayoutGroupType.Grid)] 
        public Vector2 cellSpacing = new Vector2(8f, 8f);

        // --- Flex ---
        [ShowIf(nameof(isFlex))] public FlexDirection direction = FlexDirection.Row;
        [ShowIf(nameof(isFlex))] public bool wrap;
        [ShowIf(nameof(isFlex))] public FlexJustify justifyContent = FlexJustify.Start;
        [ShowIf(nameof(isFlex))] public FlexAlign alignItems = FlexAlign.Stretch;
        [ShowIf(nameof(isFlex)), Tooltip("x = gap along the main axis, y = gap between wrapped lines.")]
        public Vector2 gap = new Vector2(8f, 8f);

        private bool isStack => type == LayoutGroupType.Horizontal || type == LayoutGroupType.Vertical;
        private bool isStackOrGrid => isStack || type == LayoutGroupType.Grid;
        private bool isFlex => type == LayoutGroupType.Flex;
    }
}
