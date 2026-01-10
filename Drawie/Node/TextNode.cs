using Avalonia;
using Avalonia.Media;

namespace Drawie;

public class TextNode : Node
{
    private readonly DrawingGroup DrawingGroup = new();
    public IBrush Foreground = Brushes.Red;
    
    private string _content = "";

    public string Content
    {
        get => _content;
        set => SetField(ref _content, value);
    }

    public double FontSize = 12;
    public Typeface Typeface = Typeface.Default;
    public int MaxLineCount = 1;

    public TextNode()
    {
        Id = IdGenerator.GenerateRandomString(16, "TXT-");
        Border = new(0);
        Dirty = true;
    }

    public TextNode(string id)
        : base(id)
    {
        Border = new(0);
    }

    private void Draw(bool redraw = false)
    {
        if (!Dirty && !redraw)
        {
            return;
        }
        Dirty = false;

        using var ctx = DrawingGroup.Open();
        
        var info = new FormattedText(
            Content,
            new System.Globalization.CultureInfo(1),
            FlowDirection.LeftToRight,
            Typeface,
            FontSize,
            foreground: Foreground
        )
        {
            MaxTextWidth = MaxWidth - (Padding.Left + Padding.Right),
            MaxTextHeight = Math.Max(MaxHeight - (Padding.Top + Padding.Bottom), FontSize),
            MaxLineCount = MaxLineCount,
            Trimming = TextTrimming.WordEllipsis,
        };
        
        Size = new(info.Width, info.Height);

        ctx.DrawText(info, Origin + new Point(Padding.Left, Padding.Top));
    }

    public override void Render(DrawingContext ctx)
    {
        Draw();
        DrawingGroup.Draw(ctx);
    }

    public void SetDirty()
    {
        Dirty = true;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        switch (propertyName)
        {
            case nameof(Origin):
                Dirty = true;
                break;
            case nameof(Content):
                Dirty = true;
                break;
        }
    }
}