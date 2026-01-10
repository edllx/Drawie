using Avalonia;
using Avalonia.Media;

namespace Drawie;

public class TaskNode : Node
{
    private string _title =  "";
    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }
    
    private string _description = "";

    public string Desctiption
    {
        get => _description;
        set => SetField(ref _description, value);
    }
        
    public IBrush Foreground { get; set; } = DefaultBrush;

    private Container Body;
    private DrawingGroup DrawingGroup = new();
    private double Width = 224;
    private double Height = 128;
    
    private TextNode TitleNode = new();
    private TextNode DescriptionNode = new();
    private LayoutNode Layout;

    public TaskNode(Point origin, string? id = null)
    {
        if (id is not null)
        {
            Id = id;
        }
        
        Size = new(Width, Height);
        
        TitleNode = new TextNode()
        {
            Content = Title,
            Foreground = Brushes.White,
            MaxWidth = Size.Width,
            MaxHeight = Size.Height,
            Padding = new(8),
            FontSize = 12,
            Typeface = new(
                FontFamily.Default,
                FontStyle.Normal,
                FontWeight.Bold,
                FontStretch.Normal
            ),
        };
        
        DescriptionNode = new TextNode()
        {
            Content = Desctiption,
            Foreground = Brushes.White,
            MaxWidth = Size.Width,
            MaxHeight = Size.Height,
            MaxLineCount = 4,
            Padding = new(8),
            FontSize = 8,
            Typeface = Typeface.Default, 
        };
       
        Layout = new LayoutNode()
        {
            Origin = origin,
            Gap = 8,
            Children = [TitleNode, DescriptionNode],
        };

        Body = new Container()
        {
            Origin = origin,
            Size = new(Width, Height),
            Background = Brushes.Black,
            Border = new(2),
            Padding = new(8),
            Children = [Layout],
        };
        
        Origin = origin;
    }

    public override void Render(DrawingContext ctx)
    {
        Body.Render(ctx);
    }

    public override void InvalidatePosition(Point position)
    {
        Layout.Origin = position;
        TitleNode.SetDirty();
        DescriptionNode.SetDirty();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        switch (propertyName)
        {
            case nameof(Title):
                TitleNode.Content = Title;
                Canvas?.Refrech();
                break;
            case nameof(Desctiption):
                DescriptionNode.Content = Desctiption;
                Canvas?.Refrech();
                break;
            case nameof(Selected):
                Body.Selected = Selected;
                break;
            case  nameof(Origin):
                Body.Origin = Origin;
                break;
        }
        base.OnPropertyChanged(propertyName);
    }
}