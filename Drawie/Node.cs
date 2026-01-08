using System.Text;
using Avalonia;
using Avalonia.Media;

namespace Drawie;

public interface IDrawable
{
    Point Origin { get; set; }
    Rect Bounds { get; }
    void Render(DrawingContext ctx);
}

public interface IDraggable
{
    Point DragOrigin { get; set; }
    event Action<Rect> OnPositionChanged;
    void Drag(Point position);
    void Move(Vector v);
}

public interface INode : IDrawable, IDraggable, IDisposable
{
    String Id { get; init; }
    bool Selected { get; set; }
    event Action<bool> OnSelectionChange;
    event Action<string, object[]> OnPropertyChanged;
    bool Contains(Point point);
    void InvalidateVisual();
    void InvalidatePosition(Point position);
}

internal static class IdGenerator
{
    private static string _alphaNum = "abcdefghijklmnopkrstuvwxyz0123456789";

    public static string GenerateRandomString(int len, string prefix = "", int? seed = null)
    {
        StringBuilder builder = new();
        Random r = new Random();

        if (seed is not null && seed.Value >= 0 && seed.Value <= int.MaxValue)
        {
            r = new(seed.Value);
        }
        else
        {
            r = Random.Shared;
        }

        int i = 0;
        for (; i < prefix.Length && i < len; i++)
        {
            builder.Append(prefix[i]);
        }

        for (; i < len; i++)
        {
            var pick = r.Next(_alphaNum.Length);
            builder.Append(_alphaNum[pick]);
        }

        return builder.ToString();
    }
}

public abstract class Node : INode
{
    protected static readonly IBrush DefaultBrush = Brushes.Black;
    protected static readonly IBrush DefaultSelectionBrush = Brushes.Chartreuse;
    protected static readonly IPen DefaultSelectedPen = new Pen(Brushes.Chartreuse, 2);

    public double MaxWidth { get; set; } = 0;
    public double MaxHeight { get; set; } = 0;

    protected bool Dirty = true;
    public IBrush Background { get; set; } = DefaultBrush;
    public Point _origin = new();
    public Point Origin
    {
        get => _origin;
        set
        {
            _origin = value;
            Bounds = new Rect(Origin, Size);
            OnPositionChanged?.Invoke(Bounds);

            OnPropertyChanged?.Invoke(nameof(Origin), [Origin]);
            PropertyChanged(nameof(Origin), [Origin]);
        }
    }
    public Point DragOrigin { get; set; }
    private bool _selected = false;
    public bool Selected
    {
        get => _selected;
        set
        {
            if (value == Selected)
            {
                return;
            }
            _selected = value;
            OnPropertyChanged?.Invoke(nameof(Selected), [Selected]);
            PropertyChanged(nameof(Selected), [Selected]);
        }
    }
    public double BoderRadius = 8;
    public Thickness Border = new(0);
    public Thickness Padding = new(0);
    private Rect _bounds = new();
    public Rect Bounds
    {
        get => _bounds;
        protected set
        {
            if (_bounds.Equals(value))
            {
                return;
            }
            _bounds = value;
            PropertyChanged(nameof(Bounds), [Bounds]);
        }
    }
    private Size _size = new(1, 1);

    public event Action<Rect>? OnPositionChanged;
    public event Action<bool>? OnSelectionChange;
    public event Action<string, Object[]>? OnPropertyChanged;

    public Size Size
    {
        get => _size;
        set
        {
            _size = value;
            Bounds = new Rect(Origin, Size);
        }
    }

    private Vector Offset { get; set; } = new(0, 0);
    public string Id { get; init; }

    public Node()
    {
        Id = IdGenerator.GenerateRandomString(16, "ND-");
    }

    public Node(string id)
    {
        Id = id;
    }

    public abstract void Render(DrawingContext ctx);

    public void Drag(Point position)
    {
        Offset += position;
        var pp = DragOrigin + Offset;
        var newOrigin = Canvas.GetOrigin(pp);
        Offset = new(0, 0);
        Origin = newOrigin;
    }

    public void Move(Vector vect)
    {
        if (vect.Equals(new(0, 0)))
        {
            return;
        }

        Origin = Origin + vect;
        InvalidatePosition(Origin);
    }

