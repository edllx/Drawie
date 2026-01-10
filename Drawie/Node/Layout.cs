using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;

namespace Drawie;


public enum LayoutType
{
    Flex,
}

public enum LayoutDirection
{
    Vertical,
    Horizontal,
}


public class LayoutNode : Container
{
    public LayoutType Type = LayoutType.Flex;
    public LayoutDirection Direction = LayoutDirection.Vertical;
    public double Gap = 8;

    public LayoutNode() : base()
    {
        
    }

    public override void Render(DrawingContext ctx)
    {
        switch (Type)
        {
            case LayoutType.Flex:
                HandleFlex(ctx);
                break;
        }
    }

    private void HandleFlex(DrawingContext ctx)
    {
        
        double offset = 0;
        switch (Direction)
        {
            case LayoutDirection.Horizontal:
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].Origin = Origin + new Point(0, offset);
                    Children[i].Render(ctx);
                    offset += Children[i].Bounds.Height;
                    offset += Gap;
                }
                break;
            case LayoutDirection.Vertical:
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].Origin = Origin + new Point(0, offset);
                    Children[i].Render(ctx);
                    offset += Children[i].Bounds.Height;
                    offset += Gap;
                }
                break;
        }
    }
}