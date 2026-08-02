namespace MyToolz.UI.Layout
{
    /// <summary>Structural region inside a screen skeleton.</summary>
    public enum UIRegion
    {
        Header,
        Body,
        Footer
    }

    /// <summary>Semantic role of a text element, used for centralized typography styling.</summary>
    public enum UITextRole
    {
        Title,
        Subtitle,
        Body,
        Caption,
        Button
    }

    /// <summary>Kind of templated UI element that can be placed into a region.</summary>
    public enum UIElementType
    {
        None,
        Label,
        Button,
        Slider,
        Dropdown,
        Toggle,
        InputField,
        Image,
        ScrollView
    }

    /// <summary>Layout applied to a container's children.</summary>
    public enum LayoutGroupType
    {
        None,
        Horizontal,
        Vertical,
        Grid,
        Flex
    }

    /// <summary>How a screen background is rendered.</summary>
    public enum BackgroundMode
    {
        None,
        SolidColor,
        Image,
        ColorAndImage
    }

    /// <summary>
    /// A color slot in the active template's <see cref="Palette"/>. Styles reference a role instead of
    /// a raw color, so re-tinting a whole theme is a six-color edit. Custom opts out of the palette.
    /// </summary>
    public enum PaletteRole
    {
        Custom,
        Primary,
        Secondary,
        Accent,
        Background,
        TextPrimary,
        TextSecondary
    }

    /// <summary>Main axis of a flex container.</summary>
    public enum FlexDirection
    {
        Row,
        Column
    }

    /// <summary>Distribution of children along a flex container's main axis.</summary>
    public enum FlexJustify
    {
        Start,
        Center,
        End,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly
    }

    /// <summary>Alignment of children along a flex container's cross axis.</summary>
    public enum FlexAlign
    {
        Start,
        Center,
        End,
        Stretch
    }
}