    public bool Contains(Point position)
    {
        var x1 = Origin.X;
        var y1 = Origin.Y;
        var x2 = (Origin.X + Size.Width);
        var y2 = (Origin.Y + Size.Height);
        return position.X >= x1 && position.X <= x2 && position.Y >= y1 && position.Y <= y2;
    }

    public virtual void Dispose() { }

    protected virtual void PropertyChanged(string propertyName, object[] args) { }

    public virtual void InvalidateVisual() { }

    public virtual void InvalidatePosition(Point position) { }
}

public class Pannel : Node
{
    public INode[] Children { get; init; } = [];

    public Pannel()
    {
        Id = IdGenerator.GenerateRandomString(16, "PAN-");
        Border = new(0);
        Dirty = true;
    }

    public Pannel(string id)
        : base(id)
    {
        Border = new(0);
    }

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var p1 = Origin;
        var p2 = new Point(p1.X + w, p1.Y);
        var p3 = new Point(p1.X + w, p1.Y + h);
        var p4 = new Point(p1.X, p1.Y + h);

        // Border
        ctx.FillRectangle(
            Selected ? DefaultSelectionBrush : Brushes.Gray,
            new(p1.X, p1.Y, w, h),
            cornerRadius: (float)BoderRadius
        );

        var pb1 = p1 + new Point(Border.Left, Border.Top);
        var pb3 = p3 + new Point(-Border.Right, -Border.Bottom);

        // Body
        ctx.FillRectangle(Background, new(pb1, pb3), cornerRadius: (float)BoderRadius);

        //Render Children
        for (int i = 0; i < Children.Length; i++)
        {
            Children[i].Render(ctx);
        }
    }

    protected override void PropertyChanged(string propertyName, object[] args)
    {
        switch (propertyName)
        {
            case nameof(Origin):

                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].Origin = Origin;
                }
                break;
        }
    }
}

public class TextNode : Node
{
    private DrawingGroup DrawingGroup = new();
    public IBrush Foreground = Brushes.Red;
    public string Content { get; set; } = "";
    public double FontSize = 12;
    public Typeface Typeface;
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

