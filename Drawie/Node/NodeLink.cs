using System.ComponentModel;
using Avalonia;
using Avalonia.Media;

namespace Drawie;


public class NodeLink : Node
{
    private List<Point> _points = [];
    public INode? Source { get; init; }
    public INode? Destination { get; init; }
    private DrawingGroup VertexGroup ;
    //private IPen Pen = new Pen(Brushes.Red, 2, lineCap: PenLineCap.Round);

    public NodeLink()
        : base()
    {
        VertexGroup = new();
        Id = IdGenerator.GenerateRandomString(18, "PT-");
    }

    private void HandleOffsetChange(Vector vector)
    {
        Dirty = true;
        Draw();
    }

    public NodeLink(Canvas canvas, string? id =null , string? src = null, string? des = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            Id = IdGenerator.GenerateRandomString(18, "PT-");
        }
        
        VertexGroup = new();
        
        Canvas = canvas;
        Source = Canvas.GetNode(src);
        Destination = Canvas.GetNode(des);
        

        Canvas.OnOffsetChanged += HandleOffsetChange;

        if (Source is not null && Destination is not null)
        {
            Source.PropertyChanged += HandleNodeChanged;
            Destination.PropertyChanged += HandleNodeChanged;
        }
    }

    private void HandleNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
           case  nameof(INode.Bounds):
               Dirty = true;
               break;
        }
    }

    private void Draw(bool redraw = false)
    {
        if (!Dirty && !redraw)
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

        Point start = _points[0] ;

        var pen = new Pen(Brushes.Red, 2, lineCap: PenLineCap.Round);
        for (int i = 1; i < _points.Count; i++)
        {
            Point end = _points[i] ;
            ctx.DrawLine(pen, start, end);
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
        Draw();
        VertexGroup.Draw(ctx);
    }

    public override void Dispose()
    {
        if (Canvas is not null)
        {
            Canvas.OnOffsetChanged -= HandleOffsetChange;
        }
        
        if (Source is not null && Destination is not null)
        {
            Source.PropertyChanged -= HandleNodeChanged;
            Destination.PropertyChanged -= HandleNodeChanged;
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        switch (propertyName)
        {
            case nameof(Source):
                break;
            
            case nameof(Destination):
                break;
            
            case nameof(Bounds):
                break;
        }
        base.OnPropertyChanged(propertyName);
    }

    public override void Drag(Point position)
    {
        return;
    }

    public override void Move(Vector v)
    {
        return;
    }
}