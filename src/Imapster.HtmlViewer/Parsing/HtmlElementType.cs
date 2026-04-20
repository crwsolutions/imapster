namespace Imapster.HtmlViewer.Parsing;

/// <summary>
/// Represents the type of an HTML element in the parsed document.
/// </summary>
public enum HtmlElementType
{
    /// <summary>
    /// Unknown or unrecognized element type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Text node containing plain text content.
    /// </summary>
    Text,

    /// <summary>
    /// Paragraph element.
    /// </summary>
    Paragraph,

    /// <summary>
    /// Heading element (h1-h6).
    /// </summary>
    Heading,

    /// <summary>
    /// Unordered list element.
    /// </summary>
    UnorderedList,

    /// <summary>
    /// Ordered list element.
    /// </summary>
    OrderedList,

    /// <summary>
    /// List item element.
    /// </summary>
    ListItem,

    /// <summary>
    /// Span element (inline container).
    /// </summary>
    Span,

    /// <summary>
    /// Anchor/link element.
    /// </summary>
    Link,

    /// <summary>
    /// Image element.
    /// </summary>
    Image,

    /// <summary>
    /// Bold/strong element.
    /// </summary>
    Bold,

    /// <summary>
    /// Italic/emphasis element.
    /// </summary>
    Italic,

    /// <summary>
    /// Underline element.
    /// </summary>
    Underline,

    /// <summary>
    /// Div element (block container).
    /// </summary>
    Div,

    /// <summary>
    /// Preformatted text element.
    /// </summary>
    Pre,

    /// <summary>
    /// Blockquote element.
    /// </summary>
    Blockquote,

    /// <summary>
    /// Line break element.
    /// </summary>
    LineBreak,

    /// <summary>
    /// Horizontal rule element.
    /// </summary>
    HorizontalRule,

    /// <summary>
    /// Button element.
    /// </summary>
    Button,

    /// <summary>
    /// Input element.
    /// </summary>
    Input,

    /// <summary>
    /// Table element.
    /// </summary>
    Table,

    /// <summary>
    /// Table header section element.
    /// </summary>
    TableHeader,

    /// <summary>
    /// Table body section element.
    /// </summary>
    TableBody,

    /// <summary>
    /// Table footer section element.
    /// </summary>
    TableFooter,

    /// <summary>
    /// Table row element.
    /// </summary>
    TableRow,

    /// <summary>
    /// Table cell element.
    /// </summary>
    TableCell,

    /// <summary>
    /// Table header cell element.
    /// </summary>
    TableHeaderCell,

    /// <summary>
    /// Section element.
    /// </summary>
    Section,

    /// <summary>
    /// Article element.
    /// </summary>
    Article,

    /// <summary>
    /// Header element.
    /// </summary>
    Header,

    /// <summary>
    /// Footer element.
    /// </summary>
    Footer,

    /// <summary>
    /// Nav element.
    /// </summary>
    Nav,

    /// <summary>
    /// Main element.
    /// </summary>
    Main,

    /// <summary>
    /// Center element (deprecated but still used in legacy HTML).
    /// </summary>
    Center,

    /// <summary>
    /// Skip element.
    /// </summary>
    Skip,

    /// <summary>
    /// Time element.
    /// </summary>
    Time,

    /// <summary>
    /// Data element.
    /// </summary>
    Data,

    /// <summary>
    /// Mark element.
    /// </summary>
    Mark,

    /// <summary>
    /// Code element.
    /// </summary>
    Code,

    /// <summary>
    /// Quote element.
    /// </summary>
    Quote,

    /// <summary>
    /// Cite element.
    /// </summary>
    Cite,

    /// <summary>
    /// Kbd element.
    /// </summary>
    Kbd,

    /// <summary>
    /// Samp element.
    /// </summary>
    Samp,

    /// <summary>
    /// Var element.
    /// </summary>
    Var,

    /// <summary>
    /// Subscript element.
    /// </summary>
    Subscript,

    /// <summary>
    /// Superscript element.
    /// </summary>
    Superscript,

    /// <summary>
    /// Small element.
    /// </summary>
    Small,

    /// <summary>
    /// Del element.
    /// </summary>
    Del,

    /// <summary>
    /// Ins element.
    /// </summary>
    Ins,

    /// <summary>
    /// Abbr element.
    /// </summary>
    Abbrev,

    /// <summary>
    /// Acronym element.
    /// </summary>
    Acronym,

    /// <summary>
    /// Address element.
    /// </summary>
    Address,

    /// <summary>
    /// Details element.
    /// </summary>
    Details,

    /// <summary>
    /// Summary element.
    /// </summary>
    Summary,

    /// <summary>
    /// Dialog element.
    /// </summary>
    Dialog,

    /// <summary>
    /// Figure element.
    /// </summary>
    Figure,

    /// <summary>
    /// Figcaption element.
    /// </summary>
    Figcaption,

    /// <summary>
    /// Picture element.
    /// </summary>
    Picture,

    /// <summary>
    /// Source element.
    /// </summary>
    Source,

    /// <summary>
    /// Video element.
    /// </summary>
    Video,

    /// <summary>
    /// Audio element.
    /// </summary>
    Audio,

    /// <summary>
    /// Track element.
    /// </summary>
    Track,

    /// <summary>
    /// Map element.
    /// </summary>
    Map,

    /// <summary>
    /// Area element.
    /// </summary>
    Area,

    /// <summary>
    /// Canvas element.
    /// </summary>
    Canvas,

    /// <summary>
    /// Script element.
    /// </summary>
    Script,

    /// <summary>
    /// Style element.
    /// </summary>
    Style,

    /// <summary>
    /// Meta element.
    /// </summary>
    Meta,

    /// <summary>
    /// Link element.
    /// </summary>
    LinkElement,

    /// <summary>
    /// Title element.
    /// </summary>
    Title,

    /// <summary>
    /// Head element.
    /// </summary>
    Head,

    /// <summary>
    /// Body element.
    /// </summary>
    Body,

    /// <summary>
    /// Html element.
    /// </summary>
    Html,

    /// <summary>
    /// Comment node.
    /// </summary>
    Comment,

    /// <summary>
    /// Document fragment.
    /// </summary>
    DocumentFragment
}