    public void Draw(bool redraw = false)
    {
        if (!Dirty && !redraw)
        {
            return;
        }

        Dirty = false;

        using (var ctx = DrawingGroup.Open())
        {
            double availableSpaceX = Size.Width - (Padding.Left + Padding.Right);
            double availableSpaceY = Size.Height - (Padding.Top + Padding.Bottom);

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
    }

    protected override void PropertyChanged(string propertyName, object[] args)
    {
        switch (propertyName)
        {
            case nameof(Origin):
                //Dirty = true;
                break;
        }
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
}

public enum LayoutType
{
    Flex,
}

public enum LayoutDirection
{
    Vertical,
    Horizontal,
}

public class LayoutNode : Node
{
    public LayoutType Type;
    public LayoutDirection Direction;

    public double Gap = 8;
    public INode[] Children { get; init; } = [];

    public override void Render(DrawingContext ctx)
    {
        switch (Type)
        {
            case LayoutType.Flex:
                HadleFlex(ctx);
                break;
        }
    }

    private void HadleFlex(DrawingContext ctx)
    {
        switch (Direction)
        {
            case LayoutDirection.Horizontal:
                break;
            case LayoutDirection.Vertical:
                double offset = 0;
                for (int i = 0; i < Children.Length; i++)
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

public class TaskNode : Node
{
    public string Title = "Task title very long title that shoul overflow";
    public string Desctiption =
        "Task description with multi line and a very long first line this is the second line, simple filler text simple filler text simple filler text simple filler text simple filler text simple filler text simple filler text simple filler text";
    public IBrush Foreground { get; set; } = DefaultBrush;

    private Pannel Body;
    private DrawingGroup DrawingGroup = new();
    private double Width = 224;
    private double Height = 128;

    private TextNode TitleNode = new();
    private TextNode DescriptionNode = new();
    private LayoutNode Layout = new();

    public TaskNode(Point origin, string? id = null)
    {
        if (id is not null)
        {
            Id = id;
        }

        Origin = origin;
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
            Typeface = new(
                FontFamily.Default,
                FontStyle.Normal,
                FontWeight.Normal,
                FontStretch.Normal
            ),
        };

        Layout = new LayoutNode()
        {
            Origin = Origin,
            Gap = 8,
            Children = [TitleNode, DescriptionNode],
        };

        Body = new Pannel()
        {
            Origin = origin,
            Size = new(Width, Height),
            Background = Brushes.Black,
            Border = new(2),
            Padding = new(8),
            Children = [Layout],
        };
        Init();
    }

    void Init()
    {
        OnPropertyChanged += HandlePropertyChange;
    }

    private void HandlePropertyChange(string propertyName, object[] args)
    {
        switch (propertyName)
        {
            case nameof(Selected):
                Body.Selected = Selected;
                break;

            case nameof(Origin):
                Body.Origin = Origin;
                break;
        }
    }

    public override void Dispose()
    {
        OnPropertyChanged += HandlePropertyChange;
    }

    private void HandlePositionChange(Rect rect)
    {
        Body.Origin = new(rect.X, rect.Y);
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
}

public abstract class VisiblePath : INode
{
    private bool _selected = false;
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnSelectionChange?.Invoke(_selected);
        }
    }
    public Point Origin { get; set; }

    public Rect Bounds { get; protected set; }
    protected bool Dirty = true;

    public Point DragOrigin { get; set; }
    public string Id { get; init; }

    public VisiblePath()
    {
        Id = IdGenerator.GenerateRandomString(18, "PT-");
    }

    public VisiblePath(string path)
    {
        Id = path;
    }

    public event Action<Rect>? OnPositionChanged;
    public event Action<bool>? OnSelectionChange;
    public event Action<string, object[]>? OnPropertyChanged;

    public bool Contains(Point point)
    {
        return false;
    }

    public bool ContainsWorld(Point point)
    {
        return false;
    }

    public void Drag(Point position)
    {
        return;
    }

    public void Move(Vector v)
    {
        return;
    }

    public abstract void Render(DrawingContext ctx);

    public virtual void Dispose() { }

    public void PropertyChanged(string propertyName, object[] args) { }

    public void InvalidateVisual()
    {
        Dirty = true;
    }

    public void InvalidatePosition(Point position) { }
}

public class NodeLink : VisiblePath
{
    private List<Point> _points = [];
    public INode? Source { get; init; }
    public INode? Destination { get; init; }
    private Canvas _canvas { get; init; }
    private DrawingGroup VertexGroup = new();
    private IPen Pen = new Pen(Brushes.Red, 2, lineCap: PenLineCap.Round);

    public NodeLink(Canvas canvas)
        : base()
    {
        _canvas = canvas;
    }

    private void HandleOffsetChange(Vector vector)
    {
        Dirty = true;
        UpdatePath();
    }

    private void HandlePositionChange(Rect rect)
    {
        Dirty = true;
        UpdatePath();
    }

    public NodeLink(Canvas canvas, string id, string? src = null, string? des = null)
        : base(id)
    {
        _canvas = canvas;
        Source = _canvas.GetNode(src);
        Destination = _canvas.GetNode(des);

        _canvas.OnOffsetChanged += HandleOffsetChange;

        if (Source is not null && Destination is not null)
        {
            Source.OnPositionChanged += HandlePositionChange;
            Destination.OnPositionChanged += HandlePositionChange;
        }
    }

    private void UpdatePath()
    {
        if (!Dirty)
        {
            return;
        }

        Dirty = false;

        _points.Clear();

        FindPath();

        UpdateBounds();

        using var ctx = VertexGroup.Open();
        if (_points.Count == 0)
        {
            return;
        }

        Point start = _points[0] * Canvas.GridSize;

        for (int i = 1; i < _points.Count; i++)
        {
            Point end = _points[i] * Canvas.GridSize;

            ctx.DrawLine(Pen, start, end);

            start = end;
        }
    }

    private void FindPath()
    {
        _points.Clear();
        if (Source is null || Destination is null)
        {
            return;
        }

        Point sourceLeft = new(
            Source.Bounds.TopLeft.X - 1,
            Source.Bounds.TopLeft.Y + Source.Bounds.Height / 2
        );
        Point sourceRight = new(
            Source.Bounds.TopRight.X + 1,
            Source.Bounds.TopRight.Y + Source.Bounds.Height / 2
        );

        Point destLeft = new(
            Destination.Bounds.TopLeft.X - 1,
            Destination.Bounds.TopLeft.Y + Source.Bounds.Height / 2
        );
        Point destRight = new(
            Destination.Bounds.TopRight.X + 1,
            Destination.Bounds.TopRight.Y + Source.Bounds.Height / 2
        );

        if (
            (sourceLeft.X >= destLeft.X && sourceLeft.X <= destRight.X)
            || (destLeft.X >= sourceLeft.X && destLeft.X <= sourceRight.X)
        )
        {
            _points.Clear();

            int maxX = (int)Math.Max(sourceRight.X, destRight.X);
            int maxY = (int)Math.Max(sourceRight.Y, destRight.Y);
            int minY = (int)Math.Min(sourceRight.Y, destRight.Y);

            _points.Add(sourceRight + new Point(-1, 0));
            _points.Add(sourceRight);

            if (sourceRight.Y > destRight.Y)
            {
                _points.Add(new(maxX, maxY));
                _points.Add(new(maxX, minY));
            }
            else
            {
                _points.Add(new(maxX, minY));
                _points.Add(new(maxX, maxY));
            }

            _points.Add(destRight);

            _points.Add(destRight + new Point(-1, 0));

            return;
        }

        List<(double, Point, Point, Vector, Vector)> dd =
        [
            (Point.Distance(sourceLeft, destLeft), sourceLeft, destLeft, new(1, 0), new(1, 0)),
            (Point.Distance(sourceLeft, destRight), sourceLeft, destRight, new(1, 0), new(-1, 0)),
            (Point.Distance(sourceRight, destLeft), sourceRight, destLeft, new(-1, 0), new(1, 0)),
            (
                Point.Distance(sourceRight, destRight),
                sourceRight,
                destRight,
                new(-1, 0),
                new(-1, 0)
            ),
        ];

        dd.Sort(
            (a, b) =>
            {
                if (b.Item1 > a.Item1)
                {
                    return -1;
                }
                if (b.Item1 < a.Item1)
                {
                    return 1;
                }
                return 0;
            }
        );

        int i = 0;
        for (; i < dd.Count; i++)
        {
            var d = dd[i];
            if (d.Item1 > 1)
            {
                break;
            }
        }

        if (i == dd.Count)
        {
            return;
        }

        var start = dd[i].Item2;
        var end = dd[i].Item3;

        _points.Add(start + dd[i].Item4);

        var path = ComputePath(start, end);

        foreach (var item in path)
        {
            _points.Add(item);
        }

        _points.Add(end + dd[i].Item5);
    }

    private List<Point> ComputePath(Point start, Point end)
    {
        int minX = (int)Math.Min(start.X, end.X);
        int maxX = (int)Math.Max(start.X, end.X);

        int minY = (int)Math.Min(start.Y, end.Y);
        int maxY = (int)Math.Max(start.Y, end.Y);

        int midX = (int)((minX + maxX) / 2);

        Point mid1 = new(midX, minY);
        Point mid2 = new(midX, maxY);

        List<Point> res = [start];

        if (start.Y > end.Y)
        {
            res.Add(mid2);
            res.Add(mid1);
        }
        else
        {
            res.Add(mid1);
            res.Add(mid2);
        }

        res.Add(end);

        return res;
    }

    private void UpdateBounds()
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;

        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Point p in _points)
        {
            minX = Math.Min(minX, (int)p.X);
            minY = Math.Min(minY, (int)p.Y);
            maxX = Math.Max(maxX, (int)p.X);
            maxY = Math.Max(maxY, (int)p.Y);
        }

        Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public override void Render(DrawingContext ctx)
    {
        UpdatePath();
        VertexGroup.Draw(ctx);
    }

    public override void Dispose()
    {
        _canvas.OnOffsetChanged -= HandleOffsetChange;
        if (Source is not null && Destination is not null)
        {
            Source.OnPositionChanged -= HandlePositionChange;
            Destination.OnPositionChanged -= HandlePositionChange;
        }
    }
}

public class DoubleDescendingComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        // For max-heap (descending priority): higher values have higher priority
        // return y.CompareTo(x);

        // For min-heap (ascending priority - default): lower values have higher priority
        return x.CompareTo(y);
    }
}
