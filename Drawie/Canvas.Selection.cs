using Avalonia;
using Avalonia.Media;

namespace Drawie;

public partial class Canvas
{
    private static readonly IPen SelectionPen = new Pen(Brushes.Crimson, 1);
    internal Selection Selection { get; }

    internal void ClearSelection()
    {
        foreach (var nd in _nodes)
        {
            nd.Value.Selected = false;
        }

        Selection.Nodes.Clear();
        Selection.TopLeft = Canvas.GetOrigin(ScreenToWorld(LastMousePressedPosition));
        Selection.BotRight = Selection.TopLeft;

        Selection.NotityBoundChanged();
    }

    private void DrawSelection(DrawingContext ctx)
    {
        if (Selection.TopLeft.Equals(Selection.BotRight))
        {
            return;
        }

        var (tl, tr, br, bl) = Selection.Bound();

        ctx.DrawLine(SelectionPen, tl, tr);
        ctx.DrawLine(SelectionPen, tl, bl);
        ctx.DrawLine(SelectionPen, br, tr);
        ctx.DrawLine(SelectionPen, br, bl);
    }
}

internal class Selection()
{
    public event Action<Point, Point>? OnBoundChange;
    public Point TopLeft { get; set; }
    private (Point, Point) _cache = (new(), new());
    public Point BotRight { get; set; }
    public bool Dragging { get; set; }
    public Point DragOrigin { get; set; }
    public Point ClickOffset { get; set; }
    private Vector BaseOffset { get; set; } = new(0, 0);
    private Vector Offset { get; set; } = new(0, 0);

    public readonly List<INode> Nodes = [];

    public (Point, Point, Point, Point) Bound()
    {
        Point tr = new(BotRight.X, TopLeft.Y);
        Point bl = new(TopLeft.X, BotRight.Y);
        return (TopLeft, tr, BotRight, bl);
    }

    public void NotityBoundChanged()
    {
        var bound = (TopLeft, BotRight);
        if (_cache.Equals(bound))
        {
            return;
        }
        _cache = bound;
        OnBoundChange?.Invoke(TopLeft, BotRight);
    }

    public bool Contains(Point position)
    {
        var x1 = Math.Min(TopLeft.X, BotRight.X);
        var x2 = Math.Max(TopLeft.X, BotRight.X);
        var y1 = Math.Min(TopLeft.Y, BotRight.Y);
        var y2 = Math.Max(TopLeft.Y, BotRight.Y);
        return position.X >= x1 && position.X <= x2 && position.Y >= y1 && position.Y <= y2;
    }

    private void SetOrigin(Point origin)
    {
        var delta = BotRight - TopLeft;
        TopLeft = origin;
        BotRight = origin + delta;
    }

    public void AddNode(INode? node, bool adjustBounds = true)
    {
        if (node == null)
        {
            return;
        }

        node.Selected = true;
        if (Nodes.Contains(node))
        {
            return;
        }

        Nodes.Add(node);
        if (!adjustBounds)
        {
            return;
        }
    }

    public void UpdateBounds()
    {
        TopLeft = new Point(int.MaxValue, int.MaxValue);
        BotRight = new Point(int.MinValue, int.MinValue);
        foreach (var node in Nodes)
        {
            TopLeft = new(
                Math.Min(TopLeft.X, node.Bounds.TopLeft.X),
                Math.Min(TopLeft.Y, node.Bounds.TopLeft.Y)
            );
            
            BotRight = new(
                Math.Max(BotRight.X, node.Bounds.BottomRight.X),
                Math.Max(BotRight.Y, node.Bounds.BottomRight.Y)
            );
        }
        NotityBoundChanged();
    }

    public void RemoveNode(string id)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == id);
        if (node is null){return;}
        Nodes.Remove(node);
    }

    public void Drag(Point position)
    {
        Offset += position;
        var pp = DragOrigin + Offset + ClickOffset;
        var newOrigin = Canvas.GetOrigin(pp) - Canvas.GetOrigin(ClickOffset);
        Offset = BaseOffset;

        foreach (Node node in Nodes)
        {
            node.Move(newOrigin - TopLeft);
        }

        SetOrigin(newOrigin);
        NotityBoundChanged();
    }
}