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
    bool Draggable { get; set; }
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
    public bool Draggable { get; set; }
    
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

    public virtual void Drag(Point position)
    {
        Offset += position;
        var pp = DragOrigin + Offset;
        var newOrigin = Canvas.GetOrigin(pp);
        Offset = new(0, 0);
        Origin = newOrigin;
    }

    public virtual void Move(Vector vect)
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

