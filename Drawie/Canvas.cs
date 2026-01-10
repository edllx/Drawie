using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Drawie;

internal interface ICanvas
{
    INode? GetNode(string id);
    INode? GetHittingNode(Point p);

    event Action<double> OnZoomChanged;
    event Action<Vector> OnOffsetChanged;
    
    void AddNode(INode node, bool replace =false);
    void AddNode(IEnumerable<INode> nodes,bool replace =false,bool clear=false);
    void RemoveNode(string id);  
    void RemoveNode(Predicate<INode> predicate);
    
    void AddNodeLink(string fromId, string toId,string? id =null);
    void RemoveNodeLink(string id);
    void RemoveNodeLink(Predicate<NodeLink> predicate);
    
}

public partial class Canvas : Control, ICanvas
{
    private static readonly double MaxZoom = 3;
    private static readonly double MinZoom = 0.25;
    private CanvasPointerEventHandler PointerHandler;

    public event Action<double>? OnZoomChanged;
    public event Action<Vector>? OnOffsetChanged;

    

    private Stopwatch _renderStopwatch = new Stopwatch();
    private Stopwatch _cacheStopwatch = new Stopwatch();
    private long _lastRenderTimeMs = 0;
    private long _lastCacheTimeMs = 0;
    private int _cacheHits = 0;
    private int _cacheMisses = 0;
    private bool _useCache = true;
    
    public static readonly StyledProperty<bool> ShowStatProperty = AvaloniaProperty.Register<Canvas, bool>(
        nameof(ShowStat));
    
    public static readonly StyledProperty<GridType> GridTypeProperty = AvaloniaProperty.Register<Canvas, GridType>(
        nameof(GridType),defaultValue:GridType.None);


    private ObservableCollection<int> col = [];
    

    public bool ShowStat
    {
        get => GetValue(ShowStatProperty);
        set => SetValue(ShowStatProperty, value);
    }

    public GridType GridType
    {
        get => GetValue(GridTypeProperty);
        set => SetValue(GridTypeProperty, value);
    }

    public double Zoom { get; private set; } = 1;
    internal Vector PanOffset { get; private set; } = new Vector(0, 0); 
    internal Point LastMousePressedPosition { get; private set; }

    //private readonly List<INode> _nodes = [];
    //private readonly List<NodeLink> _nodeLinks = [];


    public Canvas()
    {
        PointerHandler = new(this);
        Selection = new Selection();
        Selection.OnBoundChange += HandleSelectionBoundChange;
    }

    private void HandleSelectionBoundChange(Point tl, Point br)
    {
        Refrech();
    }

    internal bool SetPanOffset(Vector offset)
    {
        PanOffset = offset;

        OnOffsetChanged?.Invoke(PanOffset);
        return true;
    }

    internal bool SetLastPressed(Point p)
    {
        LastMousePressedPosition = p;
        return true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        PointerHandler
            .HandleShiftLeftPressed(e)
            .Action(() =>
            {
                LastMousePressedPosition = e.GetPosition(this);
            })
            .HandleLeftPressed(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var old = Zoom;
        var prev = ScreenToWorld(e.GetPosition(this));
        if (e.Delta.Y > 0)
        {
            Zoom += 0.25;
        }
        else if (e.Delta.Y < 0)
        {
            Zoom -= 0.25;
        }

        Zoom = Math.Clamp(Zoom, MinZoom, MaxZoom);

        if (Math.Abs(Zoom - old) < 0.01)
        {
            return;
        }

        var current = ScreenToWorld(e.GetPosition(this));
        PanOffset += (current - prev);
        UpdateGrid();
        Refrech();
        OnZoomChanged?.Invoke(Zoom);
        e.Handled = true;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        UpdateGrid();
        Refrech();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _ = MouseManager.Execute(
            new(async () =>
            {
                PointerHandler
                    .HandleShiftLeftPressedMove(e)
                    .HandleMiddlePressedMove(e)
                    .HandleLeftPressedMove(e);

                if (e.Handled)
                {
                    UpdateGrid();
                    Refrech();
                }
            })
        );
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        PointerHandler.HandleLeftRelease(e);
        Refrech();
    }

    public override void Render(DrawingContext context)
    {
        _renderStopwatch.Restart();

        DrawBackground(context);
        var adj = Matrix.CreateTranslation(PanOffset) * Matrix.CreateScale(new Vector(Zoom, Zoom));

        using (context.PushTransform(adj))
        {
            DrawGrid(context);
            DrawNodes(context);
            DrawNodeLinks(context);
            DrawSelection(context);
        }

        DrawZoom(context);

        _renderStopwatch.Stop();
        _lastRenderTimeMs = _renderStopwatch.ElapsedMilliseconds;

        if (ShowStat)
        {
            DrawMetrics(context);
        }
    }

    private void DrawMetrics(DrawingContext ctx)
    {
        var metrics = new FormattedText(
            $"Cache: {(_useCache ? "ON" : "OFF")}\n" + $"Render: {_lastRenderTimeMs}ms\n",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            10,
            Brushes.White
        );

        ctx.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 150, 80));
        ctx.DrawText(metrics, new Point(5, 5));
    }

    public Point ScreenToWorld(Point p)
    {
        return (p / Zoom) - PanOffset;
    }

    public Point WorldToScreen(Point p)
    {
        return (p + PanOffset) * Zoom;
    }

    
}
