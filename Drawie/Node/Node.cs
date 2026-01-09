using System.ComponentModel;
using System.Runtime.CompilerServices;
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

public interface INode : IDrawable, IDraggable, IDisposable, INotifyPropertyChanged
{
    String Id { get; init; }
    bool Selected { get; set; }
    bool Contains(Point point);
    void InvalidateVisual();
    void InvalidatePosition(Point position);
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

    #region Properties
    private Point _origin = new();
    public Point Origin
    {
        get => _origin;
        set
        {
            if (SetField(ref _origin, value))
            {
                Bounds = new Rect(Origin, Size);
            }
        }
    }
    
    private Size _size = new(1, 1);
    public Size Size
    {
        get => _size;
        set
        {
            if (SetField(ref _size, value))
            {
                Bounds = new Rect(Origin, Size);
            }
        }
    }
    
    private bool _selected = false;
    public bool Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    } 
    
    private Rect _bounds = new();
    public Rect Bounds
    {
        get => _bounds;
        protected set => SetField(ref _bounds, value);
    }

    #endregion
    
    public Point DragOrigin { get; set; }
    public double BoderRadius = 8;
    public Thickness Border = new(0);
    public Thickness Padding = new(0);
    public event Action<Rect>? OnPositionChanged;
    public event Action<bool>? OnSelectionChange;

    private Vector Offset { get; set; } = new(0, 0);
    public Canvas? Canvas { get; set; }
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

    public virtual void InvalidateVisual() { }

    public virtual void InvalidatePosition(Point position) { }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
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
    //public event Action<string, object[]>? OnPropertyChanged;

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

    //public void PropertyChanged(string propertyName, object[] args) { }

    public void InvalidateVisual()
    {
        Dirty = true;
    }

    public void InvalidatePosition(Point position) { }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
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